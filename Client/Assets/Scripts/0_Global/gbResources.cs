using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

// relation gobees	: gbDebug

namespace gbBase
{
    static public class ResourceLoader
    {
        public enum TYPE
        {
            RESOURCES = 0,
            ASSETBUNDLE,
            ASSETBUNDLE_STREAMING,
        }

        public enum LANGUAGE
        {
            NONE = -1,
            KR = 0,
            EN = 1,
            JP = 2, // 일어
            TC = 3, // 대만
            TH = 4, // 태국
            VN = 5, // 베트남
            ID = 6, // 인니
        }

        public enum DATA // 나열된 순서가 로드되는 순서.
        {
            NONE,
            PREFAB,
            TABLE,
            UI,
            EFFECT,
            TEXTURE,
            SOUND,
            MANAGEMENT,
            WORLD,
            CHARACTER,
            ANIMATION,
        }

        static private string[] path =
        {
            "",
            "Prefab/",
            "Table/",
            "UI/",
            "Effect/",
            "Texture/",
            "Sound/",
            "Management/",
            "World/",
            "Character/",
            "Animation/",
        };

        public enum CHAR_DATA
        {
            FACE = 0,
            HANDS = 1,
            HAIR = 2,
            BODY = 3,
            PANTS = 4,
            SHOES = 5,
            ACCESSORY = 6,
            SET = 7,
        };

        static private string[] subpath =
        {
            "01_Face/",
            "02_Hand/",
            "03_Hair/",
            "04_Body/",
            "05_Pants/",
            "06_Shoes/",
            "07_Accessory/",
            "08_Set/",
        };

        static public TYPE type = TYPE.RESOURCES;

        static public LANGUAGE language
        {
            set
            {
                PlayerPrefs.SetInt(GlobalDefine.DEF_PREFS_LANGUAGE, (int)value);
                _Language = value;
            }

            get
            {
                if (_Language != LANGUAGE.NONE)
                    return _Language;

                int language = -1;
                if (PlayerPrefs.HasKey(GlobalDefine.DEF_PREFS_LANGUAGE))
                {
                    language = PlayerPrefs.GetInt(GlobalDefine.DEF_PREFS_LANGUAGE);
                }

                if (language != (int)LANGUAGE.NONE)
                {
                    _Language = (LANGUAGE)language;
                }
                else
                {
                    switch (Application.systemLanguage)
                    {
                        case SystemLanguage.Korean:
                            _Language = LANGUAGE.KR;
                            break;
                        case SystemLanguage.Japanese:
                            _Language = LANGUAGE.JP;
                            break;
                        case SystemLanguage.Thai: // 태국어
                            _Language = LANGUAGE.TH;
                            break;
                        case SystemLanguage.ChineseTraditional: // 대만
                            _Language = LANGUAGE.TC;
                            break;
                        case SystemLanguage.Indonesian: // 인도네시아어
                            _Language = LANGUAGE.ID;
                            break;
                        case SystemLanguage.Vietnamese: // 베트남
                            _Language = LANGUAGE.VN;
                            break;
                        default:
                            _Language = LANGUAGE.EN;
                            break;
                    }
                }

                ReloadFont(_Language);

                return _Language;
            }
        }

        static private LANGUAGE _Language = LANGUAGE.NONE;

        public static void ReloadFont(LANGUAGE lang)
        {
            SetFont("Font/Ref_NotoSans-Regular", GetFontPathRegular(lang));
            SetFont("Font/Ref_NotoSans-Medium", GetFontPathMedium(lang));
            SetFont("Font/Ref_NotoSans-Black", GetFontPathBlack(lang));
        }

        private static void SetFont(string refFontName, string newFontName)
        {
            GameObject ob = GetPrefab(DATA.UI, refFontName) as GameObject;
            GameObject ob2 = GetPrefab(DATA.UI, newFontName) as GameObject;
            if (ob != null && ob2 != null)
            {
                /*
                UIFont refFont = ob.GetComponent<UIFont>();
                UIFont newFont = ob2.GetComponent<UIFont>();
                if (refFont != null && newFont != null)
                    refFont.replacement = newFont;
                    */
            }
        }


        private static string GetFontPathRegular(LANGUAGE lang)
        {
            switch (lang.ToString())
            {
                case "KR":
                    return "Font/NotoSansKR-Regular";
                case "JP":
                    return "Font/NotoSansJP-Medium";
                case "TC":
                    return "Font/NotoSansTC-Regular";
                case "TH":
                    return "Font/NotoSansTH-Regular";
                case "IR":
                    return "Font/Iran_BYekan";
                default:
                    return "Font/NotoSans-Regular";
            }
        }

        private static string GetFontPathMedium(LANGUAGE lang)
        {
            switch (lang.ToString())
            {
                case "KR":
                    return "Font/NotoSansKR-Medium";
                case "JP":
                    return "Font/NotoSansJP-Medium";
                case "TC":
                    return "Font/NotoSansTC-Medium";
                case "TH":
                    return "Font/NotoSansTH-Medium";
                case "IR":
                    return "Font/Iran_BYekan";
                default:
                    return "Font/NotoSans-Medium";
            }
        }

        private static string GetFontPathBlack(LANGUAGE lang)
        {
            switch (lang.ToString())
            {
                case "KR":
                    return "Font/NotoSansKR-Black";
                case "JP":
                    return "Font/NotoSansJP-Black";
                case "TC":
                    return "Font/NotoSansTC-Black";
                case "TH":
                    return "Font/NotoSansTH-Bold";
                case "IR":
                    return "Font/Iran_BYekan";
                default:
                    return "Font/NotoSans-Bold";
            }
        }

        public static Object GetPrefab(DATA data, string prefabName)
        {
#if BUNDLE
			if (PatchManager.Instance.patchload.myPatchTable != null &&
				PatchManager.Instance.isContainKey(prefabName))
			{
				return PatchManager.Instance.Load_File_Sync<Object>(prefabName);
			}
#endif
            int pathIdx = (int)data;
            string filePath = path[pathIdx] + prefabName;

            switch (type)
            {
                case TYPE.RESOURCES:
                    return Resources.Load(filePath);

                case TYPE.ASSETBUNDLE:
                    break;

                case TYPE.ASSETBUNDLE_STREAMING:
                    break;
            }

            Debug.LogErrorFormat("ERROR : RESOURCE LOAD FAIL - {0}", prefabName);

            return null;
        }


        public static ResourceRequest GetPrefabAsync(DATA data, string prefabName)
        {
            int pathIdx = (int)data;
            string filePath = string.Empty;

            ResourceRequest request = null;

            // 테이블일시 경로 수정.
            if (data == DATA.TABLE)
            {
                filePath = path[pathIdx] + GlobalFunction.getTablePath() + prefabName;
                request = Resources.LoadAsync(filePath);

                // 서버/스토어 폴더로 분리 위치.
                if (request == null || request.asset == null)
                {
                    filePath = string.Empty;
                    filePath = path[pathIdx] + prefabName;
                    request = Resources.LoadAsync(filePath);
                    if (request != null)
                        return request;
                }

                // 기본 위치.
                if (request == null || request.asset == null)
                {
                    filePath = string.Empty;
                    filePath = path[pathIdx] + prefabName;
                    request = Resources.LoadAsync(filePath);
                    if (request != null)
                        return request;
                }
            }
            else
            {
                filePath = path[pathIdx] + prefabName;
            }

            switch (type)
            {
                case TYPE.RESOURCES:
                    return Resources.LoadAsync(filePath);

                case TYPE.ASSETBUNDLE:
                    break;

                case TYPE.ASSETBUNDLE_STREAMING:
                    break;
            }

            Debug.LogErrorFormat("ERROR : RESOURCE LOAD FAIL - {0}", prefabName);

            return null;
        }

        public static GameObject GetInstance(DATA data, string prefabName)
        {
            Object prefab = GetPrefab(data, prefabName);
            if (prefab != null)
            {
                GameObject instance = GameObject.Instantiate(prefab) as GameObject;

                if (instance != null)
                {
#if BUNDLE
				    if (Application.platform == RuntimePlatform.WindowsEditor)
				    {
				        if (data == DATA.EFFECT && instance.GetComponent<ReApplyShaders>() == null)
				            instance.AddComponent<ReApplyShaders>();
                    }
#endif
                    return instance;
                }

                Debug.LogErrorFormat("ERROR : INSTANTIATE FAIL - {0}", prefabName);
            }

            return null;
        }

        public static GameObject GetInstance(Object asset)
        {
            if (asset != null)
            {
                GameObject instance = GameObject.Instantiate(asset) as GameObject;
                if (instance == null)
                {
                    Debug.LogError(string.Format("ERROR : INSTANTIATE FAIL - {0}", asset.name));
                }

                return instance;
            }

            return null;
        }

        /// <summary>
        /// UniTask를 사용한 비동기 프리팹 로드
        /// </summary>
        /// <param name="data">데이터 타입</param>
        /// <param name="prefabName">프리팹 이름</param>
        /// <returns>로드된 Object</returns>
        public static async Cysharp.Threading.Tasks.UniTask<Object> GetPrefabAsync2(DATA data, string prefabName)
        {
#if BUNDLE
    if (PatchManager.Instance.patchload.myPatchTable != null &&
        PatchManager.Instance.isContainKey(prefabName))
    {
        return PatchManager.Instance.Load_File_Sync<Object>(prefabName);
    }
#endif

            int pathIdx = (int)data;
            string filePath = string.Empty;

            // 테이블일시 경로 수정.
            if (data == DATA.TABLE)
            {
                filePath = path[pathIdx] + GlobalFunction.getTablePath() + prefabName;
                var request = Resources.LoadAsync(filePath);
                await request.ToUniTask();

                // 서버/스토어 폴더로 분리 위치.
                if (request.asset == null)
                {
                    filePath = path[pathIdx] + prefabName;
                    request = Resources.LoadAsync(filePath);
                    await request.ToUniTask();

                    if (request.asset != null)
                        return request.asset;
                }
                else
                {
                    return request.asset;
                }

                // 기본 위치.
                if (request.asset == null)
                {
                    filePath = path[pathIdx] + prefabName;
                    request = Resources.LoadAsync(filePath);
                    await request.ToUniTask();

                    if (request.asset != null)
                        return request.asset;
                }
            }
            else
            {
                filePath = path[pathIdx] + prefabName;
            }

            switch (type)
            {
                case TYPE.RESOURCES:
                    var resourceRequest = Resources.LoadAsync(filePath);
                    await resourceRequest.ToUniTask();
                    return resourceRequest.asset;

                case TYPE.ASSETBUNDLE:
                    break;

                case TYPE.ASSETBUNDLE_STREAMING:
                    break;
            }

            Debug.LogErrorFormat("ERROR : RESOURCE LOAD FAIL - {0}", prefabName);
            return null;
        }

        /// <summary>
        /// UniTask를 사용한 비동기 인스턴스 생성
        /// </summary>
        /// <param name="data">데이터 타입</param>
        /// <param name="prefabName">프리팹 이름</param>
        /// <returns>생성된 GameObject</returns>
        public static async UniTask<GameObject> GetInstanceAsync(DATA data, string prefabName)
        {
            Object prefab = await GetPrefabAsync2(data, prefabName);
            return GetInstance(prefab);
        }

        /// <summary>
        /// UniTask를 사용한 비동기 인스턴스 생성 (Object에서)
        /// </summary>
        /// <param name="asset">인스턴스화할 Object</param>
        /// <returns>생성된 GameObject</returns>
        public static async UniTask<GameObject> GetInstanceAsync(Object asset)
        {
            // 메인 스레드에서 실행되어야 하는 작업이므로 UniTask.Yield()로 프레임을 양보
            await UniTask.Yield();

            if (asset != null)
            {
                GameObject instance = GameObject.Instantiate(asset) as GameObject;
                if (instance == null)
                {
                    Debug.LogError(string.Format("ERROR : INSTANTIATE FAIL - {0}", asset.name));
                }

                return instance;
            }

            return null;
        }

        /// <summary>
        /// UniTask를 사용한 비동기 캐릭터 인스턴스 생성
        /// </summary>
        /// <param name="_chardata">캐릭터 데이터 타입</param>
        /// <param name="prefabName">프리팹 이름</param>
        /// <returns>생성된 GameObject</returns>
        public static async Cysharp.Threading.Tasks.UniTask<GameObject> GetInstanceCharacterAsync(CHAR_DATA _chardata,
            string prefabName)
        {
            string subpathName = subpath[(int)_chardata] + prefabName;
            Object prefab = await GetPrefabAsync(DATA.CHARACTER, subpathName);

            if (prefab != null)
            {
                GameObject instance = GameObject.Instantiate(prefab) as GameObject;

                if (instance != null)
                {
#if BUNDLE
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (instance.GetComponent<ReApplyShaders>() == null)
                    instance.AddComponent<ReApplyShaders>();
            }
#endif
                    return instance;
                }

                Debug.LogErrorFormat("ERROR : INSTANTIATE FAIL - {0}", prefabName);
            }

            return null;
        }


        public static GameObject GetInstanceCharacter(CHAR_DATA _chardata, string prefabName)
        {
            //subpath
            string subpathName = subpath[(int)_chardata] + prefabName;

            Object prefab = GetPrefab(DATA.CHARACTER, subpathName);
            if (prefab != null)
            {
                GameObject instance = GameObject.Instantiate(prefab) as GameObject;

                if (instance != null)
                {
#if BUNDLE
				    if (Application.platform == RuntimePlatform.WindowsEditor)
				    {
				        if (data == DATA.EFFECT && instance.GetComponent<ReApplyShaders>() == null)
				            instance.AddComponent<ReApplyShaders>();
                    }
#endif
                    return instance;
                }

                Debug.LogErrorFormat("ERROR : INSTANTIATE FAIL - {0}", prefabName);
            }

            return null;
        }

        /// <summary>
        /// 테이블 스트링을 가져오기 위한 메소드.
        /// </summary>
        /// <param name="key">스트링 테이블 인덱스</param>
        /// <returns></returns>
        /*public static string GetString(string key)
        {
            TableDefineString tableDefineString = TableManager.Instance.Get<TableDefineString>();
            try
            {
                string msg = tableDefineString.GetValue(key);

                if (string.IsNullOrEmpty(msg))
                    return "";
                else
                {
                    msg = msg.Replace("\\n", "\n");
                    return msg;
                }
            }
            catch (System.NullReferenceException e)
            {
                Debug.LogError(e.ToString());
                return "Empty";
            }
        }*/
    }
}
