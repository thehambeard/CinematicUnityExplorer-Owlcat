using UnityExplorer.Serializers;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class BonesManager : PanelBase, ICellPoolDataSource<BonesCell>
    {
        public override string Name => $"Bones Manager";
        public override int MinWidth => 1000;
        public override int MinHeight => 800;
        public override Vector2 DefaultAnchorMin => Vector2.zero;
        public override Vector2 DefaultAnchorMax => Vector2.zero;
        public Toggle turnOffAnimatorToggle;
        
        private IAnimator animator;
        private Text skeletonName;
        private InputFieldRef saveLoadinputField;
        private InputFieldRef searchBoneNameInput;
        private List<Transform> bones = new List<Transform>();
        private Dictionary<string, CachedBonesTransform> bonesOriginalState = new();

        public List<BoneTree> boneTrees = new();
        public ScrollPool<BonesCell> boneScrollPool;
        public int ItemCount => boneTrees.Count;
        private bool DoneScrollPoolInit;

        public BonesManager(UIBase owner, List<Transform> bones, IAnimator animator) : base(owner)
        {
            this.bones = bones;
            this.animator = animator;
            skeletonName.text = $"Skeleton: {animator?.name}  ";
            BuildBoneTrees();
        }

        public void RefreshBones(List<Transform> bones) {
            this.bones = bones;
            boneTrees.Clear();
            BuildBoneTrees();
            boneScrollPool.Refresh(true, true);
        }

        private void BuildBoneTrees(){
            BoneTree root = new BoneTree(animator.wrappedObject.gameObject, bones);
            if (root.obj != null){
                root.AssignLevels();
                boneTrees.Add(root);
            } else {
                foreach(BoneTree childTree in root.childTrees){
                    childTree.AssignLevels();
                    boneTrees.Add(childTree);
                }
            }
        }

        private void CollapseBoneTrees(){
            boneTrees.Clear();
            BuildBoneTrees();
            boneScrollPool.Refresh(true, true);
        }

        private void ExpandBoneTrees(){
            // We collapse before expanding to start from scratch and dont duplicate nodes
            CollapseBoneTrees();

            List<BoneTree> newBoneTrees = new();

            foreach(BoneTree childTree in boneTrees){
                newBoneTrees.AddRange(childTree.flatten());
            }

            boneTrees = newBoneTrees;
            boneScrollPool.Refresh(true, false);
        }

        private void SearchBones(){
            ExpandBoneTrees();
            boneTrees = boneTrees.Where(tree => tree.obj.name.IndexOf(searchBoneNameInput.Component.text, 0, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            boneScrollPool.Refresh(true, true);
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            if (active && !DoneScrollPoolInit)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.Rect);
                boneScrollPool.Initialize(this);
                DoneScrollPoolInit = true;
            }
        }

        protected override void ConstructPanelContent()
        {
            GameObject bonesPanelHeader = UIFactory.CreateHorizontalGroup(ContentRoot, "BonesPanelHeader", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(bonesPanelHeader, minHeight: 25, flexibleWidth: 9999);

            skeletonName = UIFactory.CreateLabel(bonesPanelHeader, $"SkeletonName", "");
            UIFactory.SetLayoutElement(skeletonName.gameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            skeletonName.fontSize = 16;

            saveLoadinputField = UIFactory.CreateInputField(bonesPanelHeader, $"FileNameInput", "File name");
            UIFactory.SetLayoutElement(saveLoadinputField.GameObject, minWidth: 380, minHeight: 25);

            GameObject spacer1 = UIFactory.CreateUIObject("Spacer", bonesPanelHeader);
            LayoutElement spaceLayout1 = UIFactory.SetLayoutElement(spacer1, minWidth: 20, flexibleWidth: 0);

            ButtonRef savePose = UIFactory.CreateButton(bonesPanelHeader, "SavePoseButton", "Save pose");
            UIFactory.SetLayoutElement(savePose.GameObject, minWidth: 100, minHeight: 25);
            savePose.OnClick += SaveBones;

            ButtonRef loadPose = UIFactory.CreateButton(bonesPanelHeader, "LoadPoseButton", "Load pose");
            UIFactory.SetLayoutElement(loadPose.GameObject, minWidth: 100, minHeight: 25);
            loadPose.OnClick += LoadBones;

            GameObject header = UIFactory.CreateHorizontalGroup(ContentRoot, "BonesPanelHeader", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(header, minHeight: 35, flexibleWidth: 9999);

            GameObject turnOffAnimatorToggleObj = UIFactory.CreateToggle(header, "Animator toggle", out turnOffAnimatorToggle, out Text turnOffAnimatorToggleText);
            UIFactory.SetLayoutElement(turnOffAnimatorToggleObj, minHeight: 25, flexibleWidth: 9999);
            turnOffAnimatorToggle.onValueChanged.AddListener(OnTurnOffAnimatorToggle);
            turnOffAnimatorToggleText.text = "Toggle animator (needs to be off to move bones)";

            searchBoneNameInput = UIFactory.CreateInputField(header, $"SearchBoneInput", "Search bone name");
            UIFactory.SetLayoutElement(searchBoneNameInput.GameObject, minWidth: 225, minHeight: 25);

            ButtonRef searchButton = UIFactory.CreateButton(header, "SearchButton", "Search");
            UIFactory.SetLayoutElement(searchButton.GameObject, minWidth: 75, minHeight: 25);
            searchButton.OnClick += SearchBones;

            ButtonRef resetButton = UIFactory.CreateButton(header, "ResetSearchButton", "Reset");
            UIFactory.SetLayoutElement(resetButton.GameObject, minWidth: 75, minHeight: 25);
            resetButton.OnClick += () => { CollapseBoneTrees(); searchBoneNameInput.Component.text = ""; };

            GameObject spacer2 = UIFactory.CreateUIObject("Spacer", header);
            LayoutElement spaceLayout2 = UIFactory.SetLayoutElement(spacer2, minWidth: 20, flexibleWidth: 0);

            ButtonRef collapseAllButton = UIFactory.CreateButton(header, "CollapseAllButton", "Collapse all");
            UIFactory.SetLayoutElement(collapseAllButton.GameObject, minWidth: 100, minHeight: 25);
            collapseAllButton.OnClick += CollapseBoneTrees;

            ButtonRef expandAllButton = UIFactory.CreateButton(header, "ExpandAllButton", "Expand all");
            UIFactory.SetLayoutElement(expandAllButton.GameObject, minWidth: 100, minHeight: 25);
            expandAllButton.OnClick += ExpandBoneTrees;

            boneScrollPool = UIFactory.CreateScrollPool<BonesCell>(ContentRoot, "BonesList", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.06f, 0.06f, 0.06f));
            UIFactory.SetLayoutElement(scrollObj, flexibleWidth: 9999, flexibleHeight: 9999);
        }

        private void OnTurnOffAnimatorToggle(bool value)
        {
            if (value){
                // Restore meshes manually in case some are not part of a skeleton and won't get restored automatically.
                // Besides, this restores the scale, which the animator doesn't.
                foreach (Transform bone in bones){
                    bonesOriginalState[bone.name].CopyToTransform(bone);
                    // We assume these were on before. If not we should save its state beforehand.
                    bone.gameObject.SetActive(true);
                }
            } else {
                bonesOriginalState.Clear();
                foreach (Transform bone in bones){
                    bonesOriginalState[bone.name] = new CachedBonesTransform(bone.localPosition, bone.localEulerAngles, bone.localScale);
                }
            }
            animator.enabled = value;
        }

        public void RestoreBoneState(string boneName)
        {
            foreach (Transform bone in bones){
                if (bone.name == boneName){
                    bonesOriginalState[boneName].CopyToTransform(bone);
                    return;
                }
            }
        }

        private void SaveBones(){
            Dictionary<string, List<CachedBonesTransform>> bonesTreeCache = new();
            // Get the list of bones based on the hierarchy order so we can deserialize it in the same order, instead of just using the bones list.
            List<BoneTree> allBoneTrees = new();
            foreach(BoneTree tree in boneTrees) {
                allBoneTrees.AddRange(tree.flatten());
            }

            foreach(BoneTree tree in allBoneTrees){
                if (!bonesTreeCache.ContainsKey(tree.obj.name)){
                    bonesTreeCache.Add(tree.obj.name, new List<CachedBonesTransform>());
                }
                CachedBonesTransform entry = new CachedBonesTransform(tree.obj.transform.localPosition, tree.obj.transform.localEulerAngles, tree.obj.transform.localScale);
                bonesTreeCache[tree.obj.name].Add(entry);
            }

            string filename = saveLoadinputField.Component.text;
            if (filename.EndsWith(".cuepose") || filename.EndsWith(".CUEPOSE")) filename = filename.Substring(filename.Length-7);
            if (string.IsNullOrEmpty(filename)) filename = $"{animator?.name}-{DateTime.Now.ToString("yyyy-M-d HH-mm-ss")}";
            string posesPath = Path.Combine(ExplorerCore.ExplorerFolder, "Poses");
            System.IO.Directory.CreateDirectory(posesPath);

            // Serialize
            string serializedData = BonesSerializer.Serialize(bonesTreeCache);
            File.WriteAllText($"{posesPath}\\{filename}.cuepose", serializedData);
        }

        private void LoadBones(){
            string filename = saveLoadinputField.Component.text;
            if (filename.EndsWith(".cuepose") || filename.EndsWith(".CUEPOSE")) filename = filename.Substring(filename.Length-7);
            if (string.IsNullOrEmpty(filename)){
                ExplorerCore.LogWarning("Empty file name. Please write the name of the file to load.");
                return;
            }

            string posesPath = Path.Combine(ExplorerCore.ExplorerFolder, "Poses");
            string xml;
            try {
                xml = File.ReadAllText($"{posesPath}\\{filename}.cuepose");
            }
            catch (Exception ex) {
                ExplorerCore.LogWarning(ex);
                return;
            }
            Dictionary<string, List<CachedBonesTransform>> deserializedDict;
            try {
                deserializedDict = BonesSerializer.Deserialize(xml);
            }
            catch (Exception ex) {
                ExplorerCore.LogWarning(ex);
                return;
            }

            turnOffAnimatorToggle.isOn = false;
            foreach(Transform boneTransform in bones) {
                List<CachedBonesTransform> cachedTransformList;
                deserializedDict.TryGetValue(boneTransform.name, out cachedTransformList);
                if (cachedTransformList != null && cachedTransformList.Count > 0){
                    CachedBonesTransform cachedTransform = cachedTransformList[0];
                    cachedTransform.CopyToTransform(boneTransform);

                    cachedTransformList.RemoveAt(0);
                    if (cachedTransformList.Count == 0) {
                        deserializedDict.Remove(boneTransform.name);
                    } else {
                        deserializedDict[boneTransform.name] = cachedTransformList;
                    }
                }
            }

            if (deserializedDict.Count > 0) {
                ExplorerCore.LogWarning($"Couldn't apply every bone in the pose. Wrong entity?");
            }
        }

        public void SetCell(BonesCell cell, int index)
        {
            if (index >= boneTrees.Count)
            {
                cell.Disable();
                return;
            }

            BoneTree boneTree = boneTrees[index];
            cell.SetBoneTree(boneTree, this);
            cell.UpdateTransformControlValues(true);
        }

        public void OnCellBorrowed(BonesCell cell) {
            cell.UpdateVectorSlider();
        }

        public override void Update()
        {
            base.Update();

            foreach(BonesCell boneCell in boneScrollPool.CellPool) {
                boneCell.UpdateVectorSlider();
            }
        }
    }

    public class BoneTree
    {
        public GameObject obj;
        public int level;
        public List<BoneTree> childTrees = new();

        public BoneTree(GameObject obj, List<Transform> bones){
            // For some reason comparing GameObjects isn't working as intended in IL2CPP games, therefore we use their instance hash.
#if CPP
            if (bones.Any(bone => bone.gameObject.GetInstanceID() == obj.GetInstanceID())) {
                this.obj = obj;
            }
#else
            if (bones.Contains(obj.transform)) {
                this.obj = obj;
            }
#endif
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                if (child.gameObject.activeSelf){
                    childTrees.Add(new BoneTree(child.gameObject, bones));
                }
            }

            Trim();
            childTrees = childTrees.OrderBy(b => b.obj.name).ToList();
        }

        private void Trim(){
            List<BoneTree> newList = new();
            foreach (BoneTree childTree in childTrees)
            {
                if (childTree.obj == null){
                    newList.AddRange(childTree.childTrees);
                } else {
                    newList.Add(childTree);
                }
            }

            this.childTrees = newList;
        }

        // TODO: refactor BoneTree so we don't need to call this after creating an instance.
        public void AssignLevels(){
            AssignLevel(0);
        }

        private void AssignLevel(int distanceFromRoot){
            level = distanceFromRoot;
            foreach (BoneTree childTree in childTrees)
            {
                childTree.AssignLevel(distanceFromRoot + 1);
            }
        }

        public override string ToString(){
            string return_string = "";
            if (obj != null){
                return_string = $"{obj.name} lvl: {level} - ";
            }

            foreach (BoneTree childTree in childTrees)
            {
                return_string = return_string + childTree.ToString();
            }

            return return_string;
        }

        public List<GameObject> getGameObjects(){
            List<GameObject> return_list = new();
            if (obj != null){
                return_list.Add(obj);
            }

            foreach (BoneTree childTree in childTrees)
            {
                return_list.AddRange(childTree.getGameObjects());
            }

            return return_list;
        }

        public List<BoneTree> flatten(){
            List<BoneTree> return_list = new();
            if (obj != null){
                return_list.Add(this);
            }

            foreach (BoneTree childTree in childTrees)
            {
                return_list.AddRange(childTree.flatten());
            }

            return return_list;
        }
    }
}
