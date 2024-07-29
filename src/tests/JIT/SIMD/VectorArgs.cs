// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using Xunit;

internal class Color
{
    private Vector<float> _rgb;

    public Color(float r, float g, float b)
    {
        float[] temp = new float[Vector<float>.Count];
        temp[0] = r; temp[1] = g; temp[2] = b;
        _rgb = new Vector<float>(temp);
    }

    public Color(Vector<float> _rgb)
    { this._rgb = _rgb; }

    public Color Change(float f)
    {
        Vector<float> t = new Vector<float>(f);
        // t[3] = 0;
        return new Color(t * _rgb);
    }

    public Vector<float> RGB { get { return _rgb; } }
}

public partial class VectorTest
{
    private static int VectorArgs()
    {
        const int Pass = 100;
        const int Fail = -1;

        float[] temp = new float[Vector<float>.Count];
        for (int i = 0; i < Vector<float>.Count; i++)
        {
            temp[i] = 3 - i;
        }
        Vector<float> rgb = new Vector<float>(temp);

        float x = 2f;
        Color c1 = new Color(rgb);
        Color c2 = c1.Change(x);

        for (int i = 0; i < Vector<float>.Count; i++)
        {
            // Round to integer for comparison.
            if (((int)c2.RGB[i]) != (3 - i) * x)
            {
                return Fail;
            }
        }

        return Pass;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return VectorArgs();
    }
}
