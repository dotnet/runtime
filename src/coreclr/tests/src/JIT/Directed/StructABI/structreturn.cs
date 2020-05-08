// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Test register struct returns and local vars retyping cases.

using System;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#region Test struct return optimizations.
class TestStructReturns
{
    struct LessNativeInt
    {
        public bool a;
        public bool b;
    }

    struct NativeIntOneField
    {
        public long a;
    }

    struct NativeIntTwoFields
    {
        public int a;
        public int b;
    }

    struct NativeIntFloatField
    {
        public double a;
    }

    struct NativeIntMixedFields
    {
        public int a;
        public float b;
    }

    static LessNativeInt TestLessNativeIntReturnBlockInit()
    {
        LessNativeInt a = new LessNativeInt();
        return a;
    }

    static LessNativeInt TestLessNativeIntReturnFieldInit()
    {
        LessNativeInt a;
        a.a = false;
        a.b = true;
        return a;
    }


    static NativeIntOneField TestNativeIntOneFieldReturnBlockInit()
    {
        NativeIntOneField a = new NativeIntOneField();
        return a;
    }

    static NativeIntOneField TestNativeIntOneFieldReturnFieldInit()
    {
        NativeIntOneField a;
        a.a = 100;
        return a;
    }

    static NativeIntTwoFields TestNativeIntTwoFieldsReturnBlockInit()
    {
        NativeIntTwoFields a = new NativeIntTwoFields();
        return a;
    }

    static NativeIntTwoFields TestNativeIntTwoFieldsReturnFieldInit()
    {
        NativeIntTwoFields a;
        a.a = 100;
        a.b = 10;
        return a;
    }

    static NativeIntFloatField TestNativeIntFloatFieldReturnBlockInit()
    {
        NativeIntFloatField a = new NativeIntFloatField();
        return a;
    }

    static NativeIntFloatField TestNativeIntFloatFieldReturnFieldInit()
    {
        NativeIntFloatField a;
        a.a = 100;
        return a;
    }

    static NativeIntMixedFields TestNativeIntMixedFieldsReturnBlockInit()
    {
        NativeIntMixedFields a = new NativeIntMixedFields();
        return a;
    }

    static NativeIntMixedFields TestNativeIntMixedFieldsReturnFieldInit()
    {
        NativeIntMixedFields a;
        a.a = 100;
        a.b = 10;
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestLessNativeIntCall1()
    {
        int res = 0;
        var v = TestLessNativeIntReturnBlockInit();
        if (v.a)
        {
            res++;
        }
        if (v.b)
        {
            res++;
        }
        if (v.a && v.b)
        {
            res++;
        }
        if (!v.a && !v.b)
        {
            res--;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestLessNativeIntCall2()
    {
        int res = 0;
        var v = TestLessNativeIntReturnFieldInit();
        if (v.a)
        {
            res++;
        }
        if (v.b)
        {
            res++;
        }
        if (v.a && v.b)
        {
            res++;
        }
        if (!v.a && !v.b)
        {
            res--;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntOneFieldCall1()
    {
        int res = 0;
        var v = TestNativeIntOneFieldReturnBlockInit();
        if (v.a == 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntOneFieldCall2()
    {
        int res = 0;
        var v = TestNativeIntOneFieldReturnFieldInit();
        if (v.a == 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntTwoFieldsCall1()
    {
        int res = 0;
        var v = TestNativeIntTwoFieldsReturnBlockInit();
        if (v.a == 0)
        {
            res++;
        }
        if (v.b == 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntTwoFieldsCall2()
    {
        int res = 0;
        var v = TestNativeIntTwoFieldsReturnFieldInit();
        if (v.a == 0)
        {
            res++;
        }
        if (v.b == 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntFloatFieldCall1()
    {
        int res = 0;
        var v = TestNativeIntFloatFieldReturnBlockInit();
        if (v.a >= 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntFloatFieldCall2()
    {
        int res = 0;
        var v = TestNativeIntFloatFieldReturnFieldInit();
        if (v.a >= 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntMixedFieldsCall1()
    {
        int res = 0;
        var v = TestNativeIntMixedFieldsReturnBlockInit();
        if (v.a == 0)
        {
            res++;
        }
        if (v.b == 0)
        {
            res++;
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestNativeIntMixedFieldsCall2()
    {
        int res = 0;
        var v = TestNativeIntMixedFieldsReturnFieldInit();
        if (v.a == 0)
        {
            res++;
        }
        if (v.b == 0)
        {
            res++;
        }
        return res;
    }

    public static void Test()
    {
        TestLessNativeIntCall1();
        TestLessNativeIntCall2();
        TestNativeIntOneFieldCall1();
        TestNativeIntOneFieldCall2();
        TestNativeIntTwoFieldsCall1();
        TestNativeIntTwoFieldsCall2();
        TestNativeIntFloatFieldCall1();
        TestNativeIntFloatFieldCall2();
        TestNativeIntMixedFieldsCall1();
        TestNativeIntMixedFieldsCall2();
    }
}
#endregion

#region Test struct unsafe casts
class TestUnsafeCasts
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim1()
    {
        long[] l = new long[1] { 1 };
        ref int r = ref Unsafe.As<long, int>(ref l[0]);
        Debug.Assert(l[0] != 2);
        r = 2;
        Debug.Assert(l[0] == 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim2()
    {
        long[] l = new long[1] { 1 };
        ref float r = ref Unsafe.As<long, float>(ref l[0]);
        Debug.Assert(l[0] != 0);
        r = 0;
        Debug.Assert(l[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim3()
    {
        double[] d = new double[1] { 154345345 };
        ref float r = ref Unsafe.As<double, float>(ref d[0]);
        Debug.Assert(d[0] != 0);
        r = 0;
        Debug.Assert(d[0] == 154345344);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim4()
    {
        double[] d = new double[1] { 154345345 };
        ref int r = ref Unsafe.As<double, int>(ref d[0]);
        Debug.Assert(d[0] != 0);
        r = 0;
        Debug.Assert(d[0] == 154345344);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim5()
    {
        long l = 0x123412341234;
        int r = Unsafe.As<long, int>(ref l);
        Debug.Assert(r == 0x12341234);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLPrim6()
    {
        long l = 0;
        float r = Unsafe.As<long, float>(ref l);
        Debug.Assert(r == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim1()
    {
        int[] i = new int[2] { 0x12341234, 0 };
        ref long r = ref Unsafe.As<int, long>(ref i[0]);
        Debug.Assert(r == 0x12341234);
        r = 0;
        Debug.Assert(i[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim2()
    {
        int[] i = new int[2] { 1, 2 };
        ref double r = ref Unsafe.As<int, double>(ref i[0]);
        Debug.Assert(i[0] != 0);
        r = 0;
        Debug.Assert(i[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim3()
    {
        float[] f = new float[2] { 1, 2 };
        ref double r = ref Unsafe.As<float, double>(ref f[0]);
        Debug.Assert(f[0] != 0);
        r = 0;
        Debug.Assert(f[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim4()
    {
        float[] f = new float[2] { 1, 2 };
        ref long r = ref Unsafe.As<float, long>(ref f[0]);
        Debug.Assert(f[0] != 0);
        r = 0;
        Debug.Assert(f[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim5()
    {
        int l = 0x12341234;
        long r = Unsafe.As<int, long>(ref l);
        Debug.Assert((uint)(r % (UInt32.MaxValue + 1L)) == 0x12341234);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSPrim6()
    {
        int l = 5;
        double r = Unsafe.As<int, double>(ref l);
        l = Unsafe.As<double, int>(ref r);
        Debug.Assert(l == 5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim1()
    {
        float[] f = new float[1] { 1 };
        ref int r = ref Unsafe.As<float, int>(ref f[0]);
        Debug.Assert(f[0] != 0);
        r = 0;
        Debug.Assert(f[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim2()
    {
        int[] i = new int[1] { 1 };
        ref float r = ref Unsafe.As<int, float>(ref i[0]);
        Debug.Assert(i[0] != 0);
        r = 0;
        Debug.Assert(i[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim3()
    {
        short[] f = new short[1] { 1 };
        ref ushort r = ref Unsafe.As<short, ushort>(ref f[0]);
        Debug.Assert(f[0] != 0);
        r = 0;
        Debug.Assert(f[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim4()
    {
        int[] i = new int[1] { 1 };
        ref uint r = ref Unsafe.As<int, uint>(ref i[0]);
        Debug.Assert(i[0] != 0);
        r = 0;
        Debug.Assert(i[0] == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim5()
    {
        double l = 0x12341234;
        long r = Unsafe.As<double, long>(ref l);
        Debug.Assert(r == 0x41b2341234000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim6()
    {
        float l = 0x12341234;
        int r = Unsafe.As<float, int>(ref l);
        Debug.Assert(r == 0x4d91a092);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim7()
    {
        long l = 0;
        double r = Unsafe.As<long, double>(ref l);
        Debug.Assert(r == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim8()
    {
        int l = 0;
        float r = Unsafe.As<int, float>(ref l);
        Debug.Assert(r == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim9()
    {
        short l = 100;
        ushort r = Unsafe.As<short, ushort>(ref l);
        Debug.Assert(r == 100);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromLargePrim()
    {
        PrimFromLPrim1();
        PrimFromLPrim2();
        PrimFromLPrim3();
        PrimFromLPrim4();
        PrimFromLPrim5();
        PrimFromLPrim6();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSmallPrim()
    {
        PrimFromSPrim1();
        PrimFromSPrim2();
        PrimFromSPrim3();
        PrimFromSPrim4();
        PrimFromSPrim5();
        PrimFromSPrim6();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PrimFromSameSizePrim()
    {
        PrimFromSameSizePrim1();
        PrimFromSameSizePrim2();
        PrimFromSameSizePrim3();
        PrimFromSameSizePrim4();
        PrimFromSameSizePrim5();
        PrimFromSameSizePrim6();
        PrimFromSameSizePrim7();
        PrimFromSameSizePrim8();
        PrimFromSameSizePrim9();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestPrimitiveCasts()
    {
        PrimFromLargePrim();
        PrimFromSmallPrim();
        PrimFromSameSizePrim();
    }

    struct smallStruct
    {
        public bool a;
        public bool b;
    }

    struct nativeStruct
    {
        public IntPtr a;
    }

    struct largeStruct
    {
        public int a;
        public long b;
        public double c;
        public bool d;
        public float e;
    }

    struct eightByteStruct
    {
        public long a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void PrimFromSStruct()
    {
        smallStruct[] s = new smallStruct[2];
        s[0].a = true;
        s[0].b = false;
        int v = Unsafe.As<smallStruct, int>(ref s[0]);
        Debug.Assert(v != 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void PrimFromLStruct()
    {
        largeStruct s;
        s.a = 1;
        s.b = 2;
        s.c = 3.0;
        s.d = false;
        s.e = 1;
        int v = Unsafe.As<largeStruct, int>(ref s);
        Debug.Assert(v == 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void PrimFromStruct()
    {
        nativeStruct s;
        s.a = new IntPtr(100);
        long v = Unsafe.As<nativeStruct, long>(ref s);
        s = Unsafe.As<long, nativeStruct>(ref v);
        Debug.Assert(s.a.ToInt32() == 100);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromSPrim()
    {
        byte[] v = new byte[8] { 1, 0, 0, 0, 0, 0, 0, 0 };
        smallStruct s = Unsafe.As<byte, smallStruct>(ref v[0]);
        Debug.Assert(s.a == true && s.b == false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromLPrim()
    {
        int v = 0b1;
        smallStruct s = Unsafe.As<int, smallStruct>(ref v);
        Debug.Assert(s.a == true && s.b == false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromPrim()
    {
        long v = 100;
        nativeStruct s = Unsafe.As<long, nativeStruct>(ref v);
        Debug.Assert(s.a.ToInt32() == 100);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromSStruct1()
    {
        smallStruct[] smallS = new smallStruct[4];
        smallS[0].a = true;
        smallS[0].b = false;
        eightByteStruct largeS = Unsafe.As<smallStruct, eightByteStruct>(ref smallS[0]);
        Debug.Assert((uint)(largeS.a % (UInt32.MaxValue + 1L)) == 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromSStruct2()
    {
        smallStruct[] smallS = new smallStruct[8];
        smallS[0].a = true;
        smallS[0].b = false;
        largeStruct largeS = Unsafe.As<smallStruct, largeStruct>(ref smallS[0]);
        Debug.Assert((uint)(largeS.a % (UInt32.MaxValue + 1L)) == 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromSStruct3()
    {
        eightByteStruct[] smallS = new eightByteStruct[2];
        smallS[0].a = 1000;
        largeStruct largeS = Unsafe.As<eightByteStruct, largeStruct>(ref smallS[0]);
        Debug.Assert(largeS.a == 1000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromLStruct1()
    {
        eightByteStruct largeS;
        largeS.a = 1;
        smallStruct smallS = Unsafe.As<eightByteStruct, smallStruct>(ref largeS);
        Debug.Assert(smallS.a == true && smallS.b == false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromLStruct2()
    {
        largeStruct largeS;
        largeS.a = 1;
        largeS.b = 2;
        largeS.c = 3.0;
        largeS.d = false;
        largeS.e = 1;
        smallStruct smallS = Unsafe.As<largeStruct, smallStruct>(ref largeS);
        Debug.Assert(smallS.a == true && smallS.b == false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromLStruct3()
    {
        largeStruct largeS;
        largeS.a = 3;
        largeS.b = 2;
        largeS.c = 3.0;
        largeS.d = false;
        largeS.e = 1;
        eightByteStruct smallS = Unsafe.As<largeStruct, eightByteStruct>(ref largeS);
        Debug.Assert(smallS.a == 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void StructFromStruct()
    {
        eightByteStruct s1;
        s1.a = 3;
        eightByteStruct s2 = Unsafe.As<eightByteStruct, eightByteStruct>(ref s1);
        Debug.Assert(s2.a == 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestStructCasts()
    {
        PrimFromSStruct();
        PrimFromLStruct();
        PrimFromStruct();

        StructFromSPrim();
        StructFromLPrim();
        StructFromPrim();

        StructFromSStruct1();
        StructFromSStruct2();
        StructFromSStruct3();

        StructFromLStruct1();
        StructFromLStruct2();
        StructFromLStruct3();

        StructFromStruct();
    }

    #region for the tests below we are expecting only one move instruction to be generated for each.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReturnAsLong(eightByteStruct a)
    {
        return Unsafe.As<eightByteStruct, long>(ref a);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static eightByteStruct ReturnAsEightByteStructFromLong(long a)
    {
        return Unsafe.As<long, eightByteStruct>(ref a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReturnAsDouble(eightByteStruct a)
    {
        return Unsafe.As<eightByteStruct, double>(ref a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static eightByteStruct ReturnAsEightByteStructFromDouble(double a)
    {
        return Unsafe.As<double, eightByteStruct>(ref a);
    }

    struct eightByteStructOverDouble
    {
        public double a;
    }

    static long ReturnAsLong(eightByteStructOverDouble a)
    {
        return Unsafe.As<eightByteStructOverDouble, long>(ref a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static eightByteStructOverDouble ReturnAsEightByteStructOverDoubleFromLong(long a)
    {
        return Unsafe.As<long, eightByteStructOverDouble>(ref a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReturnAsDouble(eightByteStructOverDouble a)
    {
        return Unsafe.As<eightByteStructOverDouble, double>(ref a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static eightByteStructOverDouble ReturnAsEightByteStructOverDoubleFromDouble(double a)
    {
        return Unsafe.As<double, eightByteStructOverDouble>(ref a);
    }

    #endregion

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCastSameSize()
    {
        eightByteStruct e;
        e.a = 32;
        long l = ReturnAsLong(e);
        Debug.Assert(l == 32);
        e = ReturnAsEightByteStructFromLong(l);
        Debug.Assert(e.a == 32);
        double d = ReturnAsDouble(e);
        e = ReturnAsEightByteStructFromDouble(d);
        Debug.Assert(e.a == 32);

        eightByteStructOverDouble ed;
        ed = ReturnAsEightByteStructOverDoubleFromLong(l);
        l = ReturnAsLong(ed);
        Debug.Assert(l == 32);
        d = ReturnAsDouble(ed);
        ed = ReturnAsEightByteStructOverDoubleFromDouble(d);
        l = ReturnAsLong(ed);
        Debug.Assert(e.a == 32);
    }

    struct StructWithVectorField
    {
        public int a;
        public Vector<float> b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestSIMDInit()
    {
        Vector<float> localVector = new Vector<float>();
        StructWithVectorField structWithVectorField;
        structWithVectorField.a = 0;
        structWithVectorField.b = new Vector<float>();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestWhatShouldBeOptimized()
    {
        TestCastSameSize();
        TestSIMDInit();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        TestPrimitiveCasts();
        TestStructCasts();
        TestWhatShouldBeOptimized();
    }
}

#endregion

#region Test merge return blocks
class TestMergeReturnBlocks
{
    struct ReturnStruct
    {
        public float a;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ReturnStruct(int a)
        {
            this.a = a;
        }
    }

    static ReturnStruct TestConstPropogation(int a)
    {
        if (a == 0)
        {
            ReturnStruct s = new ReturnStruct(); // ASG(a, 0);
            return s;
        }
        else if (a == 1)
        {
            ReturnStruct s = new ReturnStruct(1);
            return s;
        }
        else if (a == 2)
        {
            ReturnStruct s;
            s.a = 2;
            return s;
        }
        else if (a == 3)
        {
            ReturnStruct s = new ReturnStruct(3);
            ReturnStruct s2 = s;
            ReturnStruct s3 = s2;
            return s3;
        }
        return new ReturnStruct(4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestConstPropogation()
    {
        TestConstPropogation(5);
    }


    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct StructWithOverlaps
    {
        [FieldOffset(0)]
        public int val;
        [FieldOffset(0)]
        public ReturnStruct s;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public StructWithOverlaps(int v)
        {
            val = 0;
            s.a = v;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ReturnStruct TestNoFieldSeqPropogation(int a)
    {
        StructWithOverlaps s = new StructWithOverlaps();
        if (a == 0)
        {
            return new ReturnStruct();
        }
        else if (a == 1)
        {
            return s.s;
        }
        else if (a == 2)
        {
            StructWithOverlaps s2 = new StructWithOverlaps(2);
            return s2.s;
        }
        else if (a == 3)
        {
            StructWithOverlaps s3 = new StructWithOverlaps(3);
            return s3.s;
        }
        else
        {
            StructWithOverlaps s4 = new StructWithOverlaps(4);
            return s4.s;
        }

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestNoFieldSeqPropogation()
    {
        TestNoFieldSeqPropogation(5);
    }


    public static void Test()
    {
        TestConstPropogation();
        TestNoFieldSeqPropogation();
    }
}
#endregion

class TestStructs
{
    public static int Main()
    {
        TestStructReturns.Test();
        TestUnsafeCasts.Test();
        TestMergeReturnBlocks.Test();
        return 100;
    }
}
