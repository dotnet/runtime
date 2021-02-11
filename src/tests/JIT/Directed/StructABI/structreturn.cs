// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test register struct returns and local vars retyping cases.

using System;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

class TestHFAandHVA
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float ReturnFloat()
    {
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static double ReturnDouble()
    {
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2 ReturnVector2()
    {
        return new Vector2(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector3 ReturnVector3()
    {
        return new Vector3(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector4 ReturnVector4()
    {
        return new Vector4(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector4 ReturnVector4UsingCall()
    {
        return ReturnVector4();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnPrimitives()
    {
        ReturnFloat();
        ReturnDouble();
        ReturnVector2();
        ReturnVector3();
        ReturnVector4();
        ReturnVector4UsingCall();
    }

    struct FloatWrapper
    {
        public float f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static FloatWrapper ReturnFloatWrapper()
    {
        return new FloatWrapper();
    }

    struct DoubleWrapper
    {
        public double f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static DoubleWrapper ReturnDoubleWrapper()
    {
        return new DoubleWrapper();
    }

    struct Floats2Wrapper
    {
        public float f1;
        public float f2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Floats2Wrapper ReturnFloats2Wrapper()
    {
        return new Floats2Wrapper();
    }

    struct Doubles2Wrapper
    {
        public double f1;
        public double f2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Doubles2Wrapper ReturnDoubles2Wrapper()
    {
        return new Doubles2Wrapper();
    }
    struct Floats3Wrapper
    {
        public float f1;
        public float f2;
        public float f3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Floats3Wrapper ReturnFloats3Wrapper()
    {
        return new Floats3Wrapper();
    }

    struct Doubles3Wrapper
    {
        public double f1;
        public double f2;
        public double f3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Doubles3Wrapper ReturnDoubles3Wrapper()
    {
        return new Doubles3Wrapper();
    }

    struct Floats4Wrapper
    {
        public float f1;
        public float f2;
        public float f3;
        public float f4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Floats4Wrapper ReturnFloats4Wrapper()
    {
        return new Floats4Wrapper();
    }

    struct Doubles4Wrapper
    {
        public double f1;
        public double f2;
        public double f3;
        public double f4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Doubles4Wrapper ReturnDoubles4Wrapper()
    {
        return new Doubles4Wrapper();
    }

    struct Vector2Wrapper
    {
        public Vector2 f1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2Wrapper ReturnVector2Wrapper()
    {
        return new Vector2Wrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2Wrapper ReturnVector2WrapperPromoted()
    {
        var a = new Vector2Wrapper
        {
            f1 = Vector2.Zero
        };

        return a;
    }

    struct Vector3Wrapper
    {
        public Vector3 f1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector3Wrapper ReturnVector3Wrapper()
    {
        return new Vector3Wrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector3Wrapper ReturnVector3WrapperPromoted()
    {
        var a = new Vector3Wrapper
        {
            f1 = Vector3.Zero
        };

        return a;
    }

    struct Vector4Wrapper
    {
        public Vector4 f1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector4Wrapper ReturnVector4Wrapper()
    {
        return new Vector4Wrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector4Wrapper ReturnVector4WrapperPromoted()
    {
        var a = new Vector4Wrapper
        {
            f1 = Vector4.Zero
        };

        return a;
    }

    struct Vector2x2Wrapper
    {
        Vector2 f1;
        Vector2 f2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2x2Wrapper ReturnVector2x2Wrapper()
    {
        return new Vector2x2Wrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnPrimitivesInWrappers()
    {
        ReturnFloatWrapper();
        ReturnDoubleWrapper();
        ReturnFloats2Wrapper();
        ReturnDoubles2Wrapper();
        ReturnFloats3Wrapper();
        ReturnDoubles3Wrapper();
        ReturnFloats4Wrapper();
        ReturnDoubles4Wrapper();
        ReturnVector2Wrapper();
        ReturnVector2WrapperPromoted();
        ReturnVector3Wrapper();
        ReturnVector3WrapperPromoted();
        ReturnVector4Wrapper();
        ReturnVector4WrapperPromoted();
        ReturnVector2x2Wrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ReturnVectorInt()
    {
        return new Vector<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ReturnVectorIntUsingCall()
    {
        var v = ReturnVectorInt();
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> ReturnVectorFloat()
    {
        return new Vector<float>();
    }

    struct A
    {
        bool a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<A> ReturnVectorA()
    {
        return new Vector<A>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> ReturnVectorFloat2()
    {
        return (Vector<float>)ReturnVectorA();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<T> ReturnVectorT<T>() where T : struct
    {
        return new Vector<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<T> ReturnVectorTWithMerge<T>(int v, T init1, T init2, T init3, T init4) where T : struct
    {
        if (v == 0)
        {
            return new Vector<T>();
        }
        else if (v == 1)
        {
            return new Vector<T>(init1);
        }
        else if (v == 2)
        {
            return new Vector<T>(init2);
        }
        else if (v == 3)
        {
            return new Vector<T>(init3);
        }
        else
        {
            return new Vector<T>(init4);
        }
        return new Vector<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<T> ReturnVectorT2<T>(T init) where T : struct
    {
        var a = new Vector<T>();
        var b = new Vector<T>(init);
        var c = new Vector<T>(init);
        var d = a + b + c;
        return d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ReturnVectorInt2<T>(Vector<T> left, Vector<T> right) where T : struct
    {
        Vector<int> cond = (Vector<int>)Vector.LessThan(left, right);
        return cond;
    }

    struct VectorShortWrapper
    {
        public Vector<short> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorShortWrapper ReturnVectorShortWrapper()
    {
        return new VectorShortWrapper();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorShortWrapper ReturnVectorShortWrapperPromoted()
    {
        var a = new VectorShortWrapper()
        {
            f = Vector<short>.Zero
        };
        return a;
    }

    struct VectorLongWrapper
    {
        Vector<long> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorLongWrapper ReturnVectorLongWrapper()
    {
        return new VectorLongWrapper();
    }

    struct VectorDoubleWrapper
    {
        Vector<double> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorDoubleWrapper ReturnVectorDoubleWrapper()
    {
        return new VectorDoubleWrapper();
    }

    struct VectorTWrapper<T> where T : struct
    {
        Vector<T> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorTWrapper<T> ReturnVectorTWrapper<T>() where T : struct
    {
        return new VectorTWrapper<T>();
    }

    struct VectorTWrapperWrapper<T> where T : struct
    {
        VectorTWrapper<T> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorTWrapperWrapper<T> ReturnVectorTWrapperWrapper<T>() where T : struct
    {
        return new VectorTWrapperWrapper<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestReturnViaThrowing<T>() where T : struct
    {
        Vector<T> vector = Vector<T>.One;
        try
        {
            T value = vector[Vector<T>.Count];
            System.Diagnostics.Debug.Assert(false);
        }
        catch (IndexOutOfRangeException)
        {
            return;
        }
        System.Diagnostics.Debug.Assert(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestThrowing()
    {
        TestReturnViaThrowing<byte>();
        TestReturnViaThrowing<sbyte>();
        TestReturnViaThrowing<ushort>();
        TestReturnViaThrowing<short>();
        TestReturnViaThrowing<uint>();
        TestReturnViaThrowing<int>();
        TestReturnViaThrowing<ulong>();
        TestReturnViaThrowing<long>();
        TestReturnViaThrowing<float>();
        TestReturnViaThrowing<double>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnVectorT()
    {
        ReturnVectorInt();
        ReturnVectorIntUsingCall();
        ReturnVectorFloat();
        ReturnVectorT<int>();
        ReturnVectorT<uint>();
        ReturnVectorT<short>();
        ReturnVectorT<long>();
        ReturnVectorT<Vector2>();
        ReturnVectorT<Vector3>();
        ReturnVectorT<Vector4>();
        ReturnVectorT<VectorTWrapperWrapper<int>>();
        ReturnVectorT2<int>(1);
        try
        {
            var a = ReturnVectorT2<Vector4>(new Vector4(1));
            Debug.Assert(false, "unreachable");
        }
        catch (System.NotSupportedException)
        {
        }
        try
        {
            var a = ReturnVectorT2<VectorTWrapperWrapper<int>>(new VectorTWrapperWrapper<int>());
            Debug.Assert(false, "unreachable");
        }
        catch (System.NotSupportedException)
        {
        }
        ReturnVectorInt2<float>(new Vector<float>(1), new Vector<float>(2));
        ReturnVectorInt2<int>(new Vector<int>(1), new Vector<int>(2));

        ReturnVectorTWithMerge(0, 0, 0, 0, 0);
        ReturnVectorTWithMerge(1, 0.0, 0.0, 0.0, 0.0);
        ReturnVectorTWithMerge<short>(2, 0, 0, 0, 0);
        ReturnVectorTWithMerge<long>(3, 0, 0, 0, 0);

        ReturnVectorShortWrapper();
        ReturnVectorShortWrapperPromoted();
        ReturnVectorLongWrapper();
        ReturnVectorDoubleWrapper();
        ReturnVectorTWrapper<bool>();
        ReturnVectorTWrapper<byte>();
        ReturnVectorTWrapperWrapper<int>();
        ReturnVectorTWrapperWrapper<Vector2>();
        ReturnVectorTWrapperWrapper<Vector3>();
        ReturnVectorTWrapperWrapper<float>();

        TestThrowing();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> ReturnVector64Int()
    {
        return System.Runtime.Intrinsics.Vector64.Create(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<double> ReturnVector64Double()
    {
        return System.Runtime.Intrinsics.Vector64.Create(1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> ReturnVector64IntWithMerge(int v)
    {
        switch (v)
        {
            case 0:
                return System.Runtime.Intrinsics.Vector64.Create(0);
            case 1:
                return System.Runtime.Intrinsics.Vector64.Create(1);
            case 2:
                return System.Runtime.Intrinsics.Vector64.Create(2);
            case 3:
                return System.Runtime.Intrinsics.Vector64.Create(3);
            case 4:
                return System.Runtime.Intrinsics.Vector64.Create(4);
            case 5:
                return System.Runtime.Intrinsics.Vector64.Create(5);
        }
        return System.Runtime.Intrinsics.Vector64.Create(6);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnVector64(int v)
    {
        var a = ReturnVector64Int();
        var b = ReturnVector64Double();
        var c = ReturnVector64IntWithMerge(8);
        if (v == 0)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> ReturnVector128Int()
    {
        return System.Runtime.Intrinsics.Vector128.Create(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> ReturnVector128Double()
    {
        return System.Runtime.Intrinsics.Vector128.Create(1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> ReturnVector128IntWithMerge(int v)
    {
        switch (v)
        {
            case 0:
                return System.Runtime.Intrinsics.Vector128.Create(0);
            case 1:
                return System.Runtime.Intrinsics.Vector128.Create(1);
            case 2:
                return System.Runtime.Intrinsics.Vector128.Create(2);
            case 3:
                return System.Runtime.Intrinsics.Vector128.Create(3);
            case 4:
                return System.Runtime.Intrinsics.Vector128.Create(4);
            case 5:
                return System.Runtime.Intrinsics.Vector128.Create(5);
        }
        return System.Runtime.Intrinsics.Vector128.Create(6);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnVector128(int v)
    {
        var a = ReturnVector128Int();
        var b = ReturnVector128Double();
        var c = ReturnVector128IntWithMerge(8);
        if (v == 0)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> ReturnVector256Int()
    {
        return System.Runtime.Intrinsics.Vector256.Create(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<double> ReturnVector256Double()
    {
        return System.Runtime.Intrinsics.Vector256.Create(1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> ReturnVector256IntWithMerge(int v)
    {
        switch (v)
        {
            case 0:
                return System.Runtime.Intrinsics.Vector256.Create(0);
            case 1:
                return System.Runtime.Intrinsics.Vector256.Create(1);
            case 2:
                return System.Runtime.Intrinsics.Vector256.Create(2);
            case 3:
                return System.Runtime.Intrinsics.Vector256.Create(3);
            case 4:
                return System.Runtime.Intrinsics.Vector256.Create(4);
            case 5:
                return System.Runtime.Intrinsics.Vector256.Create(5);
        }
        return System.Runtime.Intrinsics.Vector256.Create(6);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestReturnVector256(int v)
    {
        var a = ReturnVector256Int();
        var b = ReturnVector256Double();
        var c = ReturnVector256IntWithMerge(8);
        if (v == 0)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
        }
    }

    public struct Vector64Wrapped
    {
        public Vector64<float> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector64Wrapped TestReturnVector64WrappedPromoted()
    {
        var a = new Vector64Wrapped
        {
            f = Vector64<float>.Zero
        };
        return a;
    }

    public struct Vector128Wrapped
    {
        public Vector128<short> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128Wrapped TestReturnVector128WrappedPromoted()
    {
        var a = new Vector128Wrapped
        {
            f = Vector128<short>.Zero
        };
        return a;
    }

    public struct Vector256Wrapped
    {
        public Vector256<byte> f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector256Wrapped TestReturnVector256WrappedPromoted()
    {
        var a = new Vector256Wrapped
        {
            f = Vector256<byte>.Zero
        };
        return a;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestReturnVectorNWrappedPromoted()
    {
        TestReturnVector64WrappedPromoted();
        TestReturnVector128WrappedPromoted();
        TestReturnVector256WrappedPromoted();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        TestReturnPrimitives();
        TestReturnPrimitivesInWrappers();
        TestReturnVectorT();
        TestReturnVector64(1);
        TestReturnVector128(1);
        TestReturnVector256(1);
        TestReturnVectorNWrappedPromoted();
    }
}

class TestNon2PowerStructs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Byte3Struct
    {
        public byte f1;
        public byte f2;
        public byte f3;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Byte3Struct(int v)
        {
            f1 = 1;
            f2 = 2;
            f3 = 3;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Byte5Struct
    {
        public byte f1;
        public byte f2;
        public byte f3;
        public byte f4;
        public byte f5;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Byte5Struct(int v)
        {
            f1 = 4;
            f2 = 5;
            f3 = 6;
            f4 = 7;
            f5 = 8;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Byte6Struct
    {
        public byte f1;
        public byte f2;
        public byte f3;
        public byte f4;
        public byte f5;
        public byte f6;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Byte6Struct(int v)
        {
            f1 = 9;
            f2 = 10;
            f3 = 11;
            f4 = 12;
            f5 = 13;
            f6 = 14;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Byte7Struct
    {
        public byte f1;
        public byte f2;
        public byte f3;
        public byte f4;
        public byte f5;
        public byte f6;
        public byte f7;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Byte7Struct(int v)
        {
            f1 = 15;
            f2 = 16;
            f3 = 17;
            f4 = 18;
            f5 = 19;
            f6 = 20;
            f7 = 21;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CompositeOfOddStructs
    {
        public Byte3Struct a;
        public Byte5Struct b;
        public Byte6Struct c;
        public Byte7Struct d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Byte3Struct Return3()
    {
        return new Byte3Struct(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Byte5Struct Return5()
    {
        return new Byte5Struct(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Byte6Struct Return6()
    {
        return new Byte6Struct(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Byte7Struct Return7()
    {
        return new Byte7Struct(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static CompositeOfOddStructs CreateComposite()
    {
        CompositeOfOddStructs c = new CompositeOfOddStructs();
        c.a = Return3();
        c.b = Return5();
        c.c = Return6();
        c.d = Return7();
        return c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestComposite()
    {
        var c = CreateComposite();
        Debug.Assert(c.a.f1 == 1);
        Debug.Assert(c.a.f2 == 2);
        Debug.Assert(c.a.f3 == 3);
        Debug.Assert(c.b.f1 == 4);
        Debug.Assert(c.b.f2 == 5);
        Debug.Assert(c.b.f3 == 6);
        Debug.Assert(c.b.f4 == 7);
        Debug.Assert(c.b.f5 == 8);
        Debug.Assert(c.c.f1 == 9);
        Debug.Assert(c.c.f2 == 10);
        Debug.Assert(c.c.f3 == 11);
        Debug.Assert(c.c.f4 == 12);
        Debug.Assert(c.c.f5 == 13);
        Debug.Assert(c.c.f6 == 14);
        Debug.Assert(c.d.f1 == 15);
        Debug.Assert(c.d.f2 == 16);
        Debug.Assert(c.d.f3 == 17);
        Debug.Assert(c.d.f4 == 18);
        Debug.Assert(c.d.f5 == 19);
        Debug.Assert(c.d.f6 == 20);
        Debug.Assert(c.d.f7 == 21);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte TestLocals(int v)
    {
        var a = Return3();
        var a1 = a;
        a1.f1 = 0;
        var b = Return5();
        var c = Return6();
        var d = Return7();
        if (v == 0)
        {
            return a.f1;
        }
        else if (v == 1)
        {
            return b.f1;
        }
        else if (v == 3)
        {
            return c.f1;
        }
        else if (v == 4)
        {
            return d.f1;
        }
        else
        {
            return a1.f1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        TestComposite();
        TestLocals(0);
    }
}

class TestStructs
{
    public static int Main()
    {
        TestStructReturns.Test();
        TestUnsafeCasts.Test();
        TestMergeReturnBlocks.Test();
        TestHFAandHVA.Test();
        TestNon2PowerStructs.Test();
        return 100;
    }
}
