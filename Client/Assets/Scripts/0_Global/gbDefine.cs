using System.Collections.Generic;
using System;


public static class GlobalDefine
{
    #region Player Prefs

    public const string DEF_PREFS_USER_IDX = "userIdx";
    public const string DEF_PREFS_USER_ID = "userId";
    public const string DEF_PREFS_LOCAL_USER_ID = "local_userId";
    public const string DEF_PREFS_LANGUAGE = "language";

    #endregion

    public const int screen_w = 1920;
    public const int screen_h = 1080;


    public const float DEF_LOADINGDELAYTIME = 0.1f;

    public const string FontStyle_H3 = "<style=H3>";
    public const string FontStyle_Normal = "<style=Normal>";

    public const byte DEF_BGM_COUNT = 3;
    public const byte DEF_SFX_COUNT = 6;

    public const float DEF_SHOWMESSAGE_DELAY = 3.5f;

    public const float DEF_ALERT_SHOWTIME = 3f;

    public const string DEF_SERVER_ERROR = "9000000";

    public const int IDOL_STORAGE_MAX_SLOT = 7;

    #region 서버 관련 상수

    // 서버 전송 타입 정의
    public const string DEF_SERVER_AUTH = "AUTH";
    public const string DEF_SERVER_ACCOUNT = "CREATE_USER";
    public const string DEF_SERVER_ACCOUNT_LOGIN = "CREATE_USER_LOGIN";
    public const string DEF_SERVER_LOGIN = "LOGIN";
    public const string DEF_SERVER_LOGOUT = "LOGOUT";
    public const string DEF_SERVER_PING = "PING";
    public const string DEF_SERVER_LOAD_DATA = "LOAD_DATA";
    public const string DEF_SERVER_SAVE_DATA = "SAVE_DATA";
    public const string DEF_SERVER_CREATE_PD = "CREATE_PD";

    public const string DEF_SERVER_CHEAT = "CHEAT";

    // 결과 코드
    public const int RESULT_SUCCESS = 200;
    public const int RESULT_FAIL = 400;
    public const int RESULT_UNAUTHORIZED = 401;
    public const int RESULT_NOT_FOUND = 404;
    public const int RESULT_DATA_IS_NULL = 499;

    // 플랫폼 타입
    public const int PLATFORM_ANDROID = 1;
    public const int PLATFORM_IOS = 2;
    public const int PLATFORM_WINDOWS = 3;

    // 기타 상수
    public const int DEFAULT_PING_INTERVAL = 10; // seconds
    public const int MAX_MESSAGE_SIZE = 16 * 1024 * 1024; // 16MB

    #endregion

    // test

    public const float ALPHA_NONE = 0.1f;
    public const float ALPHA_NOTE_IN = 0.2f;
    public const float ALPHA_CARDDRAW = 1.0f;
    public const float ALPHA_CENTER = 0.5f;
    public const float ALPHA_SELECTED = 0f;
    public const float ALPHA_NOTE_JUDGMENT_OUT = 0.3f;
    public const float ALPHA_NOTE_JUDGMENT_IN = 1f;

    #region 판정 계수

    public const float DEF_JUDGMENT_BAD = 0.5f;
    public const float DEF_JUDGMENT_GOOD = 0.7f;
    public const float DEF_JUDGMENT_GREAT = 1.0f;
    public const float DEF_JUDGMENT_PERFECT = 1.2f;
    public const float DEF_JUDGMENT_PERFECTx2 = 1.3f;
    public const float DEF_JUDGMENT_PERFECTx3 = 1.4f;
    public const float DEF_JUDGMENT_PERFECTx4 = 1.5f;

    #endregion


#if CARD_BATTLE
    public enum CARD_GRADE
    {
        N = 0,
        R,
        SR,
        SSR,
        USR
    }

    public const int DEF_DECK_COUNT = 30;
    public const int DEF_HAND_COUNT_MAX = 10;
    public const int DEF_DRAW_COUNT_MAX = 5;
    public const int DEF_SLOT_COUNT_MIN = 2;
    public const int DEF_SLOT_COUNT_MAX = 5;
    public const float DEF_SLOT_REGISTATION_TIME = 15.0f;
    public const float DEF_SLOT_PRODUCTION_TIME = 15.0f;
    public const float DEF_TIMEINGMECHANIC_TIME = 30.0f;

    public const int DEF_ONESHOT_DECK_COUNT = 30;
    public const int DEF_ONESHOT_DECK_PRODUCTION_COUNT = 10;

    public const int DEF_ONESHOT_DRAW_COUNT_MAX = 3;
    public const int DEF_ONESHOT_DRAW_PRODUCTION_COUNT_MAX = 1;

    public const float DEF_ONESHOT_CARD_SELECT_TIME = 10.0f;

    public const int DEF_HAND_PRODUCTION_COUNT_MAX = 10;
    public const int DEF_PLAYER_HP_MAX = 100;


    public enum BATTLE_CARD_TYPE
    {
        RED = 0,
        GREEN,
        BLUE,
        NONE
    }

    public enum BATTLE_CARD_GRADE
    {
        R = 1,
        SR,
        GR,
        UR
    }

    public enum BATTLE_BEAT_GRADE
    {
        BAD = 0,
        COOL,
        GREAT,
        PERFECT,
        MAX
    }

    public enum BATTLE_PLAYER_INDEX
    {
        PLAYER_1 = 0,
        PLAYER_2,
        PLAYER_3,
        PLAYER_4,
        PLAYER_5,
        PLAYER_6,
        PLAYER_7,
        PLAYER_8,
        MAX
    }

    public enum BATTLE_ABILITY_TYPE
    {
        NONE = 0,
        POWER,
        SCORE,
        HP,
        DAMAGE_DOWN,
        DAMAGE_UP,
        POWER_P = 1001,
        SCORE_P,
        HP_P,
        DAMAGE_DOWN_P,
        DAMAGE_UP_P,
        REFLEX_P,
        SCORE_STEAL_P,
        PERFECT = 10001,
        ENEMY_GREAT,
        ENEMY_COOL,
        ENEMY_BAD,
        POWER_STEAL,
        IGNORE,
    }

    public enum BATTLE_BUFF
    {
        BUFF_EFFECT_IGNORE = 0x00000001,
    }

    public const int DEF_BATTLE_DECK_CARD_COUNT_MAX = 50;
    public const int DEF_BATTLE_DECK_HANDCARD_COUNT_MAX = 2;
    public const int DEF_BATTLE_PLAYER_HP_MAX = 70000;
#endif

    #region 오피스 로비 모듈 프리랩 네임 정리

    public const string DEF_PATH_OFFICELOBBY_USERINFO_SHORT = "Management/Lobby/Lobby_UserInfo_Short";
    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIL = "Management/Lobby/Lobby_UserInfo_Detail";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIl_EMPLOYEE =
        "Management/Lobby/Lobby_UserInfo_Detail_Employee";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIL_EMPLOYEE_DETAIL =
        "Management/Lobby/Lobby_UserInfo_Detail_Employee_Detail";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIl_OFFICE = "Management/Lobby/Lobby_UserInfo_Detail_Office";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIl_SCHEDULE =
        "Management/Lobby/Lobby_UserInfo_Detail_Schedule";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIl_CASHMANAGE =
        "Management/Lobby/Lobby_UserInfo_Detail_CashManage";

    public const string DEF_PATH_OFFICELOBBY_USERINFO_DETAIl_RECRUIT = "Management/Lobby/Lobby_UserInfo_Detail_Recruit";
    public const string DEF_PATH_OFFICELOBBY_QUICK_SCEDULEVIEW = "Management/Lobby/Lobby_Quick_ScheduleView";

    public const string DEF_PATH_OFFICELOBBY_WALLET_SHORT = "Management/Lobby/Lobby_Wallet_Short";
    public const string DEF_PATH_OFFICELOBBY_FREEMEMBER_SHORT = "Management/Lobby/Lobby_FreeMember_Short";
    public const string DEF_PATH_OFFICELOBBY_FREEMEMBER = "Management/Lobby/Lobby_FreeMember";

    public const string DEF_PATH_OFFICELOBBY_DANCEBATTLE = "Management/Lobby/Lobby_DanceBattle_Detail";
    public const string DEF_PATH_OFFICELOBBY_DANCEBATTLE_SHORT = "Management/Lobby/Lobby_DanceBattle_Short";

    public const string DEF_PATH_OFFICELOBBY_DANCEBATTLE2 = "Management/Lobby/Lobby_DanceBattle_Detail";
    public const string DEF_PATH_OFFICELOBBY_DANCEBATTLE2_SHORT = "Management/Lobby/Lobby_DanceBattle2_Short";


    public const string DEF_PATH_OFFICELOBBY_NPC_QUICKMENU = "Management/Lobby/Lobby_NPC_QuickMenu";

    public const string DEF_PATH_OFFICELOBBY_ORDER_TRAINING = "Management/Lobby/Lobby_Order_Training";
    public const string DEF_PATH_OFFICELOBBY_ORDER_ALBEIT = "Management/Lobby/Lobby_Order_Albeit";
    public const string DEF_PATH_OFFICELOBBY_ORDER_REST = "Management/Lobby/Lobby_Order_Rest";

    public const string DEF_PATH_OFFICELOBBY_LOCK = "Management/Lobby/3D Lock";

    public const string DEF_PATH_OFFICELOBBY_ASSIGNSHORT = "Management/Lobby/LOBBY_Assignment_Short";
    public const string DEF_PATH_OFFICELOBBY_ASSIGNDETAIL = "Management/Lobby/LOBBY_Assignment_Detail";

    public const string DEF_PATH_OFFICELOBBY_IDOLMONITER = "Management/Lobby/Lobby_Monitoring";

    public const string DEF_PATH_OFFICELOBBY_RECRUIT_SHORT = "Management/Lobby/Lobby_Recruit_Short";

    public const string DEF_PATH_OFFICELOBBY_WORLD_SHORT = "Management/Lobby/Lobby_World_Short";

    public const string DEF_PATH_OFFICELOBBY_WORLD_PRODUCTION = "Management/Lobby/World_Production";

    #endregion

    #region UI연출 이름

    public static string ui_basic_in = "ui_basic_in";
    public static string ui_basic_out = "ui_basic_out";
    public static string ui_lobby_in = "ui_lobby_in";
    public static string ui_lobby_out = "ui_lobby_out";
    public static string ui_eye_on = "ui_eye_on";
    public static string ui_eye_off = "ui_eye_off";

    #endregion
}
