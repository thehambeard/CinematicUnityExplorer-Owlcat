using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif
using UniverseLib.UI.Widgets.ScrollView;

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

namespace UnityExplorer.UI.Panels
{
    public class CamPaths : UEPanel, ICellPoolDataSource<CamPathNodeCell>
    {
        public CamPaths(UIBase owner) : base(owner)
        {
            controlPoints = new List<CatmullRom.CatmullRomPoint>();
            followObject = null;
            pathVisualizer = null;
            time = 10;

            // Timer setup
            startTimer = new System.Timers.Timer(3000);
            startTimer.Elapsed += (source, e) => StartPath();
            startTimer.AutoReset = false;

            // CatmullRom Constants
            alphaCatmullRomSlider = new Slider();
            tensionCatmullRomSlider = new Slider();
        }

        public override string Name => "Cam Paths";
        public override UIManager.Panels PanelType => UIManager.Panels.CamPaths;
        public override int MinWidth => 725;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
		public List<CatmullRom.CatmullRomPoint> controlPoints = new List<CatmullRom.CatmullRomPoint>();
        bool closedLoop;
        float time = 10;

        public GameObject followObject;

        Toggle visualizePathToggle;
        public GameObject pathVisualizer;

        bool unpauseOnPlay;
        bool waitBeforePlay;
        private System.Timers.Timer startTimer;
        public bool pauseOnFinish;

        InputFieldRef alphaCatmullRomInput;
        Slider alphaCatmullRomSlider;
        float alphaCatmullRomValue = 0.5f;

        InputFieldRef tensionCatmullRomInput;
        Slider tensionCatmullRomSlider;
        float tensionCatmullRomValue = 0;

        public ScrollPool<CamPathNodeCell> nodesScrollPool;
        public int ItemCount => controlPoints.Count;
        private static bool DoneScrollPoolInit;

        CamPointsUpdater camPointsUpdater;

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            if (active && !DoneScrollPoolInit)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);
                nodesScrollPool.Initialize(this);
                DoneScrollPoolInit = true;
            }

            nodesScrollPool.Refresh(true, false);
        }

        public void OnCellBorrowed(CamPathNodeCell cell) { }

        public void SetCell(CamPathNodeCell cell, int index){
            if (index >= controlPoints.Count)
            {
                cell.Disable();
                return;
            }

            CatmullRom.CatmullRomPoint point = controlPoints[index];
            cell.point = point;
            cell.index = index;
            cell.indexLabel.text = $"{index}";
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {   
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "MainOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);

            ButtonRef startButton = UIFactory.CreateButton(horiGroup, "Start", "►", new Color(0.2f, 0.4f, 0.2f));
            UIFactory.SetLayoutElement(startButton.GameObject, minWidth: 50, minHeight: 25);
            startButton.OnClick += StartButton_OnClick;

            ButtonRef pauseContinueButton = UIFactory.CreateButton(horiGroup, "Pause/Continue", "❚❚/►");
            UIFactory.SetLayoutElement(pauseContinueButton.GameObject, minWidth: 50, minHeight: 25);
            pauseContinueButton.OnClick += TogglePause_OnClick;

            ButtonRef stopButton = UIFactory.CreateButton(horiGroup, "Stop", "■", new Color(0.4f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(stopButton.GameObject, minWidth: 50, minHeight: 25);
            stopButton.ButtonText.fontSize = 20;
            stopButton.OnClick += Stop_OnClick;

            ButtonRef AddNode = UIFactory.CreateButton(horiGroup, "AddCamNode", "+");
            UIFactory.SetLayoutElement(AddNode.GameObject, minWidth: 50, minHeight: 25);
            AddNode.ButtonText.fontSize = 20;
            AddNode.OnClick += AddNode_OnClick;

            ButtonRef DeletePath = UIFactory.CreateButton(horiGroup, "DeletePath", "Clear");
            UIFactory.SetLayoutElement(DeletePath.GameObject, minWidth: 70, minHeight: 25);
            DeletePath.OnClick += () => {
                controlPoints.Clear();
                nodesScrollPool.Refresh(true, false);
                MaybeRedrawPath();
            };

            Toggle closedLoopToggle = new Toggle();
            GameObject toggleClosedLoopObj = UIFactory.CreateToggle(horiGroup, "Close path in a loop", out closedLoopToggle, out Text toggleClosedLoopText);
            UIFactory.SetLayoutElement(toggleClosedLoopObj, minHeight: 25, flexibleWidth: 9999);
            closedLoopToggle.isOn = false;
            closedLoopToggle.onValueChanged.AddListener((isClosedLoop) => {closedLoop = isClosedLoop; MaybeRedrawPath(); EventSystemHelper.SetSelectedGameObject(null);});
            toggleClosedLoopText.text = "Close path in a loop";

            InputFieldRef TimeInput = null;
            AddInputField("Time", "Path time (in seconds at 60fps):", $"Default: {time}", out TimeInput, Time_OnEndEdit, 50, horiGroup);
            TimeInput.Text = time.ToString();

            
            GameObject secondRow = UIFactory.CreateHorizontalGroup(ContentRoot, "ExtraOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(secondRow, minHeight: 25, flexibleWidth: 9999);

            GameObject visualizePathObj = UIFactory.CreateToggle(secondRow, "Visualize Path", out visualizePathToggle, out Text visualizePathText);
            UIFactory.SetLayoutElement(visualizePathObj, minHeight: 25, flexibleWidth: 9999);
            visualizePathToggle.isOn = false;
            visualizePathToggle.onValueChanged.AddListener(ToggleVisualizePath);
            visualizePathText.text = "Visualize path";

            GameObject unpauseOnPlayObj = UIFactory.CreateToggle(secondRow, "Unpause on play", out Toggle unpauseOnPlayToggle, out Text unpauseOnPlayText);
            UIFactory.SetLayoutElement(unpauseOnPlayObj, minHeight: 25, flexibleWidth: 9999);
            unpauseOnPlayToggle.isOn = false;
            unpauseOnPlayToggle.onValueChanged.AddListener((value) => unpauseOnPlay = value);
            unpauseOnPlayText.text = "Unpause on play";

            GameObject pauseOnFinishObj = UIFactory.CreateToggle(secondRow, "Pause on finish", out Toggle pauseOnFinishToggle, out Text pauseOnFinishText);
            UIFactory.SetLayoutElement(pauseOnFinishObj, minHeight: 25, flexibleWidth: 9999);
            pauseOnFinishToggle.isOn = false;
            pauseOnFinishToggle.onValueChanged.AddListener((value) => pauseOnFinish = value);
            pauseOnFinishText.text = "Pause on finish";

            GameObject waitBeforePlayObj = UIFactory.CreateToggle(secondRow, "Wait before play", out Toggle waitBeforePlayToggle, out Text waitBeforePlayText);
            UIFactory.SetLayoutElement(waitBeforePlayObj, minHeight: 25, flexibleWidth: 9999);
            waitBeforePlayToggle.isOn = false;
            waitBeforePlayToggle.onValueChanged.AddListener((value) => waitBeforePlay = value);
            waitBeforePlayText.text = "Wait 3 seconds before start";


            // CatmullRom alpha value
            GameObject thridRow = AddInputField("alphaCatmullRom", "Alpha:", "0.5", out alphaCatmullRomInput, AlphaCatmullRom_OnEndEdit, 50);
            alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString();

            GameObject alphaCatmullRomObj = UIFactory.CreateSlider(thridRow, "Alpha CatmullRom Slider", out alphaCatmullRomSlider);
            UIFactory.SetLayoutElement(alphaCatmullRomObj, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            alphaCatmullRomSlider.m_FillImage.color = Color.clear;
            alphaCatmullRomSlider.minValue = 0;
            alphaCatmullRomSlider.maxValue = 1;
            alphaCatmullRomSlider.value = alphaCatmullRomValue;
            alphaCatmullRomSlider.onValueChanged.AddListener((newAlpha) => {
                alphaCatmullRomValue = newAlpha;
                alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString("0.00");

                MaybeRedrawPath();
            });

            // CatmullRom tension value
            AddInputField("tensionCatmullRomO", "Tension:", "0", out tensionCatmullRomInput, TensionCatmullRom_OnEndEdit, 50, thridRow);
            tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString();

            GameObject tensionCatmullRomObj = UIFactory.CreateSlider(thridRow, "Tension CatmullRom Slider", out tensionCatmullRomSlider);
            UIFactory.SetLayoutElement(tensionCatmullRomObj, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            tensionCatmullRomSlider.m_FillImage.color = Color.clear;
            tensionCatmullRomSlider.minValue = 0;
            tensionCatmullRomSlider.maxValue = 1;
            tensionCatmullRomSlider.value = tensionCatmullRomValue;
            tensionCatmullRomSlider.onValueChanged.AddListener((newTension) => {
                tensionCatmullRomValue = newTension;
                tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString("0.00");

                MaybeRedrawPath();
            });

            nodesScrollPool = UIFactory.CreateScrollPool<CamPathNodeCell>(ContentRoot, "NodeList", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.03f, 0.03f, 0.03f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);
        }

        void AlphaCatmullRom_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                alphaCatmullRomInput.Text = alphaCatmullRomValue.ToString();
                return;
            }

            alphaCatmullRomValue = parsed;
            alphaCatmullRomSlider.value = alphaCatmullRomValue;

            MaybeRedrawPath();
        }

        void TensionCatmullRom_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                tensionCatmullRomInput.Text = tensionCatmullRomValue.ToString();
                return;
            }

            tensionCatmullRomValue = parsed;
            tensionCatmullRomSlider.value = tensionCatmullRomValue;

            MaybeRedrawPath();
        }

        private void ToggleVisualizePath(bool enable){
            // Had to include this check because the pathVisualizer was null for some reason
            if (enable){
                if (controlPoints.Count > 2){
                    CreateAndMovePathVisualizer();

                    UpdateCatmullRomMoverData();
                    List<CatmullRom.CatmullRomPoint> lookaheadPoints = GetCameraPathsManager().GetLookaheadPoints();
                    int skip_points = 5; // How many points do we have to skip before drawing another arrow (otherwise they look very cluttered)
                    int n = 0;
                    for (int i=0; i < lookaheadPoints.Count; i++){
                        // We also want to draw an arrow on the last point in case its skipped.
                        if (n < skip_points && i != lookaheadPoints.Count - 1){ 
                            n++;
                            continue;
                        }

                        Vector3 arrowPos = lookaheadPoints[i].position;
                        if (followObject != null){
                            if (!FreeCamPanel.followRotationToggle.isOn){
                                arrowPos = Quaternion.Inverse(FreeCamPanel.followObject.transform.rotation) * arrowPos;
                            }
                            // TODO: Avoid using TransformPoint
                            arrowPos = followObject.transform.TransformPoint(arrowPos);
                        }
                        Quaternion arrowRot = lookaheadPoints[i].rotation;
                        if (followObject != null && FreeCamPanel.followRotationToggle.isOn) arrowRot = followObject.transform.rotation * arrowRot;

                        // We could expose the color of the arrow to a setting
                        GameObject arrow = ArrowGenerator.CreateArrow(arrowPos, arrowRot, Color.green);
                        arrow.transform.SetParent(pathVisualizer.transform, true);
                        n = 0;
                    }
                }
            }
            else {
                if (pathVisualizer) {
                    UnityEngine.Object.Destroy(pathVisualizer);
                    pathVisualizer = null;
                    CreateAndMovePathVisualizer();
                }
            }
        }

        public void MaybeRedrawPath(){
            if (visualizePathToggle.isOn){
                ToggleVisualizePath(false);
                ToggleVisualizePath(true);
            }
        }

        GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit, int inputMinWidth = 50, GameObject existingRow = null)
        {
            GameObject row = existingRow != null ? existingRow : UIFactory.CreateHorizontalGroup(ContentRoot, "Editor Field",
            false, false, true, true, 5, default, new Color(1, 1, 1, 0));

            Text posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 40, minHeight: 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: inputMinWidth, minHeight: 25);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return row;
        }

        void StartButton_OnClick()
        {
            if (waitBeforePlay) {
                if (startTimer.Enabled){
                    startTimer.Stop();
                }

                startTimer.Enabled = true;
            } else {
                StartPath();
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void StartPath(){
            if (GetCameraPathsManager()){
                UpdateCatmullRomMoverData();
                GetCameraPathsManager().StartPath();
                UIManager.ShowMenu = false;
                visualizePathToggle.isOn = false;
            }

            if (unpauseOnPlay && UIManager.GetTimeScaleWidget().IsPaused()){
                    UIManager.GetTimeScaleWidget().PauseToggle();
            }
        }

        void TogglePause_OnClick(){
            if(GetCameraPathsManager()){
                GetCameraPathsManager().TogglePause();
                if (visualizePathToggle.isOn && GetCameraPathsManager().IsPaused()) visualizePathToggle.isOn = false;
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void Stop_OnClick(){
            if (GetCameraPathsManager()){
                GetCameraPathsManager().Stop();
            }

            EventSystemHelper.SetSelectedGameObject(null);
        }

        void AddNode_OnClick(){
            EventSystemHelper.SetSelectedGameObject(null);

            CatmullRom.CatmullRomPoint point = new CatmullRom.CatmullRomPoint(
                FreeCamPanel.GetCameraPosition(),
                FreeCamPanel.GetCameraRotation(),
                FreeCamPanel.GetFreecam().fieldOfView
            );

            controlPoints.Add(point);
            nodesScrollPool.Refresh(true, false);
            MaybeRedrawPath();
        }

        void Time_OnEndEdit(string input)
        {
            EventSystemHelper.SetSelectedGameObject(null);

            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            time = parsed;
            if(GetCameraPathsManager()){
                GetCameraPathsManager().setTime(time);
            }
        }

        private CatmullRom.CatmullRomMover GetCameraPathsManager(){
            return FreeCamPanel.cameraPathMover;
        }

        public void UpdatedFollowObject(GameObject obj){
            // Had to include this check because the pathVisualizer was null for some reason
            if (followObject != null){
                // destroy the obhject from the parent
                GameObject.Destroy(camPointsUpdater);
                camPointsUpdater = null;
            }
            followObject = obj;
            if (obj != null){
                // create the component on the object itself
                camPointsUpdater = RuntimeHelper.AddComponent<CamPointsUpdater>(obj, typeof(CamPointsUpdater));

                camPointsUpdater.SetFollowObject(obj);
            }
            //nodesScrollPool.Refresh(true, false);
        }

        public void CreateAndMovePathVisualizer(){
            if (pathVisualizer == null)
                pathVisualizer = new GameObject("PathVisualizer");

            if (followObject != null){
                pathVisualizer.transform.position = followObject.transform.position;
                if (FreeCamPanel.followRotationToggle.isOn){
                    Vector3 offset = pathVisualizer.transform.position - followObject.transform.position;
                    pathVisualizer.transform.position = pathVisualizer.transform.position - offset + followObject.transform.rotation * offset;
                    pathVisualizer.transform.rotation = followObject.transform.rotation;
                }
            }
        }

        public void TranslatePointsToGlobal(bool isFollowingRotation){
            if (isFollowingRotation) TranslatePointsRotationToGlobal();
            TranslatePointsPositionToGlobal();
        }

        public void TranslatePointsToLocal(bool isFollowingRotation){
            TranslatePointsPositionToLocal();
            if (isFollowingRotation) TranslatePointsRotationToLocal();
        }

        private void TranslatePointsPositionToGlobal(){
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Vector3 newPos = point.position + followObject.transform.position;

                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, point.rotation, point.fov);
                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        // Also changes position based on rotation
        public void TranslatePointsRotationToGlobal(){
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Quaternion newRot = followObject.transform.rotation * point.rotation;
                Vector3 newPos = followObject.transform.rotation * point.position;

                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, newRot, point.fov);
                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        private void TranslatePointsPositionToLocal(){
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Vector3 newPos = point.position - followObject.transform.position;
                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, point.rotation, point.fov);

                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        // Also changes position based on rotation
        public void TranslatePointsRotationToLocal(){
            List<CatmullRom.CatmullRomPoint> newControlPoints = new List<CatmullRom.CatmullRomPoint>();
            foreach(CatmullRom.CatmullRomPoint point in controlPoints){
                Quaternion newRot = Quaternion.Inverse(followObject.transform.rotation) * point.rotation;
                Vector3 newPos = Quaternion.Inverse(followObject.transform.rotation) * point.position;

                CatmullRom.CatmullRomPoint newPoint = new CatmullRom.CatmullRomPoint(newPos, newRot, point.fov);
                newControlPoints.Add(newPoint);
            }

            controlPoints = newControlPoints;
        }

        void UpdateCatmullRomMoverData() {
            GetCameraPathsManager().setClosedLoop(closedLoop);
            GetCameraPathsManager().setSplinePoints(controlPoints.ToArray());
            GetCameraPathsManager().setTime(time);
            GetCameraPathsManager().setCatmullRomVariables(alphaCatmullRomValue, tensionCatmullRomValue);

            GetCameraPathsManager().CalculateLookahead();
        }
    }

    public class CamPointsUpdater : MonoBehaviour
    {
        GameObject followObject;

#if CPP
        static CamPointsUpdater()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CamPointsUpdater>();
        }

        public CamPointsUpdater(IntPtr ptr) : base(ptr) { }
#endif

        internal void SetFollowObject(GameObject obj){
            followObject = obj;
        }

        internal void Update()
        {
            if (followObject == null)
                return;

            //CamPathsPanel.MaybeRedrawPath();

            CamPaths CamPathsPanel = UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths);

            if (CamPathsPanel.pathVisualizer){
                CamPathsPanel.pathVisualizer.transform.position = followObject.transform.position;
                if (FreeCamPanel.followRotationToggle.isOn){
                    Vector3 offset = CamPathsPanel.pathVisualizer.transform.position - FreeCamPanel.followObject.transform.position;
                    CamPathsPanel.pathVisualizer.transform.position = CamPathsPanel.pathVisualizer.transform.position - offset + FreeCamPanel.followObject.transform.rotation * offset;
                    CamPathsPanel.pathVisualizer.transform.rotation = followObject.transform.rotation;
                }
            }
        }
    }
}
