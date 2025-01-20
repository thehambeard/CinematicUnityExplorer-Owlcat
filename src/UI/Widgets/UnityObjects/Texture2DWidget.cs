﻿using System.Collections;
using System.Diagnostics;
using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Panels;
using UniverseLib.Runtime;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public class Texture2DWidget : UnityObjectWidget
    {
        Texture2D texture;
        Color[] originalColors;
        bool shouldDestroyTexture;

        bool textureViewerWanted;
        ButtonRef toggleButton;

        bool toggleRedChannel = true;
        bool toggleGreenChannel = true;
        bool toggleBlueChannel = true;
        bool toggleAlphaChannel = true;
        ButtonRef toggleRedButton;
        ButtonRef toggleBlueButton;
        ButtonRef toggleGreenButton;
        ButtonRef toggleAlphaButton;

        GameObject textureViewerRoot;
        InputFieldRef savePathInput;
        Image image;
        LayoutElement imageLayout;

        public override void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            if (target.TryCast<Cubemap>() is Cubemap cubemap)
            {
                texture = TextureHelper.UnwrapCubemap(cubemap);
                shouldDestroyTexture = true;
            }
            else if (target.TryCast<Sprite>() is Sprite sprite)
            {
                if (sprite.packingMode == SpritePackingMode.Tight)
                    texture = sprite.texture;
                else
                {
                    texture = TextureHelper.CopyTexture(sprite.texture, sprite.textureRect);
                    shouldDestroyTexture = true;
                }
            }
            else if (target.TryCast<Image>() is Image image)
            {
                if (image.sprite.packingMode == SpritePackingMode.Tight)
                    texture = image.sprite.texture;
                else
                {
                    texture = TextureHelper.CopyTexture(image.sprite.texture, image.sprite.textureRect);
                    shouldDestroyTexture = true;
                }
            }
            else
                texture = target.TryCast<Texture2D>();

            if (textureViewerRoot)
                textureViewerRoot.transform.SetParent(inspector.UIRoot.transform);

            texture = TextureHelper.CreateReadableTexture(texture);
            originalColors = texture.GetPixels();
            SetChannelsActiveTexture();

            InspectorPanel.Instance.Dragger.OnFinishResize += OnInspectorFinishResize;
        }

        public override void OnReturnToPool()
        {
            InspectorPanel.Instance.Dragger.OnFinishResize -= OnInspectorFinishResize;

            if (shouldDestroyTexture)
                UnityEngine.Object.Destroy(texture);

            texture = null;
            shouldDestroyTexture = false;
            originalColors = null;

            if (image.sprite)
                UnityEngine.Object.Destroy(image.sprite);

            if (textureViewerWanted)
                ToggleTextureViewer();

            if (textureViewerRoot)
                textureViewerRoot.transform.SetParent(Pool<Texture2DWidget>.Instance.InactiveHolder.transform);

            base.OnReturnToPool();
        }

        void SetChannelsActiveTexture()
        {
            var modColors = new Color[originalColors.Length];

            for (int i = 0; i < originalColors.Length; i++)
            {
                modColors[i].r = toggleRedChannel ? originalColors[i].r : 0f;
                modColors[i].g = toggleGreenChannel ? originalColors[i].g : 0f;
                modColors[i].b = toggleBlueChannel ? originalColors[i].b : 0f;
                modColors[i].a = toggleAlphaChannel ? originalColors[i].a : 1f;
            }
            texture.SetPixels(modColors);
            texture.Apply();
        }

        void SetButtonColorFromState(bool toggle, ButtonRef button)
        {
            if (button != null && toggle)
                RuntimeHelper.SetColorBlockAuto(button.Component, new(0.2f, 0.4f, 0.2f));
            else
                RuntimeHelper.SetColorBlockAuto(button.Component, new(0.4f, 0.2f, 0.2f));
        }

        void ToggleRedChannel()
        {
            toggleRedChannel = !toggleRedChannel;
            SetButtonColorFromState(toggleRedChannel, toggleRedButton);
            SetChannelsActiveTexture();
        }

        void ToggleBlueChannel()
        {
            toggleBlueChannel = !toggleBlueChannel;
            SetButtonColorFromState(toggleBlueChannel, toggleBlueButton);
            SetChannelsActiveTexture();
        }

        void ToggleGreenChannel()
        {
            toggleGreenChannel = !toggleGreenChannel;
            SetButtonColorFromState(toggleGreenChannel, toggleGreenButton);
            SetChannelsActiveTexture();
        }

        void ToggleAlphaChannel()
        {
            toggleAlphaChannel = !toggleAlphaChannel;
            SetButtonColorFromState(toggleAlphaChannel, toggleAlphaButton);
            SetChannelsActiveTexture();
        }

        void ToggleTextureViewer()
        {
            if (textureViewerWanted)
            {
                // disable
                textureViewerWanted = false;
                textureViewerRoot.SetActive(false);
                toggleButton.ButtonText.text = "View Texture";

                owner.ContentRoot.SetActive(true);
            }
            else
            {
                // enable
                if (!image.sprite)
                    SetupTextureViewer();

                SetImageSize();

                textureViewerWanted = true;
                textureViewerRoot.SetActive(true);
                toggleButton.ButtonText.text = "Hide Texture";

                owner.ContentRoot.gameObject.SetActive(false);
            }
        }

        void SetupTextureViewer()
        {
            if (!this.texture)
                return;

            string name = texture.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";
            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.png");

            Sprite sprite = TextureHelper.CreateSprite(texture);
            image.sprite = sprite;
        }

        void OnInspectorFinishResize()
        {
            SetImageSize();
        }

        void SetImageSize()
        {
            if (!imageLayout)
                return;

            RuntimeHelper.StartCoroutine(SetImageSizeCoro());
        }

        IEnumerator SetImageSizeCoro()
        {
            // let unity rebuild layout etc
            yield return null;

            RectTransform imageRect = InspectorPanel.Instance.Rect;

            float rectWidth = imageRect.rect.width - 25;
            float rectHeight = imageRect.rect.height - 196;

            // If our image is smaller than the viewport, just use 100% scaling
            if (texture.width < rectWidth && texture.height < rectHeight)
            {
                imageLayout.minWidth = texture.width;
                imageLayout.minHeight = texture.height;
            }
            else // we will need to scale down the image to fit
            {
                // get the ratio of our viewport dimensions to width and height
                float viewWidthRatio = (float)((decimal)rectWidth / (decimal)texture.width);
                float viewHeightRatio = (float)((decimal)rectHeight / (decimal)texture.height);

                // if width needs to be scaled more than height
                if (viewWidthRatio < viewHeightRatio)
                {
                    imageLayout.minWidth = texture.width * viewWidthRatio;
                    imageLayout.minHeight = texture.height * viewWidthRatio;
                }
                else // if height needs to be scaled more than width
                {
                    imageLayout.minWidth = texture.width * viewHeightRatio;
                    imageLayout.minHeight = texture.height * viewHeightRatio;
                }
            }
        }

        void OnSaveTextureClicked()
        {
            if (!texture)
            {
                ExplorerCore.LogWarning("Texture is null, maybe it was destroyed?");
                return;
            }

            if (string.IsNullOrEmpty(savePathInput.Text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");
                return;
            }

            string path = savePathInput.Text;
            if (!path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                path += ".png";

            path = IOUtility.EnsureValidFilePath(path);

            if (File.Exists(path))
                File.Delete(path);

            TextureHelper.SaveTextureAsPNG(texture, path);
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            GameObject ret = base.CreateContent(uiRoot);

            // Button

            toggleButton = UIFactory.CreateButton(UIRoot, "TextureButton", "View Texture", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 150);
            toggleButton.OnClick += ToggleTextureViewer;

            // Texture viewer

            textureViewerRoot = UIFactory.CreateVerticalGroup(uiRoot, "TextureViewer", false, false, true, true, 2, new Vector4(5, 5, 5, 5),
                new Color(0.1f, 0.1f, 0.1f), childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(textureViewerRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            GameObject channelRowObj = UIFactory.CreateHorizontalGroup(textureViewerRoot, "ChannelRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            toggleRedButton = UIFactory.CreateButton(channelRowObj, "RedButton", "Red Channel");
            UIFactory.SetLayoutElement(toggleRedButton.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            toggleRedButton.OnClick += ToggleRedChannel;
            SetButtonColorFromState(toggleRedChannel, toggleRedButton);

            toggleBlueButton = UIFactory.CreateButton(channelRowObj, "BlueButton", "Blue Channel");
            UIFactory.SetLayoutElement(toggleBlueButton.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            toggleBlueButton.OnClick += ToggleBlueChannel;
            SetButtonColorFromState(toggleBlueChannel, toggleBlueButton);

            toggleGreenButton = UIFactory.CreateButton(channelRowObj, "GreenButton", "Green Channel");
            UIFactory.SetLayoutElement(toggleGreenButton.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            toggleGreenButton.OnClick += ToggleGreenChannel;
            SetButtonColorFromState(toggleGreenChannel, toggleGreenButton);

            toggleAlphaButton = UIFactory.CreateButton(channelRowObj, "AlphaButton", "Alpha Channel");
            UIFactory.SetLayoutElement(toggleAlphaButton.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            toggleAlphaButton.OnClick += ToggleAlphaChannel;
            SetButtonColorFromState(toggleAlphaChannel, toggleAlphaButton);

            // Save helper

            GameObject saveRowObj = UIFactory.CreateHorizontalGroup(textureViewerRoot, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            ButtonRef saveBtn = UIFactory.CreateButton(saveRowObj, "SaveButton", "Save .PNG", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveTextureClicked;

            savePathInput = UIFactory.CreateInputField(saveRowObj, "SaveInput", "...");
            UIFactory.SetLayoutElement(savePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // Actual texture viewer

            GameObject imageViewport = UIFactory.CreateVerticalGroup(textureViewerRoot, "ImageViewport", false, false, true, true,
                bgColor: new(1, 1, 1, 0), childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(imageViewport, flexibleWidth: 9999, flexibleHeight: 9999);

            GameObject imageHolder = UIFactory.CreateUIObject("ImageHolder", imageViewport);
            imageLayout = UIFactory.SetLayoutElement(imageHolder, 1, 1, 0, 0);

            GameObject actualImageObj = UIFactory.CreateUIObject("ActualImage", imageHolder);
            RectTransform actualRect = actualImageObj.GetComponent<RectTransform>();
            actualRect.anchorMin = new(0, 0);
            actualRect.anchorMax = new(1, 1);
            image = actualImageObj.AddComponent<Image>();

            textureViewerRoot.SetActive(false);

            return ret;
        }
    }
}
