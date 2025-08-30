using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gbBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public static class GlobalFunction
{
    #region UI애니메이션

    /// <summary>
    /// UI 클릭시 애니메이션
    /// </summary>
    /// <param name="uiObject"></param>
    /// <param name="duration"></param>
    /// <param name="action"></param>
    public static void ClickUI_TypeA_Animation(GameObject uiObject, float duration = 0.25f, Action action = null)
    {
        if (DOTween.IsTweening(uiObject)) return;

        Vector3 temp = uiObject.transform.localScale;

        // DOTween으로 변경
        uiObject.transform.DOScale(temp * 1.1f, duration * 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                uiObject.transform.DOScale(temp, duration * 0.3f)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => action?.Invoke());
            });
    }

    /// <summary>
    /// Open 시
    /// </summary>
    /// <param name="uiObject"></param>
    /// <param name="duration"></param>
    public static void OpenUIWithAnimation(GameObject uiObject, float duration = 0.25f, Action action = null)
    {
        if (Application.isPlaying == false) return;
        if (DOTween.IsTweening(uiObject)) return;

        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }

        // 초기 설정
        canvasGroup.alpha = 0f;
        uiObject.transform.localScale = Vector3.one;
        uiObject.SetActive(true);

        //SoundManager.Instance.Play_SFX(SOUND_SFX.popup_open);

        // DOTween으로 변경
        canvasGroup.DOFade(1f, duration).SetEase(Ease.InOutQuad);

        uiObject.transform.DOScale(Vector3.one * 1.1f, duration * 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                uiObject.transform.DOScale(Vector3.one, duration * 0.3f)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => action?.Invoke());
            });
    }

    /// <summary>
    /// Close 시
    /// </summary>
    /// <param name="uiObject"></param>
    /// <param name="duration"></param>
    /// <param name="onComplete"></param>
    public static void CloseUIWithAnimation(GameObject uiObject, float duration = 0.2f, Action onComplete = null)
    {
        if (Application.isPlaying == false) return;
        if (DOTween.IsTweening(uiObject)) return;

        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }

        //SoundManager.Instance.Play_SFX(SOUND_SFX.popup_open);

        // DOTween으로 변경
        canvasGroup.DOFade(0f, duration).SetEase(Ease.InOutQuad);

        uiObject.transform.DOScale(Vector3.one * 1.1f, duration * 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                uiObject.transform.DOScale(Vector3.one, duration * 0.7f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        onComplete?.Invoke();
                        uiObject.SetActive(false);
                    });
            });
    }

    public static async UniTask OnAnim(Animation anim, string animationClip, Action finishAction = null)
    {
        anim.Play(animationClip);
        await UniTask.WaitWhile(() => anim.isPlaying);
        finishAction?.Invoke();
    }
    
    public static void CloseUIWithAnimation_ScaleOnly(GameObject uiObject, float duration = 0.2f, Action onComplete = null)
    {
        if (Application.isPlaying == false) return;
        if (DOTween.IsTweening(uiObject)) return;

        uiObject.transform.DOScale(Vector3.one * 1.1f, duration * 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                uiObject.transform.DOScale(Vector3.one, duration * 0.7f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        onComplete?.Invoke();
                        uiObject.SetActive(false);
                    });
            });
    }
    #endregion

    public static void BindButton(Transform transform, string path, UnityAction onClick)
    {
        var btn = transform.Find(path)?.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    /*public static string Get_Prefs_UserID()
    {
        if (GameMain.Instance.connectServer == Server.LOCAL)
            return GlobalDefine.DEF_PREFS_LOCAL_USER_ID;
        else
            return GlobalDefine.DEF_PREFS_USER_ID;
    }*/

    public static string GetPathFile(string filename)
    {
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                string documentsPath = Path.Combine(Application.dataPath, "..", "Documents"); // iOS Documents 경로
                return Path.Combine(documentsPath, filename);

            case RuntimePlatform.Android:
                string filesPath = Path.Combine(Application.persistentDataPath, "files");
                Directory.CreateDirectory(filesPath); // files 디렉토리 생성
                return Path.Combine(filesPath, filename);

            default:
                // Editor 또는 다른 플랫폼
                string dataPath = Application.dataPath;
                return Path.Combine(dataPath, filename);
        }
    }

    public static string getTablePath()
    {
        string path = string.Empty;

        /*
        switch (GameMain.Instance.platform)
        {
            case PLATFORM.IOS:
                path = "IOS";
                break;
            case PLATFORM.GOOGLE:
                path = "GOOGLE";
                break;
            case PLATFORM.ONESTORE:
                path = "ONESTORE";
                break;
        }
        */

        return path;
    }

    /*public static string GetString(int index, params object[] args)
    {
        return GetString(StringTableType.Global, index, args);
    }

    public static string GetString(StringTableType tableType, int index, params object[] args)
    {
        return TableManager.Instance.StringMng.GetString(tableType, index, args);
    }*/

    public static string SetString(string text, params object[] args)
    {
        if (args.Length > 0)
        {
            switch (args.Length)
            {
                case 1:
                    text = string.Format(text, args[0]);
                    break;
                case 2:
                    text = string.Format(text, args[0], args[1]);
                    break;
                case 3:
                    text = string.Format(text, args[0], args[1], args[2]);
                    break;
                case 4:
                    text = string.Format(text, args[0], args[1], args[2], args[3]);
                    break;
                case 5:
                    text = string.Format(text, args[0], args[1], args[2], args[3], args[4]);
                    break;
            }
        }

        return text;
    }

    public static void ShowMessage(string text, params object[] args)
    {
        if (args.Length > 0)
        {
            switch (args.Length)
            {
                case 1:
                    text = string.Format(text, args[0]);
                    break;
                case 2:
                    text = string.Format(text, args[0], args[1]);
                    break;
                case 3:
                    text = string.Format(text, args[0], args[1], args[2]);
                    break;
                case 4:
                    text = string.Format(text, args[0], args[1], args[2], args[3]);
                    break;
                case 5:
                    text = string.Format(text, args[0], args[1], args[2], args[3], args[4]);
                    break;
            }
        }

       // UIManager.Instance.Get<SystemNotification>().ShowMessage(text);
    }

    public static void ShowIndicator(string _key)
    {
     //   UIManager.Instance.UIIndicator.Show(_key);
    }

    public static void HideIndicator(string _key)
    {
    //    UIManager.Instance.UIIndicator.Hide(_key);
    }

    
    
    public static string ReadCSV(int idx, LANGGUAGE language, StringTableType tableType)
    {
        string findText = string.Empty;
        string tableName = tableType switch
                           {
                               StringTableType.Global    => "string_global",
                               StringTableType.StoryMode => "string_storymode",
                               StringTableType.IdolRoad  => "string_idolroad",
                               _                         => "string_global",
                           };
        string path = Application.dataPath + $"/51_Resources/Resources/Table/{tableName}.csv";
        try
        {
            if (File.Exists(path) == false)
            {
                Debug.Log("File Not Found!!!");
                return string.Empty;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, false))
                {
                    string lines = null;
                    string[] keys = null;

                    int noRows = 0;
                    int headerIdx = 0;
                    string _stridx = idx.ToString();
                    while ((lines = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(lines)) return string.Empty;

                        // Header 체크
                        if (noRows++ == 0)
                        {
                            string[] cols = lines.Split(',');
                            string targetHeader = language switch
                                                  {
                                                      LANGGUAGE.KR => "kr",
                                                      LANGGUAGE.US => "us",
                                                      LANGGUAGE.JP => "jp",
                                                      _            => "kr",
                                                  };
                            for (int i = 0; i < cols.Length; ++i)
                            {
                                if (!cols[i].Equals(targetHeader)) continue;
                                headerIdx = i;
                                break;
                            }

                            continue;
                        }

                        var values = new List<string>();
                        bool inQuotes = false;
                        string currentValue = "";

                        foreach (var c in lines)
                        {
                            if (c == '\"')
                            {
                                // 큰따옴표 시작/종료 처리
                                inQuotes = !inQuotes;
                            }
                            else if (c == ',' && !inQuotes)
                            {
                                // 쉼표를 만나면 현재 값을 저장하고 초기화
                                values.Add(currentValue.Replace("\\n", "\n"));
                                currentValue = "";
                            }
                            else
                            {
                                currentValue += c;
                            }
                        }

                        // 마지막 값 추가
                        values.Add(currentValue.Replace("\\n", "\n"));

                        if (values[0] != _stridx || values.Count <= headerIdx) continue;

                        findText = values[headerIdx];
                        break;
                    }

                    sr.Close();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        return findText;
    }
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// CSV를 한 번 읽어서 "문구 → 인덱스" 맵을 만듭니다(에디터 전용).
    /// </summary>
    public static Dictionary<string, int> BuildStringToIndexMap(LANGGUAGE language, StringTableType tableType)
    {
        var map = new Dictionary<string, int>();

        string tableName = tableType switch
        {
            StringTableType.Global    => "string_global",
            StringTableType.StoryMode => "string_storymode",
            StringTableType.IdolRoad  => "string_idolroad",
            _                         => "string_global",
        };

        string path = Application.dataPath + $"/51_Resources/Resources/Table/{tableName}.csv";
        if (!File.Exists(path))
        {
            Debug.LogWarning($"CSV Not Found: {path}");
            return map;
        }

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8, false);

            string? line;
            int row = 0;
            int langCol = -1;

            // 헤더
            if ((line = sr.ReadLine()) != null)
            {
                row++;
                var header = line.Split(',');
                string target = language switch
                {
                    LANGGUAGE.KR => "kr",
                    LANGGUAGE.US => "us",
                    LANGGUAGE.JP => "jp",
                    _            => "kr",
                };

                for (int i = 0; i < header.Length; i++)
                {
                    if (header[i].Equals(target))
                    {
                        langCol = i;
                        break;
                    }
                }

                if (langCol < 0)
                {
                    Debug.LogWarning($"헤더에서 언어 컬럼({target})을 찾지 못했습니다.");
                    return map;
                }
            }

            // 데이터
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;

                var values = ParseCsvLine(line);
                if (values.Count == 0) continue;

                // 0번이 인덱스라고 가정
                if (!int.TryParse(values[0], out int idx)) continue;
                if (values.Count <= langCol) continue;

                string text = Normalize(values[langCol]);
                if (string.IsNullOrEmpty(text)) continue;

                // 중복 문구는 첫 항목만 사용(원하시면 충돌 리포트/선택 로직 추가)
                if (!map.ContainsKey(text))
                    map.Add(text, idx);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        return map;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        var sb = new StringBuilder();

        foreach (var c in line)
        {
            if (c == '\"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes)
            {
                values.Add(sb.ToString().Replace("\\n", "\n"));
                sb.Clear();
            }
            else { sb.Append(c); }
        }
        values.Add(sb.ToString().Replace("\\n", "\n"));
        return values;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\r\n", "\n").Trim();
    }
    
    #endif

    public static void MakeSceneDirty(GameObject source, string sourceName)
    {
        if (Application.isPlaying == false)
        {
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(source, sourceName);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorWindow.GetWindow<SceneView>().Repaint();
#endif
        }
    }

    public static T AddOrGetComponent<T>(GameObject gameObject) where T : Component
    {
        if (gameObject == null) return null;

        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    public static T AddOrGetComponent<T>(Transform trans, string childPath) where T : Component
    {
        if (trans == null) return null;

        GameObject gameObject = trans.Find(childPath)?.gameObject;

        return gameObject == null ? null : AddOrGetComponent<T>(gameObject);
    }

    public static T AddOrGetComponent<T>(GameObject gameObject, string childPath) where T : Component
    {
        if (gameObject == null) return null;

        return AddOrGetComponent<T>(gameObject.transform, childPath);
    }

    /// <summary>
    /// 레이어 변경.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="layerName"></param>
    public static void SetLayerRecursively(GameObject obj, string layerName)
    {
        int newLayer = LayerMask.NameToLayer(layerName);
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layerName);
    }

    /// <summary>
    /// 디파인 테이블의 값을 가져온다.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /*public static T GetDefineConst<T>(string id)
    {
        return TableManager.Instance.Get<Table_Define_Const>().GetValue<T>(id);
    }*/

  
    

   

    public static T Deserialize<T>(string data)
    {
        T t = default(T);
        try
        {
            t = JsonConvert.DeserializeObject<T>(data);
        }
        catch (JsonSerializationException ex)
        {
            Debug.LogError("JSON 역직렬화 오류: " + ex.Message);
        }

        return t;
    }

    public static DateTime DeserializeToDateTime(JObject json, string key, DateTime defaultValue)
    {
        if (!json.ContainsKey(key) || json[key] == null || json[key].Type == JTokenType.Null)
            return defaultValue;

        try
        {
            return DateTime.Parse((string)json[key]);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"{key} 날짜 파싱 오류: {ex.Message}");

            return defaultValue;
        }
    }

    #region 금지어 필더 기능

    /*private static AhoCorasick _slangFilter = new();
    private static bool _isFilterInitialized = false;

    // 금지어 리스트 초기화 함수 (한 번만 호출)
    public static void InitSlangFilter(List<string> slangList)
    {
        if (_isFilterInitialized) return;

        _slangFilter.Build(slangList);
        _isFilterInitialized = true;
    }

    // 금지어 포함 여부 확인
    public static bool ContainsSlang(string input)
    {
        if (!_isFilterInitialized)
        {
            Debug.LogWarning("금지어 필터가 초기화되지 않았습니다. InitSlangFilter() 먼저 호출하세요.");
            return false;
        }

        return _slangFilter.ContainsBannedWord(input);
    }*/

    #endregion

    public static DeviceType IsEmulator()
    {
        // 에디터에서는 무조건 false
#if UNITY_EDITOR
        return DeviceType.EDITOR;
#endif

        string processor = SystemInfo.processorType;
        // 실제 안드로이드 디바이스: "ARMv7", "ARM64", "Qualcomm Snapdragon" 등
        // 대부분의 에뮬레이터: "Intel", "AMD", "x86", "x86_64" 등

        if (processor.ToLower().Contains("intel") ||
            processor.ToLower().Contains("amd") ||
            processor.ToLower().Contains("x86"))
        {
            GlobalFunction.ShowMessage("x86 기반 - 에뮬레이터일 가능성 높음");
            return DeviceType.EMULATOR;
        }
        else
        {
            GlobalFunction.ShowMessage("실제 디바이스");
            return DeviceType.MOBILE;
        }

        return DeviceType.EDITOR;
    }

    public static void SetStretchAll(this RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = VVector2.GetVec2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    
    public static T LoadPrefab<T>(Transform parent, string path) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        // 기존 자식들에서 T 컴포넌트 찾기
        T script = null;
        for (int i = 0; i < parent.childCount; ++i)
        {
            Transform child = parent.GetChild(i);
            script = child.GetComponent<T>();
            if (script != null)
                return script;
        }

        // 기존에 없다면 새로 로드해서 생성
        GameObject newObj = ResourceLoader.GetInstance(ResourceLoader.DATA.UI, path);
        if (newObj != null)
        {
            newObj.transform.SetParent(parent);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;

            script = AddOrGetComponent<T>(newObj);
        }

        return script;
    }

    public static T ParseEnum<T>(string _value, T _default) where T : Enum
    {
        if (Enum.TryParse(typeof(T), _value, out object result))
            return (T)result;
        else
            return _default;
    }

    public static bool TryParseEnum<T>(string _value, T _default, out T _outValue) where T : Enum
    {
        if (Enum.TryParse(typeof(T), _value, out object result))
        {
            _outValue = (T)result;
            return true;
        }

        _outValue = _default;
        return false;
    }
}
