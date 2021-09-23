namespace Codice.UI
{
    public static class UnityConstants
    {
        public const float CANCEL_BUTTON_SIZE = 15;

        public const float SMALL_BUTTON_WIDTH = 40;
        public const float REGULAR_BUTTON_WIDTH = 60;
        public const float LARGE_BUTTON_WIDTH = 100;
        public const float EXTRA_LARGE_BUTTON_WIDTH = 130;

        public const float SEARCH_FIELD_WIDTH = 550;

        public const string TREEVIEW_META_LABEL = " +meta";
        public const float TREEVIEW_CHECKBOX_SIZE = 17;
        public const float TREEVIEW_BASE_INDENT = 16f;
        public const float FIRST_COLUMN_WITHOUT_ICON_INDENT = 5;

#if UNITY_2019_1_OR_NEWER
        public const float DROPDOWN_ICON_Y_OFFSET = 2;
        public const float TREEVIEW_FOLDOUT_Y_OFFSET = 0;
        public const float TREEVIEW_ROW_HEIGHT = 21;
        public const float TREEVIEW_HEADER_CHECKBOX_Y_OFFSET = 0;
        public const float TREEVIEW_CHECKBOX_Y_OFFSET = 0;
        public static float DIR_CONFLICT_VALIDATION_WARNING_LABEL_HEIGHT = 21;
#else
        public const float DROPDOWN_ICON_Y_OFFSET = 5;
        public const float TREEVIEW_FOLDOUT_Y_OFFSET = 1;
        public const float TREEVIEW_ROW_HEIGHT = 20;
        public const float TREEVIEW_HEADER_CHECKBOX_Y_OFFSET = 6;
        public const float TREEVIEW_CHECKBOX_Y_OFFSET = 2;
        public static float DIR_CONFLICT_VALIDATION_WARNING_LABEL_HEIGHT = 18;
#endif

        public const int LEFT_MOUSE_BUTTON = 0;
        public const int RIGHT_MOUSE_BUTTON = 1;

        public const int UNSORT_COLUMN_ID = -1;

        public const string PLASTIC_WINDOW_TITLE = "Plastic SCM";

        public const int ACTIVE_TAB_UNDERLINE_HEIGHT = 2;
        public const int SPLITTER_INDICATOR_HEIGHT = 1;

        internal const double SEARCH_DELAYED_INPUT_ACTION_INTERVAL = 0.25;
        internal const double SELECTION_DELAYED_INPUT_ACTION_INTERVAL = 0.25;
        internal const double AUTO_REFRESH_DELAYED_INTERVAL = 0.25;

        public const string PENDING_CHANGES_TABLE_SETTINGS_NAME = "{0}_PendingChangesTree_{1}";
        public const string GLUON_INCOMING_CHANGES_TABLE_SETTINGS_NAME = "{0}_GluonIncomingChangesTree_{1}";
        public const string GLUON_INCOMING_ERRORS_TABLE_SETTINGS_NAME = "{0}_GluonIncomingErrorsList_{1}";
        public const string GLUON_UPDATE_REPORT_TABLE_SETTINGS_NAME = "{0}_GluonUpdateReportList_{1}";
        public const string DEVELOPER_INCOMING_CHANGES_TABLE_SETTINGS_NAME = "{0}_DeveloperIncomingChangesTree_{1}";
        public const string DEVELOPER_UPDATE_REPORT_TABLE_SETTINGS_NAME = "{0}_DeveloperUpdateReportList_{1}";
        public const string REPOSITORIES_TABLE_SETTINGS_NAME = "{0}_RepositoriesList_{1}";
        public const string CHANGESETS_TABLE_SETTINGS_NAME = "{0}_ChangesetsList_{1}";
        public const string CHANGESETS_DATE_FILTER_SETTING_NAME = "{0}_ChangesetsDateFilter_{1}";
        public const string CHANGESETS_SHOW_CHANGES_SETTING_NAME = "{0}_ShowChanges_{1}";

        public static class ChangesetsColumns
        {
            public const float CHANGESET_NUMBER_WIDTH = 80;
            public const float CHANGESET_NUMBER_MIN_WIDTH = 50;
            public const float CREATION_DATE_WIDTH = 150;
            public const float CREATION_DATE_MIN_WIDTH = 100;
            public const float CREATED_BY_WIDTH = 200;
            public const float CREATED_BY_MIN_WIDTH = 110;
            public const float COMMENT_WIDTH = 300;
            public const float COMMENT_MIN_WIDTH = 100;
            public const float BRANCH_WIDTH = 160;
            public const float BRANCH_MIN_WIDTH = 90;
            public const float REPOSITORY_WIDTH = 210;
            public const float REPOSITORY_MIN_WIDTH = 90;
            public const float GUID_WIDTH = 270;
            public const float GUID_MIN_WIDTH = 100;
        }
    }
}
