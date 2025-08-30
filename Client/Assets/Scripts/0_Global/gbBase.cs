using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace gbBase
{
    public interface IScene
    {
        void Activity(params object[] objs);

        void Deactivity();
    }

    public class GlobalBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        static private T instance = null;

        static public T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<T>(true);
                    if (instance == null)
                    {
                        string typeString = typeof(T).ToString();
                        GameObject go = new GameObject(typeString);
                        instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }
    }

    public class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        static private T instance = null;

        static public T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<T>();
                    if (instance == null)
                    {
                        string typeString = typeof(T).Name;

                        UnityEngine.Object obj = ResourceLoader.GetPrefab(ResourceLoader.DATA.PREFAB, typeString);
                        if (obj != null)
                        {
                            GameObject go = Instantiate(obj) as GameObject;
                            go.name = typeString;
                            instance = go.GetComponent<T>();
                            if (instance == null)
                            {
                                Debug.LogWarningFormat("WARNING : PREFAB NOT FOUND - {0}", typeString);
                                instance = go.AddComponent<T>();
                            }
                        }
                        else
                        {
                            GameObject go = new GameObject(typeString);
                            instance = go.AddComponent<T>();
                        }
                    }

                    if (Application.isPlaying) DontDestroyOnLoad(instance.gameObject);
                }

                return instance;
            }
        }

        static public bool Instantiated
        {
            get
            {
                if (instance == null)
                    return false;

                return true;
            }
        }

        static public void DestroySelf()
        {
            if (instance == null)
                return;

            GameObject.Destroy(instance);
        }
    }

    public class CSVLoader
    {
        bool _loaded;
        List<string> _cols = null;
        List<string[]> _rows = null;

        char crypt = (char)69;
        int _noCols = 0;

        string message;

        public int Cols
        {
            get { return _noCols; }
        }

        public List<string> colsList
        {
            get { return _cols; }
        }

        public int Rows
        {
            get
            {
                if (_rows == null) return 0;

                return _rows.Count;
            }
        }

        public CSVLoader()
        {
            _loaded = false;
            _rows = null;
        }

        // 컬럼 인덱스 찾기 (현재는 TableBuildingCnt 에서만 사용)
        public int GetColIndex(string colName)
        {
            if (_cols == null || _cols.Contains(colName) == false)
                return -1;

            return _cols.IndexOf(colName);
        }

        public bool ReadValue(int col, int row, byte def, out byte Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = 0;
            else
                Value = byte.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, short def, out short Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = 0;
            else
                Value = short.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, ushort def, out ushort Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = 0;
            else
                Value = ushort.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, long def, out long Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = 0;
            else
                Value = long.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, bool def, out bool Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "Y" ||
                _rows[row][col].ToLower() == "true")
                Value = true;
            else
                Value = false;
            //Value = bool.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, int def, out int Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = def;
            else
            {
                try
                {
                    Value = int.Parse(_rows[row][col]);
                }
                catch (FormatException e)
                {
                    Debug.LogError("Parse Error");
                    Debug.LogError(e.Message);
                }
            }


            return true;
        }

        public bool ReadValue(int col, int row, uint def, out uint Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = def;
            else
                Value = uint.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, ulong def, out ulong Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = def;
            else
                Value = uint.Parse(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, string def, out string Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            Value = _rows[row][col];

            return true;
        }

        public bool ReadValue(int col, int row, float def, out float Value)
        {
            Value = def;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                Value = def;
            else
                Value = float.Parse(_rows[row][col]);
            //Value = System.Convert.ToSingle(_rows[row][col]);

            return true;
        }

        public bool ReadValue(int col, int row, string def, ref string[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new string[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = arrString[i];
            }

            return true;
        }

        public bool ReadValue(int col, int row, byte def, ref byte[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new byte[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = byte.Parse(arrString[i]);
            }

            return true;
        }

        public bool ReadValue(int col, int row, int def, ref int[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new int[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = int.Parse(arrString[i]);
            }

            return true;
        }

        public bool ReadValue(int col, int row, uint def, ref uint[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new uint[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = uint.Parse(arrString[i]);
            }

            return true;
        }

        public bool ReadValue(int col, int row, ushort def, ref ushort[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new ushort[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = ushort.Parse(arrString[i]);
            }

            return true;
        }


        public bool ReadValue(int col, int row, float def, ref float[] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            if (Value == null) Value = new float[arrString.Length];
            for (int i = 0; i < Value.Length; i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i] = def;
                    continue;
                }

                if (arrString[i] == "" || arrString[i] == null)
                    Value[i] = def;
                else
                    Value[i] = float.Parse(arrString[i]);
            }

            return true;
        }

        public bool ReadValue(int col, int row, int def, ref int[,] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(',');
            string[] subarrString = arrString[0].Split(':');
            if (Value == null) Value = new int[arrString.Length, subarrString.Length];
            for (int i = 0; i < Value.GetLength(0); i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i, 0] = def;
                    Value[i, 1] = def;
                    continue;
                }

                string[] _arrString = arrString[i].Split(':');

                for (int j = 0; j < Value.Rank; j++)
                {
                    if (_arrString.Length <= j)
                    {
                        Value[i, j] = def;
                        continue;
                    }

                    if (_arrString[j] == "" || _arrString[j] == null)
                        Value[i, j] = def;
                    else
                        Value[i, j] = int.Parse(_arrString[j]);
                }
            }

            return true;
        }

        public bool ReadValue(int col, int row, float def, ref float[,] Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(' ');

            for (int i = 0; i < Value.GetLength(0); i++)
            {
                if (arrString.Length <= i)
                {
                    Value[i, 0] = def;
                    Value[i, 1] = def;
                    continue;
                }

                string[] _arrString = arrString[i].Split(':');

                for (int j = 0; j < Value.Rank; j++)
                {
                    if (_arrString.Length <= j)
                    {
                        Value[i, j] = def;
                        continue;
                    }

                    if (_arrString[j] == "" || _arrString[j] == null)
                        Value[i, j] = def;
                    else
                        Value[i, j] = float.Parse(_arrString[j]);
                }
            }

            return true;
        }

        public bool ReadValue(int col, int row, float def, out Vector3 Value)
        {
            Value = Vector3.zero;

            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            string[] arrString = _rows[row][col].Split(':');
            for (int i = 0; i < 3; i++)
            {
                if (arrString.Length <= i)
                    continue;

                if (arrString[i] == "" || arrString[i] == null)
                {
                    if (i == 0)
                        Value.x = def;
                    else if (i == 1)
                        Value.y = def;
                    else if (i == 2)
                        Value.z = def;
                }
                else
                {
                    if (i == 0)
                        Value.x = float.Parse(arrString[i]);
                    else if (i == 1)
                        Value.y = float.Parse(arrString[i]);
                    else if (i == 2)
                        Value.z = float.Parse(arrString[i]);
                }
            }

            return true;
        }

        public bool ReadValue(int col, int row, ushort def, ref ArrayList Value)
        {
            if (!_loaded)
                return false;

            if (_rows == null)
                return false;

            if (row >= _rows.Count)
                return false;

            if (col >= _noCols)
                return false;

            if (_rows[row][col] == "" || _rows[row][col] == null)
                return false;

            string[] arrString = _rows[row][col].Split(':');

            for (int i = 0; i < arrString.Length; i++)
            {
                ushort val = ushort.Parse(arrString[i]);
                Value.Add(val);
            }

            return true;
        }

        public bool ReadValue<TEnum>(int col, int row, TEnum def, out TEnum value) where TEnum : struct
        {
            value = def;

            if (ReadValue(col, row, "", out string enumString))
            {
                if (Enum.TryParse(enumString, out TEnum _type))
                {
                    value = (TEnum)_type;
                    return true;
                }
            }
            return false;
        }

        public bool SecuredLoadFromFile(string pathName)
        {
            try
            {
                _noCols = 0;

                FileStream fs = File.Open(pathName, FileMode.Open, FileAccess.Read);
                BinaryReader r = new BinaryReader(fs);

                int rowCount = r.ReadInt32();
                _cols = new List<string>();
                _rows = new List<string[]>();

                int colCount = 0;
                for (int i = 0; i < rowCount; ++i)
                {
                    colCount = r.ReadInt32();
                    string[] cols = new string[colCount];

                    for (int j = 0; j < colCount; ++j)
                    {
                        int length = r.ReadInt32();
                        if (length > 0)
                        {
                            char[] stringArray = r.ReadChars(length);

                            for (int k = 0; k < length; ++k)
                                stringArray[k] ^= crypt;

                            cols[j] = new string(stringArray, 0, length);
                        }
                        else
                        {
                            cols[j] = "";
                        }
                    }

                    _rows.Add(cols);
                }

                _noCols = colCount;
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
                return false;
            }

            _loaded = true;

            Debug.Log(message);

            return true;
        }

        public bool SecuredSave(string pathName)
        {
            if (!_loaded || _rows == null)
            {
                Debug.Log("Fail to save, CSV is not loaded yet!!!");
                return false;
            }

            pathName = Path.ChangeExtension(pathName, ".bytes");

            try
            {
                FileStream fs = File.Open(pathName, FileMode.CreateNew, FileAccess.Write);
                BinaryWriter w = new BinaryWriter(fs);

                w.Write(_rows.Count);

                for (int i = 0; i < _rows.Count; ++i)
                {
                    w.Write(_rows[i].Length);

                    for (int j = 0; j < _rows[i].Length; ++j)
                    {
                        char[] stringArray = _rows[i][j].ToCharArray();
                        int length = (stringArray == null) ? 0 : stringArray.Length;

                        for (int k = 0; k < length; ++k)
                            stringArray[k] ^= crypt;

                        w.Write(length);

                        if (length > 0)
                            w.Write(stringArray, 0, length);
                    }
                }

                w.Close();
                fs.Close();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message.ToString());
                return false;
            }

            return true;
        }

        public bool LoadFromFile(string pathName)
        {
            try
            {
                FileStream fs = new FileStream(pathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                _cols = new List<string>();
                _rows = new List<String[]>();
                sr.BaseStream.Seek(0, SeekOrigin.Begin);

                _noCols = 0;

                HashSet<int> setColumnExceptComments = new HashSet<int>();

                int noRows = 0;
                while (sr.Peek() > -1)
                {
                    string aLine = sr.ReadLine();
                    aLine.Trim();

                    if (aLine.Length <= 0)
                        continue;

                    string[] cols = aLine.Split('\t');
                    if (cols.Length > 0 && cols[0] == "")
                        continue;

                    if (noRows++ == 0)
                    {
                        for (int i = 0; i < cols.Length; ++i)
                        {
                            if (cols[i].Length > 0 && !cols[i].StartsWith("*"))
                            {
                                setColumnExceptComments.Add(i);
                                _noCols++;
                            }
                        }

                        if (_noCols == 0)
                            throw new Exception("There is no valid columns");

                        continue;
                    }

                    int colCount = 0;
                    string[] data = new string[_noCols];

                    for (int i = 0; i < cols.Length; ++i)
                    {
                        if (setColumnExceptComments.Contains(i))
                        {
                            cols[i].Trim();
                            data[colCount++] = cols[i];
                        }
                    }

                    _rows.Add(data);
                }

                sr.Close();
                fs.Close();

                _loaded = true;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message.ToString());
                return false;
            }

            return true;
        }

        public bool Load(string text, bool colLoad = false)
        {
            try
            {
#if BUNDLE
                /// 해당 파일이 번들로 존재 한다면 번들로드한다.
#endif

                StringReader sr = new StringReader(text);

                _cols = new List<string>();
                _rows = new List<String[]>();

                _noCols = 0;

                HashSet<int> setColumnExceptComments = new HashSet<int>();

                int noRows = 0;
                string aLine;
                while ((aLine = sr.ReadLine()) != null)
                {
                    aLine.Trim();

                    if (aLine.Length <= 0)
                        continue;

                    string[] cols = aLine.Split('\t');
                    if (cols.Length > 0 && cols[0] == "")
                        continue;

                    if (noRows++ == 0)
                    {
                        for (int i = 0; i < cols.Length; ++i)
                        {
                            if (cols[i].Length > 0 && !cols[i].StartsWith("*"))
                            {
                                if (colLoad == true)
                                    _cols.Add(cols[i]);

                                setColumnExceptComments.Add(i);
                                _noCols++;
                            }
                        }

                        if (_noCols == 0)
                            throw new Exception("There is no valid columns");

                        continue;
                    }

                    int colCount = 0;
                    string[] data = new string[_noCols];

                    for (int i = 0; i < cols.Length; ++i)
                    {
                        if (setColumnExceptComments.Contains(i))
                        {
                            cols[i].Trim();
                            data[colCount++] = cols[i];
                        }
                    }

                    _rows.Add(data);
                }

                sr.Close();

                _loaded = true;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message.ToString());
                return false;
            }

            return true;
        }

        public bool Load2(string text, bool colLoad = false)
        {
            try
            {
#if BUNDLE
                /// 해당 파일이 번들로 존재 한다면 번들로드한다.
#endif

                StringReader sr = new StringReader(text);

                _cols = new List<string>();
                _rows = new List<String[]>();

                _noCols = 0;

                HashSet<int> setColumnExceptComments = new HashSet<int>();

                int noRows = 0;
                string aLine;
                while ((aLine = sr.ReadLine()) != null)
                {
                    aLine.Trim();

                    if (aLine.Length <= 0)
                        continue;

                    string[] cols = aLine.Split(',');
                    if (cols.Length > 0 && cols[0] == "")
                        continue;

                    if (noRows++ == 0)
                    {
                        for (int i = 0; i < cols.Length; ++i)
                        {
                            if (cols[i].Length > 0 && !cols[i].StartsWith("*"))
                            {
                                if (colLoad == true)
                                    _cols.Add(cols[i]);

                                setColumnExceptComments.Add(i);
                                _noCols++;
                            }
                        }

                        if (_noCols == 0)
                            throw new Exception("There is no valid columns");

                        continue;
                    }

                    int colCount = 0;
                    string[] data = new string[_noCols];

                    for (int i = 0; i < cols.Length; ++i)
                    {
                        if (setColumnExceptComments.Contains(i))
                        {
                            cols[i].Trim();
                            data[colCount++] = cols[i];
                        }
                    }

                    _rows.Add(data);
                }

                sr.Close();

                _loaded = true;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message.ToString());
                return false;
            }

            return true;
        }

        public bool LoadStringCSV(string text, bool colLoad = false)
        {
            try
            {
                StringReader sr = new StringReader(text);
                _cols = new List<string>();
                _rows = new List<String[]>();

                _noCols = 0;
                HashSet<int> setColumnExceptComments = new HashSet<int>();
                int noRows = 0;
                string aLine;
                while ((aLine = sr.ReadLine()) != null)
                {
                    if (aLine.Length <= 0)
                        continue;

                    if (noRows++ == 0)
                    {
                        string[] cols = aLine.Split(',');
                        for (int i = 0; i < cols.Length; ++i)
                        {
                            if (cols[i].Length > 0 && !cols[i].StartsWith("*"))
                            {
                                if (colLoad == true)
                                    _cols.Add(cols[i]);

                                setColumnExceptComments.Add(i);
                                _noCols++;
                            }
                        }

                        if (_noCols == 0)
                            throw new Exception("There is no valid columns");

                        continue;
                    }
                    
                    var values = new List<string>();
                    bool inQuotes = false;
                    string currentValue = "";

                    foreach (var c in aLine)
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
                    values.Add(currentValue.Replace("\\n","\n"));

                    _rows.Add(values.ToArray());
                }

                sr.Close();
                _loaded = true;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message.ToString());
                return false;
            }

            return true;
        }

        public bool SecuredLoad(byte[] buffer)
        {
            try
            {
                _noCols = 0;

                MemoryStream ms = new MemoryStream(buffer);
                BinaryReader r = new BinaryReader(ms);

                int rowCount = r.ReadInt32();
                _cols = new List<string>();
                _rows = new List<string[]>();

                int colCount = 0;
                for (int i = 0; i < rowCount; ++i)
                {
                    colCount = r.ReadInt32();
                    string[] cols = new string[colCount];

                    for (int j = 0; j < colCount; ++j)
                    {
                        int length = r.ReadInt32();
                        if (length > 0)
                        {
                            char[] stringArray = r.ReadChars(length);

                            for (int k = 0; k < length; ++k)
                                stringArray[k] ^= crypt;

                            cols[j] = new string(stringArray, 0, length);
                        }
                        else
                        {
                            cols[j] = "";
                        }
                    }

                    _rows.Add(cols);
                }

                _noCols = colCount;
            }
            catch (Exception e)
            {
                message = e.Message.ToString();
                Debug.Log(message);
                return false;
            }

            _loaded = true;

            return true;
        }
    }


    public class TableBase : MonoBehaviour
    {
        private bool isLoaded = false;

        public bool IsLoaded
        {
            get { return isLoaded; }
            protected set { isLoaded = value; }
        }


        public bool Load<T>(string text)
        {
            if (LoadData(text))
            {
                isLoaded = true;
                return true;
            }

            isLoaded = true;
            return false;
        }
        public virtual bool Load(string filePath)
        {
            CSVLoader csvLoader = new CSVLoader();

            //if (!csvLoader.Load(filePath)
            if (!csvLoader.Load2(filePath, true))
                return false;

            Read(csvLoader);

            isLoaded = true;

            return true;
        }

        protected virtual bool LoadData(string text)
        {
            return true;
        }
        public virtual void Read(CSVLoader csvLoader)
        {
            Debug.LogError("ERROR : TABLE MUST BE OVERRIDE FUNCTION - Read");
        }
    }
}
