// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Test7685
{
    static RectangleF passedFloatStruct;
    static RectangleD passedDoubleStruct;
    static RectangleI passedIntStruct;
    static RectangleLLarge passedLongLargeStruct;
    static RectangleLSmall passedLongSmallStruct;
    static RectangleNestedF passedNestedSmallFStruct;
     
    [Fact]
    public static int TestEntryPoint()
    {
        int iRetVal = 100;
        
        var rF = new RectangleF(1.2f, 3.4f, 5.6f, 7.8f);
        var rD = new RectangleD(1.7E+3d, 4.5d, 500.1d, 60.0d);
        var rI = new RectangleI(100, -2, 3, 64);
        var rLSmall = new RectangleLSmall(11231L);
        var rLLarge = new RectangleLLarge(1L, 20041L, 22L, 88L);
        var rNestedFSmall = new RectangleNestedF(1.2f, 3.4f, 5.6f, 7.8f);

        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffF").Invoke(null, new object[] { rF });
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffD").Invoke(null, new object[] { rD });
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffI").Invoke(null, new object[] { rI });
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffLSmall").Invoke(null, new object[] { rLSmall });
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffLLarge").Invoke(null, new object[] { rLLarge });
        typeof(Test7685).GetTypeInfo().GetDeclaredMethod("DoStuffNestedF").Invoke(null, new object[] { rNestedFSmall });

        if (!RectangleF.Equals(ref passedFloatStruct, ref rF))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with floats via reflection. Callee received {passedFloatStruct} instead of {rF}");
            iRetVal = 0;
        }

        if (!RectangleD.Equals(ref passedDoubleStruct, ref rD))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with doubles via reflection. Callee received {passedDoubleStruct} instead of {rD}");
            iRetVal = 1;
        }

        if (!RectangleI.Equals(ref passedIntStruct, ref rI))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with ints via reflection. Callee received {passedIntStruct} instead of {rI}");
            iRetVal = 2;
        }

        if (!RectangleLSmall.Equals(ref passedLongSmallStruct, ref rLSmall))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with a long via reflection. Callee received {passedLongSmallStruct} instead of {rLSmall}");
            iRetVal = 3;
        }

        if (!RectangleLLarge.Equals(ref passedLongLargeStruct, ref rLLarge))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with longs via reflection. Callee received {passedLongLargeStruct} instead of {rLLarge}");
            iRetVal = 4;
        }

        if (!RectangleNestedF.Equals(ref passedNestedSmallFStruct, ref rNestedFSmall))
        {
            TestLibrary.Logging.WriteLine($"Error: passing struct with longs via reflection. Callee received {passedNestedSmallFStruct} instead of {rNestedFSmall}");
            iRetVal = 5;
        }
        
        return iRetVal;
    }

    public static void DoStuffF(RectangleF r)
    {
        passedFloatStruct = r;
    }

    public static void DoStuffD(RectangleD r)
    {
        passedDoubleStruct = r;
    }

    public static void DoStuffI(RectangleI r)
    {
        passedIntStruct = r;
    }

    public static void DoStuffLSmall(RectangleLSmall r)
    {
        passedLongSmallStruct = r;
    }

    public static void DoStuffLLarge(RectangleLLarge r)
    {
        passedLongLargeStruct = r;
    }

    public static void DoStuffNestedF(RectangleNestedF r)
    {
        passedNestedSmallFStruct = r;
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

public struct RectangleFSmall
{
    public float _x, _y;

    public RectangleFSmall(float x, float y)
    {
        _x = x; _y = y;
    }
    
    public static bool Equals(ref RectangleFSmall r1, ref RectangleFSmall r2)
    {
        return (r2._x == r1._x) && (r2._y == r1._y);
    }

    public override string ToString() => $"[{_x}, {_y}]";
}

public struct RectangleD
{
    private double _x, _y, _width, _height;

    public RectangleD(double x, double y, double width, double height)
    {
        _x = x; _y = y; _width = width; _height = height;
    }
    
    public static bool Equals(ref RectangleD r1, ref RectangleD r2)
    {
        return (r2._x == r1._x) && (r2._y == r1._y) && (r2._width == r1._width) && (r2._height == r1._height);
    }

    public override string ToString() => $"[{_x}, {_y}, {_width}, {_height}]";
}

public struct RectangleI
{
    private int _x, _y, _width, _height;

    public RectangleI(int x, int y, int width, int height)
    {
        _x = x; _y = y; _width = width; _height = height;
    }
    
    public static bool Equals(ref RectangleI r1, ref RectangleI r2)
    {
        return (r2._x == r1._x) && (r2._y == r1._y) && (r2._width == r1._width) && (r2._height == r1._height);
    }

    public override string ToString() => $"[{_x}, {_y}, {_width}, {_height}]";
}

public struct RectangleLSmall
{
    private long _x;

    public RectangleLSmall(long x)
    {
        _x = x;
    }
    
    public static bool Equals(ref RectangleLSmall r1, ref RectangleLSmall r2)
    {
        return (r2._x == r1._x);
    }

    public override string ToString() => $"[{_x}]";
}

public struct RectangleLLarge
{
    private long _x, _y, _width, _height;

    public RectangleLLarge(long x, long y, long width, long height)
    {
        _x = x; _y = y; _width = width; _height = height;
    }
    
    public static bool Equals(ref RectangleLLarge r1, ref RectangleLLarge r2)
    {
        return (r2._x == r1._x) && (r2._y == r1._y) && (r2._width == r1._width) && (r2._height == r1._height);
    }

    public override string ToString() => $"[{_x}, {_y}, {_width}, {_height}]";
}

public struct RectangleNestedF
{
    private RectangleFSmall first, second;

    public RectangleNestedF(float x, float y, float width, float height)
    {
        first = new RectangleFSmall(x, y);
        second = new RectangleFSmall(width, height);
    }
    
    public static bool Equals(ref RectangleNestedF r1, ref RectangleNestedF r2)
    {
        return (r1.first._x == r2.first._x) && (r1.first._y == r2.first._y) && (r1.second._x == r2.second._x) && (r1.second._y == r2.second._y);
    }

    public override string ToString() => $"[{first._x}, {first._y}, {second._x}, {second._y}]";
}
