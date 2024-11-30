using UnityExplorer.CacheObject;
using UnityExplorer.Inspectors;
using UnityExplorer.ObjectExplorer;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

namespace UnityExplorer.UI.Panels
{
    public class Misc : UEPanel
    {
        public enum ScreenshotState
        {
            DoNothing,
            TurnOffUI,
            TakeScreenshot,
            TurnOnUI,
        }

        public Misc(UIBase owner) : base(owner)
        {
            disabledCanvases = new List<Canvas>();
            screenshotStatus = ScreenshotState.DoNothing;
        }

        public override string Name => "Misc";
        public override UIManager.Panels PanelType => UIManager.Panels.Misc;
        public override int MinWidth => 325;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        List<Canvas> disabledCanvases;
        Toggle HUDToggle;

        MethodInfo captureScreenshotFunction = null;
        int superSizeValue = 2;
        public ScreenshotState screenshotStatus;

        Toggle HighLodToggle;
        object qualitySettings = null;
        PropertyInfo lodBias = null;
        float _defaultLodBias = 1;

        // We save the current properties of the Renderers and Lights to restore them after editing them with togglers
        internal Dictionary<Renderer, bool> renderersReceiveShadows = new();
        internal Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> renderersCastShadows = new();
        internal Dictionary<Light, int> vanillaLightsResolution = new();
        internal Dictionary<Light, float> vanillaLightsShadowBias = new();
        internal Dictionary<Light, LightShadows> vanillaLightsShadowType = new();
        int shadowsResolution = 5000;

        // Based on https://github.com/TollyH/Unity-FreeCam
        private void SetValueHUDElements(bool value){
            if (value){
                foreach (Canvas canvas in disabledCanvases)
                {
                    if (canvas == null)
                    {
                        continue;
                    }
                    canvas.enabled = true;
                }
            }
            else {
                disabledCanvases = RuntimeHelper.FindObjectsOfTypeAll(typeof(Canvas))
                .Select(obj => obj.TryCast<Canvas>())
                .Where(c => !c.name.Contains("unityexplorer") && c.isActiveAndEnabled)
                .ToList();

                foreach (Canvas canvas in disabledCanvases)
                {
                    if (canvas == null)
                    {
                        continue;
                    }
                    canvas.enabled = false;
                }
            }
        }

        public void ToggleHUDElements(){
            // SetValueHUDElements will be triggered automatically
            HUDToggle.isOn = !HUDToggle.isOn;
        }

        private void TakeScreenshot(){
            string filename = DateTime.Now.ToString("yyyy-M-d HH-mm-ss");

            string screenshotsPath = Path.Combine(ExplorerCore.ExplorerFolder, "Screenshots");
            System.IO.Directory.CreateDirectory(screenshotsPath);
            
            object[] args = {$"{screenshotsPath}\\{filename}.png", superSizeValue};
            try {
                captureScreenshotFunction.Invoke(qualitySettings, args);
            }
            catch { ExplorerCore.LogWarning("Failed to take a screenshot. Chances are the method has been stripped."); }
        }

        private void FindCaptureScreenshotFunction(){
            try {
                object screenCaptureClass = ReflectionUtility.GetTypeByName("UnityEngine.ScreenCapture");
                Type screenCaptureType = screenCaptureClass is Type type ? type : screenCaptureClass.GetActualType();
                captureScreenshotFunction = screenCaptureType.GetMethod("CaptureScreenshot", new Type[] {typeof(string), typeof(int)});
            }
            catch { ExplorerCore.Log("Couldn't find the ScreenCapture class."); }
        }

        private void FindQualitySettings(){
            qualitySettings = ReflectionUtility.GetTypeByName("UnityEngine.QualitySettings");
        }

        private void ToogleHighLods(bool areHighLodsOn){
            if (qualitySettings == null) FindQualitySettings();
            if (lodBias == null){
                Type qualitySettingsType = qualitySettings is Type type ? type : qualitySettings.GetActualType();
                lodBias = qualitySettingsType.GetProperty("lodBias");
                _defaultLodBias = (float)lodBias.GetValue(null, null);
            }
            lodBias.SetValue(null, areHighLodsOn ? 10000 : _defaultLodBias, null);
        }

        private void ToggleAllMeshesCastAndRecieveShadows(bool enable){
            if (enable){
                renderersReceiveShadows.Clear();
                renderersCastShadows.Clear();

                List<Renderer> renderers = RuntimeHelper.FindObjectsOfTypeAll(typeof(Renderer))
                .Select(obj => obj.TryCast<Renderer>())
                .Where(r => r.isVisible && r.enabled)
                .ToList();

                foreach (Renderer renderer in renderers){
                    renderersReceiveShadows[renderer] = renderer.receiveShadows;
                    renderersCastShadows[renderer] = renderer.shadowCastingMode;

                    renderer.receiveShadows = true;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
            } else {
                foreach (Renderer renderer in renderersReceiveShadows.Keys){
                    renderer.receiveShadows = renderersReceiveShadows[renderer];
                    renderer.shadowCastingMode = renderersCastShadows[renderer];
                }
            }
        }

        private void ToggleHighResShadows(bool enable){
            PropertyInfo shadowResolution = typeof(Light).GetProperty("shadowCustomResolution");

            if (enable){
                vanillaLightsResolution.Clear();
                vanillaLightsShadowBias.Clear();

                List<Light> vanillaLights = RuntimeHelper.FindObjectsOfTypeAll(typeof(Light))
                .Select(obj => obj.TryCast<Light>())
                .Where(l => !l.name.Contains("CUE - Light") && l.isActiveAndEnabled)
                .ToList();

                foreach (Light light in vanillaLights){
                    vanillaLightsResolution[light] = (int) shadowResolution.GetValue(light, null);
                    shadowResolution.SetValue(light, shadowsResolution, null);

                    vanillaLightsShadowBias[light] = light.shadowBias;
                    light.shadowBias = 0;
                }
            } else {
                foreach (Light light in vanillaLightsResolution.Keys){
                    shadowResolution.SetValue(light, vanillaLightsResolution[light], null);

                    light.shadowBias = vanillaLightsShadowBias[light];
                }
            }
        }

        private void ToggleShadowsOnAllLights(bool enable){
            if (enable){
                vanillaLightsShadowType.Clear();

                List<Light> vanillaLights = RuntimeHelper.FindObjectsOfTypeAll(typeof(Light))
                .Select(obj => obj.TryCast<Light>())
                .Where(l => !l.name.Contains("CUE - Light") && l.isActiveAndEnabled)
                .ToList();

                foreach (Light light in vanillaLights){
                    vanillaLightsShadowType[light] = light.shadows;
                    light.shadows = LightShadows.Soft;
                }
            } else {
                foreach (Light light in vanillaLightsShadowType.Keys){
                    light.shadows = vanillaLightsShadowType[light];
                }
            }
        }

        // We use an enum to walk a series of steps in each frame, so we can take the screenshot without CinematicUnityExplorer UI.
        public void MaybeTakeScreenshot(){
            if (captureScreenshotFunction != null){
                switch (screenshotStatus){
                    case ScreenshotState.TurnOffUI:
                        screenshotStatus = ScreenshotState.TakeScreenshot;
                        UIManager.ShowMenu = false;
                        break;
                    case ScreenshotState.TakeScreenshot:
                        TakeScreenshot();
                        screenshotStatus = ScreenshotState.TurnOnUI;
                        break;
                    case ScreenshotState.TurnOnUI:
                        screenshotStatus = ScreenshotState.DoNothing;
                        UIManager.ShowMenu = true;
                        break;
                    case ScreenshotState.DoNothing:
                    default:
                        break;
                }
            }
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            // HUD toggle
            GameObject HUDhoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HUDhoriGroup", false, false, true, true, 3,
            default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(HUDhoriGroup, minHeight: 25, flexibleWidth: 9999);

            HUDToggle = new Toggle();
            GameObject HUDToggleObj = UIFactory.CreateToggle(HUDhoriGroup, "Toggle HUD", out HUDToggle, out Text HUDToggleText);
            UIFactory.SetLayoutElement(HUDToggleObj, minHeight: 25);
            HUDToggle.onValueChanged.AddListener(SetValueHUDElements);
            HUDToggle.isOn = true; // we picked up only the active UI elements
            HUDToggleText.text = "Toggle HUD";

            HighLodToggle = new Toggle();
            GameObject HighLodToggleObj = UIFactory.CreateToggle(ContentRoot, "HighLOD", out HighLodToggle, out Text HighLodToggleText);
            UIFactory.SetLayoutElement(HighLodToggleObj, minHeight: 25);
            HighLodToggle.onValueChanged.AddListener(ToogleHighLods);
            HighLodToggle.isOn = false;
            HighLodToggleText.text = "High LODs Toggle";

            // Screenshot function
            FindCaptureScreenshotFunction();
            if (captureScreenshotFunction != null){
                GameObject TakeScreenshotHoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Take screenshot", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
                UIFactory.SetLayoutElement(TakeScreenshotHoriGroup, minHeight: 25, flexibleWidth: 9999);

                ButtonRef takeScreenshot = UIFactory.CreateButton(TakeScreenshotHoriGroup, "TakeScreenshot", "Take screenshot");
                UIFactory.SetLayoutElement(takeScreenshot.GameObject, minWidth: 150, minHeight: 25);
                takeScreenshot.OnClick += () => screenshotStatus = ScreenshotState.TurnOffUI;

                AddInputField(TakeScreenshotHoriGroup, "Supersize", "Supersize:", $"{2}", SuperSize_OnEndEdit);
            }

            Toggle ShadowMeshesToggle = new Toggle();
            GameObject ShadowMeshesObj = UIFactory.CreateToggle(ContentRoot, "ShadowMeshes", out ShadowMeshesToggle, out Text ShadowMeshesText);
            UIFactory.SetLayoutElement(ShadowMeshesObj, minHeight: 25);
            ShadowMeshesToggle.onValueChanged.AddListener(ToggleAllMeshesCastAndRecieveShadows);
            ShadowMeshesToggle.isOn = false;
            ShadowMeshesText.text = "Make all meshes cast and recieve shadows";

            Toggle ShadowsOnAllLightsToggle = new Toggle();
            GameObject ShadowsOnAllLightsObj = UIFactory.CreateToggle(ContentRoot, "ShadowOnAllLights", out ShadowsOnAllLightsToggle, out Text ShadowsOnAllLightsText);
            UIFactory.SetLayoutElement(ShadowsOnAllLightsObj, minHeight: 25);
            ShadowsOnAllLightsToggle.onValueChanged.AddListener(ToggleShadowsOnAllLights);
            ShadowsOnAllLightsToggle.isOn = false;
            ShadowsOnAllLightsText.text = "Make all game lights emit shadow";

            // High Resolution Shadows
            GameObject HighResShadowsGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HighRes shadows group", false, false, true, true, 3,
            default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(HighResShadowsGroup, minHeight: 25, flexibleWidth: 9999);

            Toggle HighResShadowsToggle = new Toggle();
            GameObject HighResShadowsObj = UIFactory.CreateToggle(HighResShadowsGroup, "HighResShadows", out HighResShadowsToggle, out Text HighResShadowsText);
            UIFactory.SetLayoutElement(HighResShadowsObj, minHeight: 25);
            HighResShadowsToggle.onValueChanged.AddListener(ToggleHighResShadows);
            HighResShadowsToggle.isOn = false;
            HighResShadowsText.text = "High Res Shadows";

            AddInputField(HighResShadowsGroup, "Resolution", "Resolution:", $"{5000}", HighResShadowsResolution_OnEndEdit);
        }

        GameObject AddInputField(GameObject parent, string name, string labelText, string placeHolder, Action<string> onInputEndEdit)
        {
            Text posLabel = UIFactory.CreateLabel(parent, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 75, minHeight: 25);

            InputFieldRef inputField = UIFactory.CreateInputField(parent, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 50, minHeight: 25);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return parent;
        }

        void SuperSize_OnEndEdit(string input)
        {
            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            superSizeValue = parsed;
        }

        void HighResShadowsResolution_OnEndEdit(string input)
        {
            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            shadowsResolution = parsed;
        }
    }
}
