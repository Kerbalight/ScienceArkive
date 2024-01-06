using SpaceWarp.API.Assets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components
{
    public class UIToolkitElement : VisualElement
    {
        /// <summary>
        /// Loads a UIToolkitElement from the asset bundle, avoiding the need to manually
        /// compose the asset path and to lowercase the asset path.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static VisualTreeAsset Load(string assetPath) => AssetManager.GetAsset<VisualTreeAsset>($"{ScienceArkivePlugin.ModGuid}/ScienceArkive_ui/ui/{assetPath.ToLower()}");

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        public UIToolkitElement(string assetPath)
        {
            if (AssetManager.GetAsset<VisualTreeAsset>($"{ScienceArkivePlugin.ModGuid}/{assetPath}").CloneTree() is TemplateContainer template) Add(template);
        }
    }
}
