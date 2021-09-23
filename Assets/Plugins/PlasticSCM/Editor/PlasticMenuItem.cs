using UnityEditor;

using Codice.UI;

namespace Codice
{
    class PlasticMenuItem
    {
        [MenuItem(MENU_ITEM_NAME)]
        public static void ShowPanel()
        {
            ShowWindow.Plastic();
        }

        const string MENU_ITEM_NAME = "Window/" + UnityConstants.PLASTIC_WINDOW_TITLE;
    }
}
