#if KM
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.PostProcessing;
using UnityExplorer.ObjectExplorer;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI.Panels
{
    internal class RowComponentGroup
    {
        public readonly ButtonRef InspectButton;
        public readonly ButtonRef DefaultButton;
        public readonly Toggle Toggle;

        public RowComponentGroup(ButtonRef inspectButton, ButtonRef defaultButton, Toggle toggle)
        {
            InspectButton = inspectButton;
            DefaultButton = defaultButton;
            Toggle = toggle;
        }
    }

    internal class VolumeComponentData
    {
        public readonly PostProcessingComponentBase VolumeComponent;
        public readonly VolumeData Owner;
        public readonly bool IsActiveDefault;
        public readonly RowComponentGroup RowComponentGroup;

        public VolumeComponentData(PostProcessingComponentBase volumeComponent, VolumeData owner, RowComponentGroup rowComponentGroup)
        {
            VolumeComponent = volumeComponent;
            Owner = owner;
            RowComponentGroup = rowComponentGroup;

            IsActiveDefault = volumeComponent.GetModel().enabled;
            rowComponentGroup.Toggle.isOn = volumeComponent.GetModel().enabled;
            rowComponentGroup.InspectButton.OnClick += OnInspectButtonClick;
            rowComponentGroup.Toggle.onValueChanged.AddListener(OnToggleChanged);

            RowComponentGroup.DefaultButton.Transform.parent.gameObject.SetActive(false);
        }

        public void RevertToDefault() => RowComponentGroup.Toggle.isOn = IsActiveDefault;
        public void SetComponentState(bool isOn) => RowComponentGroup.Toggle.isOn = isOn;
        private void OnToggleChanged(bool isOn) => VolumeComponent.GetModel().enabled = isOn;
        private void OnInspectButtonClick() => InspectorManager.InspectWithFilters(VolumeComponent, string.Empty);
    }

    internal class VolumeData
    {
        public List<VolumeComponentData> Components = [];

        public readonly PostProcessingBehaviour Volume;
        public readonly SceneData Owner;
        public readonly RowComponentGroup RowComponentGroup;

        public bool IsAnyActive => Components.Any(x => x.VolumeComponent.GetModel().enabled);

        public VolumeData(PostProcessingBehaviour volume, SceneData owner, RowComponentGroup rowComponentGroup)
        {
            Owner = owner;
            Volume = volume;
            RowComponentGroup = rowComponentGroup;

            rowComponentGroup.InspectButton.OnClick += OnInspectClick;
            rowComponentGroup.DefaultButton.OnClick += RevertAllComponents;
            rowComponentGroup.Toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        public void RevertAllComponents()
        {
            Components.ForEach(x => x.RevertToDefault());
            RowComponentGroup.Toggle.m_IsOn = IsAnyActive;
        }

        public void SetAllComponents(bool isOn) => RowComponentGroup.Toggle.isOn = isOn;
        private void OnToggleChanged(bool isOn) => Components.ForEach(x => x.SetComponentState(isOn));
        private void OnInspectClick() => InspectorManager.InspectWithFilters(Volume, string.Empty);
    }

    internal class SceneData
    {
        public List<VolumeData> Volumes = [];

        public readonly Scene Scene;
        public readonly RowComponentGroup RowComponentGroup;

        public bool IsAnyActive => Volumes.Any(x => x.IsAnyActive);

        public SceneData(Scene scene, RowComponentGroup rowComponentGroup)
        {
            Scene = scene;
            RowComponentGroup = rowComponentGroup;
            rowComponentGroup.InspectButton.OnClick += OnInspectButtonClick;
            rowComponentGroup.DefaultButton.OnClick += RevertAllVolumes;
            rowComponentGroup.Toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        public void RevertAllVolumes()
        {
            Volumes.ForEach(x => x.RevertAllComponents());
            RowComponentGroup.Toggle.m_IsOn = IsAnyActive;
        }

        private void OnToggleChanged(bool isOn) => Volumes.ForEach(x => x.SetAllComponents(isOn));
        private void OnInspectButtonClick() => InspectorManager.InspectWithFilters(Scene, string.Empty);
    }

    internal class PostProcessingPanel(UIBase owner) : UEPanel(owner)
    {
        public override string Name => "Post-processing";
        public override UIManager.Panels PanelType => UIManager.Panels.PostProcessingPanel;
        public override int MinWidth => 500;
        public override int MinHeight => 500;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;
        public readonly List<SceneData> SceneData = [];

        private GameObject _content;
        private ButtonRef _updateEffects;
        private readonly List<GameObject> _uiElements = [];

        public void UpdatePPElements()
        {
            DestroyScrollViewContent();

            SceneData.Clear();

            SceneHandler.Update();

            foreach (Scene scene in SceneHandler.LoadedScenes
                .Where(s => !string.IsNullOrEmpty(s.name) && s.isLoaded))
            {
                try
                {
                    var ppBehaviours = RuntimeHelper.GetRootGameObjects(scene)
                       .SelectMany(rootObject => rootObject.GetComponentsInChildren<PostProcessingBehaviour>(includeInactive: false))
                       .ToList();

                    if (ppBehaviours.Count == 0)
                        continue;

                    SceneData sData = new(scene, ConstructRow(scene.name));

                    for (int i = 0; i < ppBehaviours.Count; i++)
                    {
                        if (ppBehaviours[i].m_Components.Count == 0)
                            continue;

                        sData.Volumes.Add(new(
                            ppBehaviours[i],
                            sData,
                            ConstructRow(UnityHelpers.GetTransformPath(ppBehaviours[i].transform, true), 1)));

                        foreach (var comp in ppBehaviours[i].m_Components)
                            sData.Volumes[i].Components.Add(new(comp, sData.Volumes[i], ConstructRow(comp.GetType().Name, 2)));
                    }

                    SceneData.Add(sData);
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogError($"Failed creating SceneData for Scene {scene}");
                    ExplorerCore.LogError(ex.Message + ex.StackTrace);
                }
            }
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        private void DestroyScrollViewContent()
        {
            _uiElements.ForEach(e => GameObject.DestroyImmediate(e));
            _uiElements.Clear();
        }

        protected override void ConstructPanelContent()
        {
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "MainOptions", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);
            //UIElements.Add(horiGroup);

            _updateEffects = UIFactory.CreateButton(horiGroup, "RefreshEffects", "Refresh Effects");
            UIFactory.SetLayoutElement(_updateEffects.GameObject, minWidth: 150, minHeight: 25);
            _updateEffects.OnClick += UpdatePPElements;

            Text openComponentLabel = UIFactory.CreateLabel(horiGroup, "OpenComponentLabel", "Open object in inspector  ", TextAnchor.MiddleRight, Color.white, false, 15);
            UIFactory.SetLayoutElement(openComponentLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 9999);

            GameObject scrollView = UIFactory.CreateScrollView(ContentRoot, "VolumeScrollView", out _content, out _, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(scrollView, flexibleHeight: 9999);
        }

        private void ConstructSpacer(GameObject partent, int? width)
        {
            GameObject spacerObj = UIFactory.CreateUIObject("Spacer", partent, new Vector2(0, 0));
            UIFactory.SetLayoutElement(spacerObj, minWidth: width, flexibleWidth: 0, minHeight: 0, flexibleHeight: 0);
        }

        private RowComponentGroup ConstructRow(string text, int tab = 0)
        {
            GameObject horiGroup = UIFactory.CreateHorizontalGroup(_content, "SceneRow", false, false, true, true, 3,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 25, flexibleWidth: 9999);

            ConstructSpacer(horiGroup, 10 * tab);

            GameObject toggleObj = UIFactory.CreateToggle(horiGroup, "BehaviourToggle", out Toggle enabledToggle, out Text behavText, default, 17, 17);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 17, flexibleHeight: 0, minWidth: 17);

            GameObject defaultBtnHolder = UIFactory.CreateHorizontalGroup(horiGroup, "NameButtonHolder",
                false, false, true, true, childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(defaultBtnHolder, minWidth: 50, minHeight: 25, flexibleHeight: 0);
            defaultBtnHolder.AddComponent<Mask>().showMaskGraphic = false;

            var defaultButton = UIFactory.CreateButton(defaultBtnHolder, "DefaultButton", "Default", new Color(0.20f, 0.20f, 0.20f));
            UIFactory.SetLayoutElement(defaultButton.Component.gameObject, minWidth: 50, minHeight: 25, flexibleHeight: 0);

            GameObject nameBtnHolder = UIFactory.CreateHorizontalGroup(horiGroup, "NameButtonHolder",
                false, false, true, true, childAlignment: TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(nameBtnHolder, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);
            nameBtnHolder.AddComponent<Mask>().showMaskGraphic = false;

            var nameButton = UIFactory.CreateButton(nameBtnHolder, "NameButton", "Name", new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(nameButton.Component.gameObject, flexibleWidth: 9999, minHeight: 25, flexibleHeight: 0);

            Text nameLabel = nameButton.Component.GetComponentInChildren<Text>();
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameLabel.alignment = TextAnchor.MiddleLeft;
            nameLabel.text = text;

            _uiElements.Add(horiGroup);

            return new(nameButton, defaultButton, enabledToggle);
        }
    }
}
#endif