using System;

using UnityEditor;

namespace Codice.UI
{
    public static class EnumPopupSetting<E>
    {
        public static E Load(
            string popupSettingName,
            E defaultValue)
        {
            string enumValue = EditorPrefs.GetString(
                GetSettingKey(popupSettingName));

            if (string.IsNullOrEmpty(enumValue))
                return defaultValue;

            return (E)Enum.Parse(typeof(E), enumValue);
        }

        public static void Save(
            E selected,
            string popupSettingName)
        {
            EditorPrefs.SetString(
                GetSettingKey(popupSettingName),
                selected.ToString());
        }

        public static void Clear(
            string popupSettingName)
        {
            EditorPrefs.DeleteKey(
                GetSettingKey(popupSettingName));
        }

        static string GetSettingKey(string popupSettingName)
        {
            return string.Format(
                popupSettingName, PlayerSettings.productGUID,
                SELECTED_ENUM_VALUE_KEY);
        }

        static string SELECTED_ENUM_VALUE_KEY = "SelectedEnumValue";
    }
}
