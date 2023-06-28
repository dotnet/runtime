// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Tests for the contiguous assignments from SIMD to memory
// optimization in morph

struct V2
{
    public float x;
    public float y;
}

struct V3
{
    public float x;
    public float y;
    public float z;
}

struct V4
{
    public float x;
    public float y;
    public float z;
    public float w;
}

public class Runtime_31615
{
    static int s_checks;
    static int s_errors;

    const float g2X = 33f;
    const float g2Y = 67f;

    const float g3X = 11f;
    const float g3Y = 65f;
    const float g3Z = 24f;

    const float g4X = 10f;
    const float g4Y = 20f;
    const float g4Z = 30f;
    const float g4W = 40f;

    const float f0 = -101f;
    const float f1 = -7f;
 
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2 G2()
    {
        Vector2 r = new Vector2();
        r.X = g2X;
        r.Y = g2Y;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector3 G3()
    {
        Vector3 r = new Vector3();
        r.X = g3X;
        r.Y = g3Y;
        r.Z = g3Z;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector4 G4()
    {
        Vector4 r = new Vector4();
        r.X = g4X;
        r.Y = g4Y;
        r.Z = g4Z;
        r.W = g4W;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Check(V2 v2, float x, float y, [CallerLineNumber] int line = 0)
    {
        s_checks++;
        if ((v2.x != x) || (v2.y != y))
        {
            s_errors++;
            Console.WriteLine($"Check at line {line} failed; have ({v2.x},{v2.y}); expected ({x},{y})");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Check(V3 v3, float x, float y, float z, [CallerLineNumber] int line = 0)
    {
        s_checks++;
        if ((v3.x != x) || (v3.y != y) || (v3.z != z))
        {
            s_errors++;
            Console.WriteLine($"Check at line {line} failed; have ({v3.x},{v3.y},{v3.z}); expected ({x},{y},{z})");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Check(V4 v4, float x, float y, float z, float w, [CallerLineNumber] int line = 0)
    {
        s_checks++;
        if ((v4.x != x) || (v4.y != y) || (v4.z != z) || (v4.w != w))
        {
            s_errors++;
            Console.WriteLine($"Check at line {line} failed; have ({v4.x},{v4.y},{v4.z},{v4.w}); expected ({x},{y},{z},{w})");
        }
    }
    
    static void TestV2A()
    {
        Vector2 g2 = G2();
        V2 v2a = new V2();
        v2a.x = g2.X;
        v2a.y = g2.Y;
        Check(v2a, g2X, g2Y);
    }

    static void TestV2B()
    {
        Vector2 g2 = G2();
        V2 v2b = new V2();
        v2b.x = g2.Y;
        v2b.y = g2.X;
        Check(v2b, g2Y, g2X);
    }

    static void TestV2C()
    {
        Vector3 g3 = G3();
        V2 v2c = new V2();
        v2c.x = g3.X;
        v2c.y = g3.Y;
        Check(v2c, g3X, g3Y);
    }

    static void TestV2D()
    {
        Vector3 g3 = G3();
        V2 v2d = new V2();
        v2d.x = g3.Y;
        v2d.y = g3.Z;
        Check(v2d, g3Y, g3Z);
    }

    static void TestV2E()
    {
        Vector3 g3 = G3();
        V2 v2e = new V2();
        v2e.x = g3.X;
        v2e.y = g3.Z;
        Check(v2e, g3X, g3Z);
    }

    static void TestV3A()
    {
        Vector2 g2 = G2();
        V3 v3a = new V3();
        v3a.x = g2.X;
        v3a.y = g2.Y;
        Check(v3a, g2X, g2Y, 0f);
    }

    static void TestV3B()
    {
        Vector2 g2 = G2();
        V3 v3b = new V3();
        v3b.y = g2.X;
        v3b.z = g2.Y;
        Check(v3b, 0f, g2X, g2Y);
    }
    
    static void TestV3C()
    {
        Vector3 g3 = G3();
        V3 v3c = new V3();
        v3c.x = g3.Y;
        v3c.y = g3.Z;
        Check(v3c, g3Y, g3Z, 0f);
    }
    
    static void TestV3D()
    {
        Vector3 g3 = G3();
        V3 v3d = new V3();
        v3d.x = g3.X;
        v3d.y = g3.Y;
        v3d.z = g3.Z;
        Check(v3d, g3X, g3Y, g3Z);
    }

    static void TestV3E()
    {
        Vector3 g3 = G3();
        V3 v3e = new V3();
        v3e.x = g3.Z;
        v3e.y = g3.X;
        v3e.z = g3.Y;
        Check(v3e, g3Z, g3X, g3Y);
    }

    static void TestV3F()
    {
        Vector3 g3 = G3();
        V3 v3f = new V3();
        v3f.x = g3.X;
        v3f.y = g3.Y;
        v3f.z = g3.Y;
        Check(v3f, g3X, g3Y, g3Y);
    }

    static void TestV3G()
    {
        Vector3 g3 = G3();
        V3 v3g = new V3();
        v3g.x = g3.Y;
        v3g.y = g3.Y;
        v3g.z = g3.Z;
        Check(v3g, g3Y, g3Y, g3Z);
    }

    static void TestV4A()
    {
        Vector4 g4 = G4();
        V4 v4a = new V4();
        v4a.x = g4.X;
        v4a.y = g4.Y;
        v4a.z = g4.Z;
        v4a.w = g4.W;
        Check(v4a, g4X, g4Y, g4Z, g4W);
    }
    
    static void TestV4B()
    {
        Vector4 g4 = G4();
        V4 v4b = new V4();
        v4b.x = g4.Y;
        v4b.y = g4.X;
        v4b.z = g4.W;
        v4b.w = g4.Z;
        Check(v4b, g4Y, g4X, g4W, g4Z);
    }
    
    static void TestV4C()
    
    {
        Vector2 g2 = G2();
        V4 v4c = new V4();
        v4c.x = g2.X;
        v4c.y = g2.Y;
        v4c.z = g2.X;
        v4c.w = g2.Y;
        Check(v4c, g2X, g2Y, g2X, g2Y);
    }
    
    static void TestV4D()
    {
        Vector3 g3 = G3();
        V4 v4d = new V4();
        v4d.x = g3.X;
        v4d.y = g3.Y;
        v4d.z = g3.Z;
        v4d.w = f1;
        Check(v4d, g3X, g3Y, g3Z, f1);
    }
    
    static void TestV4E()
    {
        Vector2 g2 = G2();
        V4 v4e = new V4();
        v4e.x = f0;
        v4e.y = g2.X;
        v4e.z = g2.Y;
        v4e.w = f1;
        Check(v4e, f0, g2X, g2Y, f1);
    }
    
    static void TestV4F()
    {
        Vector2 g2 = G2();
        V4 v4f = new V4();
        v4f.x = f0;
        v4f.y = f1;
        v4f.z = g2.X;
        v4f.w = g2.Y;
        Check(v4f, f0, f1, g2X, g2Y);
    }
    
    static void TestV4G()
    {
        Vector3 g3 = G3();
        V4 v4g = new V4();
        v4g.x = f1;
        v4g.y = g3.X;
        v4g.z = g3.Y;
        v4g.w = g3.Z;
        Check(v4g, f1, g3X, g3Y, g3Z);
    }
    
    static void TestV4H()
    {
        Vector4 g4 = G4();
        V4 v4h = new V4();
        v4h.y = g4.Y;
        v4h.x = g4.X;
        v4h.w = g4.W;
        v4h.z = g4.Z;
        Check(v4h, g4X, g4Y, g4Z, g4W);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Vector2 g2 = G2();
        Vector3 g3 = G3();
        Vector4 g4 = G4();

        TestV2A();        
        TestV2B();
        TestV2C();
        TestV2D();
        TestV2E();

        TestV3A();        
        TestV3B();
        TestV3C();
        TestV3D();
        TestV3E();        
        TestV3F();
        TestV3G();

        TestV4A();        
        TestV4B();
        TestV4C();
        TestV4D();
        TestV4E();        
        TestV4F();
        TestV4G();
        TestV4H();

        if (s_errors > 0)
        {
            Console.WriteLine($"Failed; {s_errors} errors in {s_checks} tests");
            return -1;
        }
        else
        {
            Console.WriteLine($"Passed all {s_checks} tests");
            return 100;
        }
    }
}

