using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public struct FastEnumIntEqualityComparer<T> : IEqualityComparer<T> where T : struct, IConvertible
{
    int ToInt<T>(T en) where T : struct, IConvertible
    {
        if (typeof(T).IsEnum || typeof(T) == typeof(int))
        {
            return en.ToInt32(null);
        }

        return 0;
    }

    public bool Equals(T firstEnum, T secondEnum)
    {
        return ToInt(firstEnum) == ToInt(secondEnum);
    }

    public int GetHashCode(T firstEnum)
    {
        return ToInt(firstEnum);
    }
}

internal static class YieldInsturctionCache
{
    public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
    public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

    class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y)
        {
            return x == y;
        }

        int IEqualityComparer<float>.GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }


    private static readonly Dictionary<float, WaitForSeconds> _timeInterval =
        new Dictionary<float, WaitForSeconds>(new FloatComparer());

    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        WaitForSeconds wfs;
        if (!_timeInterval.TryGetValue(seconds, out wfs))
            _timeInterval.Add(seconds, wfs = new WaitForSeconds(seconds));
        return wfs;
    }
}


public struct VVector4
{
    public static Vector4 GetVec4(float x, float y, float z, float w)
    {
        Vector4 data = Vector4.zero;
        data.x = x;
        data.y = y;
        data.z = z;
        data.w = w;
        return data;
    }
}

public struct VVector3Int
{
    public static Vector3Int GetVec3(int x, int y, int z)
    {
        Vector3Int data = Vector3Int.zero;
        data.x = x;
        data.y = y;
        data.z = z;
        return data;
    }

    public static bool Equal(Vector3Int a, Vector3Int b)
    {
        return a == b;
    }
}

[Serializable]
public struct Vector3Long
{
    public long x;
    public int y;
    public int z;

    public Vector3Long(long x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public static Vector3Long GetVec3(long idx, int cellidx, int angle)
    {
        long x = idx;
        int y = cellidx;
        int z = angle;
        
        return new Vector3Long(x, y, z);
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector3Long other)
            return x == other.x && y == other.y && z == other.z;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public static bool operator ==(Vector3Long a, Vector3Long b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    public static bool operator !=(Vector3Long a, Vector3Long b)
    {
        return !(a == b);
    }
}
public struct Vector2Long
{
    public long x;
    public long y;

    public Vector2Long(long x, long y)
    {
        this.x = x;
        this.y = y;
    }
    public static Vector2Long GetVec2(long id, long idx)
    {
        long x = id;
        long y = idx;
        
        return new Vector2Long(x, y);
    }
}


public struct VVector2Int
{
    public static Vector2Int GetVec2(int x, int y)
    {
        Vector2Int data = Vector2Int.zero;
        data.x = x;
        data.y = y;
        return data;
    }

    public static bool Equal(Vector2Int a, Vector2Int b)
    {
        return a == b;
    }
}


public struct VVector3
{
    public static Vector3 GetVec3(float x, float y, float z)
    {
        Vector3 data = Vector3.zero;
        data.x = x;
        data.y = y;
        data.z = z;
        return data;
    }

    public static Vector3 GetVec2(float x, float y)
    {
        Vector3 data = Vector3.zero;
        data.x = x;
        data.y = y;
        data.z = 0;
        return data;
    }

    public static bool Equal(Vector3 a, Vector3 b)
    {
        if (!Mathf.Approximately(a.x, b.x))
            return false;

        if (!Mathf.Approximately(a.y, b.y))
            return false;

        if (!Mathf.Approximately(a.z, b.z))
            return false;

        return true;
    }
}

public struct VVector2
{
    public static Vector2 GetVec2(float x, float y)
    {
        Vector2 data = Vector2.zero;
        data.x = x;
        data.y = y;

        return data;
    }
}

public struct CColor
{
    public static Color red
    {
        get { return GetColor(255, 50, 50); }
    }

    public static Color green
    {
        get { return GetColor(50, 255, 50); }
    }

    public static Color blue
    {
        get { return GetColor(50, 50, 255); }
    }

    public static Color gray
    {
        get { return GetColor(128, 128, 128); }
    }

    public static Color orange
    {
        get { return GetColor(255, 165, 50); }
    }

    public static Color pink
    {
        get { return GetColor(255, 150, 150); }
    }

    public static Color pale_green
    {
        get { return GetColor(150, 255, 150); }
    }

    public static Color lilac
    {
        get { return GetColor(150, 150, 255); }
    }

    public static Color gold
    {
        get { return GetColor(255, 215, 0); }
    }

    public static Color yellow
    {
        get { return GetColor(255, 200, 0); }
    }

    public static Color hot_pink
    {
        get { return GetColor(255, 0, 200); }
    }

    public static Color yellow_green
    {
        get { return GetColor(200, 255, 0); }
    }

    public static Color purple
    {
        get { return GetColor(200, 0, 255); }
    }

    public static Color aquamarine
    {
        get { return GetColor(0, 255, 200); }
    }

    public static Color sky_blue
    {
        get { return GetColor(0, 200, 255); }
    }

    public static Color whilte
    {
        get { return GetColor(255, 255, 255); }
    }

    public static Color little_black
    {
        get { return GetColor(42, 42, 42); }
    } // �н����� Ŭ���� �� �÷�.

    public static Color pass_progressbar_normal
    {
        get { return GetColor(243, 236, 123); }
    }

    public static Color pass_progressbar_playing
    {
        get { return GetColor(0, 255, 255); }
    }

    #region 비활성화 칼라

    public static Color disable_color1 => GetColor(75, 84, 112);
    public static Color disable_color2 => GetColor(52, 68, 77);

    public static Color enable_color1 => GetColor(87, 85, 215);
    public static Color enable_color2 => GetColor(58, 56, 143);

    #endregion

    #region 활성화 칼라

    #endregion


    public static int TryParse(string str)
    {
        int convert = 0;
        try
        {
            convert = int.Parse(str, NumberStyles.AllowHexSpecifier);
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex.Message);
        }

        return convert;
    }

    public static Color GetColor(string colorValue)
    {
        if (colorValue.Length != 6 && colorValue.Length != 8)
        {
            return Color.white;
        }

        string str = colorValue.Substring(0, 2);
        int r = TryParse(str);

        str = colorValue.Substring(2, 2);
        int g = TryParse(str);

        str = colorValue.Substring(4, 2);
        int b = TryParse(str);

        if (colorValue.Length == 6)
        {
            return GetColor(r, g, b);
        }

        if (colorValue.Length == 8)
        {
            // Alpha Set
            string strAlpha = colorValue.Substring(6, 2);
            int a = TryParse(strAlpha);
            return GetColor(r, g, b, a);
        }

        return Color.white;
    }

    public static string ColorToHex(Color32 c, bool includeAlpha = false)
    {
        if (includeAlpha)
            return $"{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
        else
            return $"{c.r:X2}{c.g:X2}{c.b:X2}";
    }

    /*
    public static Color GetColor(int colorCode)
    {
        string colorValue = TableManager.Instance.Get<TableColor>().GetColorValue(colorCode);
        Color color = GetColor(colorValue);

        return color;
    }

    public static Color GetColor(int groupIdx, int colorCode)
    {
        string colorValue = TableManager.Instance.Get<TableColor>().GetColorValue(groupIdx, colorCode);
        Color color = GetColor(colorValue);

        return color;
    }
    */


    public static Color GetColor(int r, int g, int b)
    {
        return GetColor((float)r, (float)g, (float)b);
    }

    public static Color GetColor(int r, int g, int b, int a)
    {
        return GetColor((float)r, (float)g, (float)b, (float)a);
    }

    public static Color GetColor(float r, float g, float b)
    {
        if (r > 1f || g > 1f || b > 1f)
        {
            r = r / 255f;
            g = g / 255f;
            b = b / 255f;
        }

        return GetColor(r, g, b, 1);
    }

    public static Color GetColor(float r, float g, float b, float a)
    {
        if (r > 1f || g > 1f || b > 1f || a > 1f)
        {
            r = r / 255f;
            g = g / 255f;
            b = b / 255f;
            a = a / 255f;
        }

        Color data = Color.white;
        data.r = r;
        data.g = g;
        data.b = b;
        data.a = a;
        return data;
    }

    public static Color GetColor(Color c, float a)
    {
        Color data = c;
        data.a = a;
        return data;
    }
}

public class XorShiftRandom
{
    private uint x, y, z, w;

    public XorShiftRandom(int seed)
    {
        x = (uint)seed;
        y = 362436069;
        z = 521288629;
        w = 88675123;
    }

    public int Next(int minValue, int maxValue)
    {
        uint t = x ^ (x << 11);
        x = y;
        y = z;
        z = w;
        w = w ^ (w >> 19) ^ (t ^ (t >> 8));
        return minValue + (int)(w % (maxValue - minValue));
    }

    // 0과 1 사이의 float 값을 반환하는 메서드
    public float NextFloat()
    {
        return (float)Next(0, int.MaxValue) / int.MaxValue;
    }

    // 주어진 범위 내의 float 값을 반환하는 메서드
    public float Range(float minValue, float maxValue)
    {
        return minValue + NextFloat() * (maxValue - minValue);
    }
}
