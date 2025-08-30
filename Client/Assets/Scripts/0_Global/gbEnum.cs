using System;
using System.ComponentModel;

/// <summary>
/// 빌드 플랫폼
/// </summary>
public enum PLATFORM
{
    GOOGLE = 1,
    IOS = 2,
    ONESTORE = 3,
}

public enum DeviceType
{
    MOBILE,
    EDITOR,
    EMULATOR,
}

/// <summary>
/// 서버 리스트
/// </summary>
public enum Server
{
    DEV, // 내부 개발
    QA,
    LIVE,
    LOCAL,
    DEV_OTHER, // 내부 개발 포트 변경
}

public enum LANGGUAGE
{
    KR = 0,
    US,
    JP,
}


#region 사운드 관련

public enum SFX_TYPE
{
    COMMON ,
    BATTLE ,
}

public enum SOUND_SFX
{
    btn_clicked = 1, // 클릭음
    popup_closed, // 팝업창 닫기
    popup_open, // 팝업창 열기 
    notification, // 알림
    item_get, //  받기류
    unlock, // 사무실 개방
    levelup, // 유저 레벨업
    npc_focus, // npc 클릭시
    titlelogo, // 타이틀 로고
    toworld, // 월드로 전환시
    notification_bell, // 알림 벨소리
    notification_error, // 알림 에러류
    miss,
    bad,
    cool,
    good,
    great,
    perfect_1,
    perfect_2,
    perfect_3,
    end_win ,
    ui_button,
    prop_touch,
    prop_delete,
    prop_rotation,
    prop_install,
    ui_error,
    prop_wall, // 벽 설치
    prop_floor, // 마루 설치
    groove_time, // 그루브 타임 효과음
    

    /*Notification = 1,   // 알림
    Achievement = 2,  // 업적
    PopFX = 3,   // 팝업*/
}

public enum SOUND_BGM
{
    NONE = 0,
    BGM_1, // 월드
    BGM_2, //  오피스
    BGM_3, // 뿜뿜??  
    BGM_4, //  영입 남자
    BGM_5, // 영입 여자
    BGM_6, // 남자-보컬1
    BGM_7, // 남자-보컬2
    BGM_8, // 남자-보컬3
    BGM_9, // 여자-보컬1
    BGM_10, // 여자-보컬2
    BGM_11, // 여자-보컬3
    BGM_12, // 댄스1
    BGM_13, // 댄스2
    BGM_14, // 게임룸 배경 음악
    BGM_15, // 게임룸 배경 음악
    BGM_101 = 101, //배틀 배경 음악 - Chung Ha_Gotta Go(102BPM)
    BGM_102, //배틀 배경 음악 - IZONE_Violeta(115BPM)
    BGM_103, //배틀 배경 음악 - IZONE_Fiesta(123BPM)
}

#endregion

/// <summary>
/// 씬 구성
/// </summary>
public enum SCENE
{
    LOGIN = 1, // 로그인
    PD_CREATE = 2, // pd create
    MANAGEMENT = 3, // 오피스 로비
    GAMEROOM = 4, // 게임룸

    //
    DANCEBATTLE = 5, // 댄스 배틀

    IDOLSTORAGE = 6, // 아이돌 보관소

    STORYMODE = 8, // 스토리 모드
    IDOLROAD = 12,
}


#region 리듬 배틀 관련

public enum BattleType
{
    Battle_StoryMode = 0, // 배틀 스토리모드 전
    Battle_Single_Score, // 배틀 개인전 점수
    Battle_Single_Damage, // 배틀 개인전 데미지
    Battle_Team_Score, // 배틀 팀전 점수
    Battle_Team_Damage, // 배틀 팀전 데미지
}


public enum CARD_TYPE
{
    NONE = 0,
    RED,
    GREEN,
    BLUE,
    GRAY,
    MAX,
}

public enum CARD_GRADE
{
    // 등급명칭 변경
    /*R = 1,
    SR,
    GR,
    UR,
    MAX,*/
    R = 1 ,
    SR ,
    UR ,
    SSR ,
}

public enum RHYTHM_MODULE_TYPE
{
    None = 0,
    Common, // 공통
    Aiming_Point, // 조준점  
    AbilitySystem, // 어빌리티(스킬같은거)
    Fusion, // 퓨전 크리티컬
    Equalizer,
    Note, // 노트
    DanceCard, // 댄스 카드
    Groove, // 그루브시
    Ranking, // 랭킹
    Log, // 효과 로그
    SingleScore , // 싱글 점수판
    StageControl , // 무대 장치 컨트롤
    Result_Single, // 개인전 결과화면
}

/// <summary>
/// 판정 결과 열거형
/// </summary>
public enum JUDGMENT_RESULT
{
    BAD, // BAD 판정
    GOOD, // GOOD 판정  
    GREAT, // GREAT 판정
    PERFECT, // PERFECT 판정
    PERFECT1, // 퍼펙트 1의 자리수 연속 판정
    PERFECT10, // 퍼펙트 10의 자리수 연속 판정
}

public enum MODE_TYPE
{
    ALL = 0, // (공용)
    SCORE, // 점수전
    DAMAGE, // 데미지전 
    MAX,
}

/// <summary>
/// 배틀의 컨디션 타입
/// </summary>
//[Flags]
public enum CONDITION_TYPE
{
    /*NONE = 0,
    PERFECT_DONE = 1 << 0, // perfect 판정
    GREAT_DONE = 1 << 1, // great 판정
    GOOD_DONE = 1 << 2, // good 판정
    BAD_DONE = 1 << 3, // bad 판정
    VITAL_RED = 1 << 4,
    VITAL_GREEN = 1 << 5,
    VITAL_BLUE = 1 << 6,
    ROUND_WIN = 1 << 7, // 라운드(트랙) 승리시
    ROUND_LOSE = 1 << 8, // 라운드(트랙) 패배시*/
    NONE = 0,
    PERFECT_DONE = 1, // perfect 판정
    GREAT_DONE = 2, // great 판정
    GOOD_DONE = 3, // good 판정
    BAD_DONE = 4, // bad 판정
    VITAL_RED = 5,
    VITAL_GREEN = 6,
    VITAL_BLUE = 7,
    ROUND_WIN = 8, // 라운드(트랙) 승리시
    ROUND_LOSE = 9, // 라운드(트랙) 패배시*/
}

public enum ABILITY_TYPE
{
    NONE = 0,
    POWER = 1, // 파워
    SCORE = 2, // 획득 점수
    HP = 3, // 체력
    DAMAGE_DOWN = 4, // 뎀쥐 감소
    DAMAGE_UP = 5, // 뎀쥐 증가

    REFLEX = 6, // 뎀쥐 반사
    SCORE_STEAL = 7, // 상대 획득 점수 일부 획득
}

/// <summary>
/// 카드 애니메이션 타입
/// </summary>
public enum CARD_ANIMATION_TYPE
{
    In,
    Out,
    Select,
    SelectOut,
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

public enum GAME_MODE
{
    NONE = 0,
    DAMAGE = 1,
    SCORE = 2,
    SOLO = 3,
    COUNT = 4,
}

#endregion


#region 컨텐츠 명

/// <summary>
/// 컨텐츠이름
/// </summary>
public enum CONTENT_NAME
{
    NONE = 0,
    IDOLROAD = 1, // 아이돌 로드
    SCHEDULE = 2, // 스케줄
}

public enum CONTENT_TYPE
{
    NONE = 0,
    STORYMODE_LOBBY,
    GAMEROOM_LIST,
}

/// <summary>
/// 해금할수 있는 상태 타입
/// </summary>
public enum UNLOCK_CONDITIONTYPE
{
    PD_LEVEL = 1, // 피디 레벨
    PD_GRADE = 2, // 피디 등급
}

#endregion

/// <summary>
/// 팝업 노티시 해당 타입
/// </summary>
public enum POPUP_NOTIFICATION_TYPE
{
    LEVEL = 0,
    GRADE = 1,
    UNLOCK = 2,
}


/// <summary>
/// 피디 등급
/// </summary>
public enum PD_GRADETYPE
{
    _1 = 1, // 교습소
    _2 = 2, // 신생 기획사
    _3 = 3, // 루키 제조소
    _4 = 4, // 대세 제작소
    _5 = 5, // 글로벌 명가
    _6 = 6, // 전설의 프로덕션
}


public enum StringTableType
{
    Global,
    StoryMode,
    IdolRoad,
}


#region 팝업 종류

public enum POPUP_TYPE
{
    NORMAL, // base  ( 취소 / 확인 )
    RIGHT, // ( 확인 )
}

#endregion

#region 방별 주소지 넘버

public enum ADDRESS
{
    NONE = 0, // 방없음
    LOBBY = 1, // 1층 로비
    REST = 2, // 1층 휴게실
    VOCAL = 3, // 2층 보컬실
    DANCE = 4, // 2층 댄스실

    // 프로토에서 추가된 방 인덱스
    ACTORS = 5, // 연기연습실
    HEALTH = 6, // 체력단련실
    HAIR = 7, // 미용실
    EXTENSION_1 = 8, // 확장룸 1
    EXTENSION_2 = 9, // 확장룸 2
    EXTENSION_3 = 10, // 확장룸 3
    //EXTENSION_4 = 11, // 확장룸 4
}

#endregion

#region 경영 쪽

/// <summary>
/// 메인 카테고리
/// </summary>
public enum MainCategory
{
    None = 0, // 카테고리 없음( 전체목록 )
    FloorProp = 1, // 바닥 장식
    FloorDeco = 2, // 바닥 소품
    WallDeco = 3, // 벽 소품
    CeilingLamp = 4, // 천장 조명
    Wallpaper = 5, // 벽지
    FloorMaterial = 6, // 장판
}

/// <summary>
/// 프랍의 테마종류
/// </summary>
public enum PropThema
{
    NONE = 0,
    MODERN = 1,
    GAME_CENTER,
    BASEMENT,
    DANCE_ROOM,
}

/// <summary>
/// 일반모드일때 카메라 값
/// </summary>
public enum NormalCamMode
{
    Normal = 0, // 시작시 뷰  , 노멀시 회전기능으로
    WALL, // 중앙에서 벽면 바라보는 뷰
    Rotation,
}


/// <summary>
/// 인테리어모드일때 카메라 값
/// </summary>
public enum InteriorCamMode
{
    TOP, // 천장서 바라보는 뷰
    WALL, // 중앙에서 벽면 바라보는 뷰
    Rotation,
}

/// <summary>
/// 배치할 곳
/// </summary>
public enum PropPlaceMent
{
    none = -1,
    floor = 0, // 바닥면
    wall = 1, // 벽면
    top = 2, // 천장
}


/// <summary>
/// 배치완료한 상태,배치대기상태(구매하고 배치안함?)
/// </summary>
public enum PropState
{
    /// <summary>
    /// 아무상태 아님
    /// </summary>
    NONE,

    /// <summary>
    /// 정상적인 배치 상태
    /// </summary>
    Occupied,

    /// <summary>
    /// 정상적인 배치 상태는 아님.
    /// </summary>
    Occupied_Err,

    /// <summary>
    /// 배치할 수 없는 위치
    /// </summary>
    Blocked,

    /// <summary>
    /// 구매는 하였지만 배치 대기 중
    /// </summary>
    Ready,

    /// <summary>
    /// 드래그 대기상태.
    /// </summary>
    DragWait,

    /// <summary>
    /// 드래그 가능
    /// </summary>
    Drag,

    /// <summary>
    /// 마우스 클릭으로 인한 선택 상태 (드래그가능은 아님)
    /// </summary>
    Clicked,
}


/// <summary>
/// 연습생 능력치 정의
/// </summary>
public enum IdolAbility
{
    vocal = 5000,
    vocal_high_notes = 5001,
    vocal_low_notes = 5002,
    vocal_tone = 5003,
    vocal_range = 5004,
    vocal_rap = 5005,
    vocal_technique = 5006,
    vocal_appeal = 5007,
    vocal_leadvocals = 5008,
    vocal_subvocals = 5009,
    vocal_mainvocals = 5010,

    dance = 6000,
    dance_flexibility = 6001,
    dance_dynamism = 6002,
    dance_creativity = 6003,
    dance_precision = 6004,
    dance_originality = 6005,
    dance_technique = 6006,
    dance_appeal = 6007,
    dance_Lead_dancer = 6008,
    dance_sub_dancer = 6009,
    dance_main_dancer = 6010,

    grit = 7000,
    grit_focus = 7001,
    grit_talent = 7002,
    grit_stamina = 7003,
    grit_mental_strength = 7004,
    grit_tenacity = 7005,
    grit_fighting_spirit = 7006,
    grit_precision = 7007,
    grit_proactiveness = 7008,
    grit_prudence = 7009,
    grit_sensitivity = 7010,

    charisma = 8000,
    charisma_leadership = 8001,
    charisma_Interpersonal_flexibility = 8002,
    charisma_adaptability = 8003,
    charisma_friendliness = 8004,
    charisma_eloquence = 8005,
    charisma_elegance = 8006,
    charisma_lyric_writing_ability = 8007,
    charisma_composition_ability = 8008,
    charisma_empathy = 8009,
    charisma_sociability = 8010,

    visual = 9000,
    visual_proportion = 9001,
    visual_glamour = 9002,
    visual_cuteness = 9003,
    visual_sexiness = 9004,
    visual_innocence = 9005,
    visual_elegance = 9006,
    visual_liveliness = 9007,
    visual_healthiness = 9008,
    visual_purity = 9009,
    visual_aura = 9010
}

#endregion

#region 캐릭터 관련 -> PD 캐릭터 , 아이돌 캐릭터

/// <summary>
/// 바뀐 파츠 구성에 따른 분류
/// </summary>
public enum PARTS
{
    TOP,
    BELOW,
    FACE,
    HAND,
    HAIR,
    SHOES,
    HAIR_EYEBROW,
    EYE_L,
    EYE_R,
}


public enum GENDERTYPE
{
    MALE = 1,
    FEMALE = 2,
}

public enum BODYSCALE
{
    BT_SMALL = 1,
    BT_NORMAL,
    BT_LARGE,
}

public enum PARTSTYPE
{
    HEAD,
    HAIR,
    HANDS,
    BODY,
    PANTS,
    SHOES,
    ACCESSORY,
    SET,

    MAX,
}

public enum SKINTYPE
{
    Skin_1 = 1,
    Skin_2,
    Skin_3,
    Skin_4,
    Skin_5,
}

public enum STAT_TYPE
{
    STAT_VOCAL,
    STAT_DANCE,
    STAT_PASSION,
    STAT_CHARM,
    STAT_VISUAL,

    MAX,
}

[Description("가창 스탯")]
public enum STAT_VOCAL_TYPE : ushort
{
    [Description("고음")] STAT_VOCAL_TREBLE = 0,
    [Description("저음")] STAT_VOCAL_BASS,
    [Description("음색")] STAT_VOCAL_TONE,
    [Description("음역대")] STAT_VOCAL_TIMBRE,
    [Description("랩")] STAT_VOCAL_LAB,
    [Description("기교")] STAT_VOCAL_TECHNIQUE,
    [Description("대중성")] STAT_VOCAL_VOCALIZATION,
    [Description("리드 보컬")] STAT_VOCAL_LEAD,
    [Description("서브 보컬")] STAT_VOCAL_SUB,
    [Description("메인 보컬")] STAT_VOCAL_MAIN,

    MAX,
}

[Description("춤 스탯")]
public enum STAT_DANCE_TYPE : ushort
{
    [Description("유연함")] STAT_DANCE_FLEXIBLE = 0,
    [Description("역동성")] STAT_DANCE_DYNIMIC,
    [Description("창의성")] STAT_DANCE_CREATIVITY,
    [Description("정확성")] STAT_DANCE_ACCURACY,
    [Description("독창성")] STAT_DANCE_ORIGINALITY,
    [Description("기교")] STAT_DANCE_FINESSE,
    [Description("대중성")] STAT_DANCE_POPULARITY,
    [Description("리드 댄서")] STAT_DANCE_LEAD,
    [Description("메인 댄서")] STAT_DANCE_MAIN,
    [Description("서브 댄서")] STAT_DANCE_SUB,

    MAX,
}

[Description("성격 스탯")]
public enum STAT_PASSION_TYPE : ushort
{
    [Description("집중성")] STAT_PASSION_FOCUS = 0,
    [Description("재능")] STAT_PASSION_TALENT,
    [Description("체력")] STAT_PASSION_STEMINA,
    [Description("정신력")] STAT_PASSION_MENTAL_STRENGTH,
    [Description("근성")] STAT_PASSION_GRIT,
    [Description("투지")] STAT_PASSION_DETERMINATION,
    [Description("정교성")] STAT_PASSION_SOPHISTICATION,
    [Description("적극성")] STAT_PASSION_PROACTIVITY,
    [Description("신중성")] STAT_PASSION_PRUDENCE,
    [Description("감지력")] STAT_PASSION_SENSITIVITY,

    MAX,
}

[Description("카리스마 스탯")]
public enum STAT_CHARM_TYPE : ushort
{
    [Description("리더쉽")] STAT_CHARM_LEADERSHIP = 0,
    [Description("유연성")] STAT_CHARM_PLIABILITY,
    [Description("융통성")] STAT_CHARM_FLEXIBILITY,
    [Description("친화력")] STAT_CHARM_AFFINITY,
    [Description("유창성")] STAT_CHARM_FLUENCY,
    [Description("기품")] STAT_CHARM_GRACIOUSNESS,
    [Description("작사능력")] STAT_CHARM_COMPOSITION,
    [Description("작곡능력")] STAT_CHARM_COMPOSITIONABILITY,
    [Description("공감력")] STAT_CHARM_EMPATHY,
    [Description("사회성")] STAT_CHARM_SOCIALIZATION,

    MAX,
}

[Description("외모 스탯")]
public enum STAT_VISUAL_TYPE : ushort
{
    [Description("프로포션")] STAT_VISUAL_PROPOSALS = 0,
    [Description("화려함")] STAT_VISUAL_SPLENDOR,
    [Description("귀여움")] STAT_VISUAL_CUTENESS,
    [Description("섹시함")] STAT_VISUAL_SEXY,
    [Description("청순함")] STAT_VISUAL_INNOCENCE,
    [Description("우아함")] STAT_VISUAL_ELEGANT,
    [Description("발랄함")] STAT_VISUAL_SPUNKY,
    [Description("건강함")] STAT_VISUAL_HEALTHY,
    [Description("순수함")] STAT_VISUAL_PURE,
    [Description("아우라")] STAT_VISUAL_AURA,

    MAX,
}

public enum IDOL_GRADE
{
    GRADE_N,
    GRADE_R,
    GRADE_SR,
    GRADE_GR,
    GRADE_UR,
    GRADE_PR,

    MAX,
}

#endregion

#region 치트 리스트

/*
 * message Cheat {
   string action = 1; // IN
   string key = 2; // IN
   string value = 3; // IN
  }
 */
public enum CHEATLIST
{
    usergender = 1,
    nickname,
    pdgrade,
    pdlevel,
    pdexp,
    cash,
    gold,
    room,
    prop,
    item,
    idol,
    pdinfo,
}

public enum CHEAT_ACTION
{
    add,
    sub,
    set
}

#endregion

#region 서버

/// <summary>
/// 결과 코드를 정의하는 열거형
/// </summary>
public enum ResultCode
{
    None = 0,
    Unknown = 9,
    Success = 200, // 성공

    // 로그인 또는 공통
    BadRequest = 400, // 잘못된 요청
    Unauthorized = 401, // 인증 실패
    Forbidden = 403, // 권한 없음
    NotFound = 404, // 찾을 수 없음
    Conflict = 409, // 충돌 (이미 존재)
    INVALID_AUTH_TOKEN = 410,
    INVALID_STATUS = 411,
    AlreadyExists = 420,
    INVALID_DATA = 430,
    Expired = 488,
    DataIsNull = 499, // 데이터 없음
    InternalServerError = 500, // 서버 오류
    InvalidStatus = 600, // 잘못된 상태

    //todo: 컨텐츠별 에러 코드 정의...

    // CreateUser -----------------------
    CREATEUSER_FAIL = 1000, // 유저 생성 실패
    CREATEUSER_DBERROR = 1001, // 유저 생성 실패(DB에러)
    CREATEUSER_ID_EXISTS = 1002, // 이미 존재하는 유저

    // PdCreate -----------------------
    PDCREATE_FAIL = 1010,
    PDCREATE_EXISTS = 1011, // PD캐릭터가 이미 존재
    PDCREATE_DUPLICATE_NICKNAME = 1012, // 닉네임 중복
    PDCREATE_INVALID_NICKNAME = 1013, // 닉네임 규칙 에러

    PDUPGRADE_FAIL = 1020, // PD승급 실패
    PDUPGRADE_NOT_ENOUGH_PDLEVEL = 1021, // PD레벨 부족
}

#endregion

#region 아이템

public enum CURRENCY_TYPE
{
    Unknown = 0,
    EXP,
    Gold,
    Cash,
}

public enum ITEM_TYPE
{
    Unknown,
    Item = 500,
    Use,
    Buff,
    Supplies,
    Material,
    Etc,
}

public enum ITEM_USEABLE_TYPE
{
    None = 0,
    Once = 1,
    Multi = 2
}

public enum ITEM_ABILITY_TYPE
{
    Unknown,
    RenamePlayer,
    PdExPBuff,
    IdolExPBuff,
    IdolAcquisitionRateBuff,
    GoldGainBuff,
    TrainingGreatSuccessRateBuff, // Stat Id로 연동하여 세부 스탯 정보를 받아온다.
    ShortenTraining, // Stat Id로 연동하여 세부 스탯 정보를 받아온다.
}

#endregion
