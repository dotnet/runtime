// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

public struct Color
{
    private Vector3 _simdVector;
    public float R { get { return _simdVector.X; } }
    public float G { get { return _simdVector.Y; } }
    public float B { get { return _simdVector.Z; } }

    public Color(double r, double g, double b)
    {
        _simdVector = new Vector3((float)r, (float)g, (float)b);
    }
    public Color(string str)
    {
        string[] nums = str.Split(',');
        if (nums.Length != 3) throw new ArgumentException();
        _simdVector = new Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
    }


    public static Color Times(double n, Color v)
    {
        Color result;
        result._simdVector = (float)n * v._simdVector;
        return result;
    }
    public static Color Times(Color v1, Color v2)
    {
        Color result;
        result._simdVector = v1._simdVector * v2._simdVector;
        return result;
    }

    public static Color Plus(Color v1, Color v2)
    {
        Color result;
        result._simdVector = v1._simdVector + v2._simdVector;
        return result;
    }
    public static Color Minus(Color v1, Color v2)
    {
        Color result;
        result._simdVector = v1._simdVector - v2._simdVector;
        return result;
    }

    public static Color Background { get { Color result; result._simdVector = Vector3.Zero; return result; } }
    public static Color DefaultColor { get { Color result; result._simdVector = Vector3.Zero; return result; } }

    public static float Legalize(float d)
    {
        return d > 1 ? 1 : d;
    }

    public static byte ToByte(float c)
    {
        return (byte)(255 * Legalize(c));
    }

    public static Int32 ToInt32(float c)
    {
        Int32 r = (Int32)(255 * c);
        return (r > 255 ? 255 : r);
    }

    public Int32 ToInt32()
    {
        return (ToInt32(B) | ToInt32(G) << 8 | ToInt32(R) << 16 | 255 << 24);
    }

    public float Brightness()
    {
        float r = (float)R / 255.0f;
        float g = (float)G / 255.0f;
        float b = (float)B / 255.0f;

        float max, min;

        max = r; min = r;

        if (g > max) max = g;
        if (b > max) max = b;

        if (g < min) min = g;
        if (b < min) min = b;

        return (max + min) / 2;
    }

    public void ChangeHue(float hue)
    {
        float H, S, L, Br;

        Br = Brightness();
        H = hue;
        S = 0.9F;
        L = ((Br - 0.5F) * 0.5F) + 0.5F;

        if (L == 0)
        {
            _simdVector = Vector3.Zero;
        }
        else
        {
            if (S == 0)
            {
                _simdVector = new Vector3(L);
            }
            else
            {
                float temp2 = ((L <= 0.5F) ? L * (1.0F + S) : L + S - (L * S));
                float temp1 = 2.0F * L - temp2;

                float[] t3 = new float[] { H + 1.0F / 3.0F, H, H - 1.0F / 3.0F };
                float[] clr = new float[] { 0, 0, 0 };

                for (int i = 0; i < 3; i++)
                {
                    if (t3[i] < 0) t3[i] += 1.0F;
                    if (t3[i] > 1) t3[i] -= 1.0F;
                    if (6.0 * t3[i] < 1.0)
                        clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0F;
                    else if (2.0 * t3[i] < 1.0)
                        clr[i] = temp2;
                    else if (3.0 * t3[i] < 2.0)
                        clr[i] = (temp1 + (temp2 - temp1) * ((2.0F / 3.0F) - t3[i]) * 6.0F);
                    else
                        clr[i] = temp1;
                }

                _simdVector = new Vector3(clr[0], clr[1], clr[2]);
            }
        }
    }
}

