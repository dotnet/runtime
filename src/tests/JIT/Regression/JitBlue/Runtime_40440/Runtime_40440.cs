// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

class Runtime_40440
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool UseArrayElementAsCallArgument<T>(T[,,] a, T b)
    {
        return G(b, a[1, 2, 3]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool G<T>(T a, T b)
    {
        if (typeof(T) == typeof(Vector<float>))
        {
            return (Vector<float>)(object)a == (Vector<float>)(object)b;
        }
        else if (typeof(T) == typeof(Vector2))
        {
            return (Vector2)(object)a == (Vector2)(object)b;
        }
        else if (typeof(T) == typeof(Vector3))
        {
            return (Vector3)(object)a == (Vector3)(object)b;
        }
        else if (typeof(T) == typeof(Vector4))
        {
            return (Vector4)(object)a == (Vector4)(object)b;
        }
        else if (typeof(T) == typeof(Vector64<float>))
        {
            return a.Equals(b);
        }
        else if (typeof(T) == typeof(SmallStruct))
        {
            return a.Equals(b);
        }
        else if (typeof(T) == typeof(LargeStruct))
        {
            return a.Equals(b);
        }
        return false;
    }

    static bool CheckVectorFloat()
    {
        var v = new Vector<float>[4, 4, 4];
        var e = new Vector<float>(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }
    static bool CheckVector2()
    {
        var v = new Vector2[4, 4, 4];
        var e = new Vector2(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    static bool CheckVector3()
    {
        var v = new Vector3[4, 4, 4];
        var e = new Vector3(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    static bool CheckVector4()
    {
        var v = new Vector3[4, 4, 4];
        var e = new Vector3(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    static bool CheckVector64()
    {
        var v = new Vector64<float>[4, 4, 4];
        var e = Vector64.Create(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    struct SmallStruct
    {
        float f;

        public SmallStruct(float f)
        {
            this.f = f;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SmallStruct))
            {
                return false;
            }
            return f == ((SmallStruct)obj).f;
        }
    }

    static bool CheckSmallStruct()
    {
        var v = new SmallStruct[4, 4, 4];
        var e = new SmallStruct(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    struct LargeStruct
    {
        float f1;
        float f2;
        float f3;
        float f4;
        float f5;

        public LargeStruct(float f)
        {
            f1 = f;
            f2 = f;
            f3 = f;
            f4 = f;
            f5 = f;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LargeStruct))
            {
                return false;
            }
            LargeStruct s2 = (LargeStruct)obj;
            return (f1 == s2.f1) && (f2 == s2.f2) && (f3 == s2.f3) && (f4 == s2.f4) && (f5 == s2.f5);
        }
    }

    static bool CheckBigStruct()
    {
        var v = new LargeStruct[4, 4, 4];
        var e = new LargeStruct(33f);
        v[1, 2, 3] = e;
        return UseArrayElementAsCallArgument(v, e);
    }

    public static int Main()
    {
        bool f = true;
        f &= CheckVectorFloat();
        Debug.Assert(f);
        f &= CheckVector2();
        Debug.Assert(f);
        f &= CheckVector3();
        Debug.Assert(f);
        f &= CheckVector4();
        Debug.Assert(f);
        f &= CheckVector64();
        Debug.Assert(f);
        f &= CheckSmallStruct();
        Debug.Assert(f);
        f &= CheckBigStruct();
        Debug.Assert(f);

        return f ? 100 : 0;
    }
}
