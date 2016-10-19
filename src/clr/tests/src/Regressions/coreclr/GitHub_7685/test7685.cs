// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

public class Test7685
{
    static RectangleF argumentInDStuff;
    
    public static int Main()
    {
        int iRetVal = 100;
        
        var r = new RectangleF(1.2f, 3.4f, 5.6f, 7.8f);
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuff").Invoke(null, new object[] { r });

        if (!RectangleF.Equals(ref argumentInDStuff, ref r))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with floats via reflection. Callee received {argumentInDStuff} instead of {r}");
            iRetVal = 0;
        }
        
        return iRetVal;
    }

    public static void DoStuff(RectangleF r)
    {
        argumentInDStuff = r;
    }
}

public struct RectangleF
{
    private float _x, _y, _width, _height;

    public RectangleF(float x, float y, float width, float height)
    {
        _x = x; _y = y; _width = width; _height = height;
    }
    
    public static bool Equals(ref RectangleF r1, ref RectangleF r2)
    {
        return (r2._x == r1._x) && (r2._y == r1._y) && (r2._width == r1._width) && (r2._height == r1._height);
    }

    public override string ToString() => $"[{_x}, {_y}, {_width}, {_height}]";
}
