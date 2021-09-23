using UnityEditor;

namespace Codice.UI
{
    public static class BoolSetting
    {
        public static bool Load(
            string boolSettingName,
            bool defaultValue)
        {
            return EditorPrefs.GetBool(
                GetSettingKey(boolSettingName),
                defaultValue);
        }

        public static void Save(
            bool value,
            string boolSettingName)
        {
            EditorPrefs.SetBool(
                GetSettingKey(boolSettingName), value);
        }

        public static void Clear(
            string boolSettingName)
        {
            EditorPrefs.DeleteKey(
                GetSettingKey(boolSettingName));
        }

        static string GetSettingKey(string boolSettingName)
        {
            return string.Format(
                boolSettingName, PlayerSettings.productGUID,
                PREFERENCE_VALUE_KEY);
        }

        static string PREFERENCE_VALUE_KEY = "PreferenceValue";
    }
}
