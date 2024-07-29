// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace GCHangCSharp
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            TestClass t = new TestClass();
            List<TestClass.LongStruct> x = t.Test();

            return (x.Count == 1) ? 100 : 101;
        }
    }

    internal class TestClass
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public List<LongStruct> Test()
        {
            List<LongStruct> x = new List<LongStruct>();

            for (int i = 0; i < 1; i++)
            {
                LongStruct ls = new LongStruct();

                if (ls.s1_0 == null) ls.s1_0 = "";
                if (ls.s1_1 == null) ls.s1_1 = "";

                if (ls.i0 == 0) ls.i0 = 1;
                if (ls.i1 == 0) ls.i1 = 1;
                if (ls.i2 == 0) ls.i2 = 1;
                if (ls.i3 == 0) ls.i3 = 1;
                if (ls.i4 == 0) ls.i4 = 1;
                if (ls.i5 == 0) ls.i5 = 1;
                if (ls.i6 == 0) ls.i6 = 1;
                if (ls.i7 == 0) ls.i7 = 1;
                if (ls.i8 == 0) ls.i8 = 1;
                if (ls.i9 == 0) ls.i9 = 1;
                if (ls.i10 == 0) ls.i10 = 1;
                if (ls.i11 == 0) ls.i11 = 1;
                if (ls.i12 == 0) ls.i12 = 1;
                if (ls.i13 == 0) ls.i13 = 1;
                if (ls.i14 == 0) ls.i14 = 1;
                if (ls.i15 == 0) ls.i15 = 1;
                if (ls.i16 == 0) ls.i16 = 1;
                if (ls.i17 == 0) ls.i17 = 1;
                if (ls.i18 == 0) ls.i18 = 1;
                if (ls.i19 == 0) ls.i19 = 1;
                if (ls.i20 == 0) ls.i20 = 1;
                if (ls.i21 == 0) ls.i21 = 1;
                if (ls.i22 == 0) ls.i22 = 1;
                if (ls.i23 == 0) ls.i23 = 1;
                if (ls.i24 == 0) ls.i24 = 1;
                if (ls.i25 == 0) ls.i25 = 1;
                if (ls.i26 == 0) ls.i26 = 1;
                if (ls.i27 == 0) ls.i27 = 1;
                if (ls.i28 == 0) ls.i28 = 1;
                if (ls.i29 == 0) ls.i29 = 1;
                if (ls.i30 == 0) ls.i30 = 1;
                if (ls.i31 == 0) ls.i31 = 1;
                if (ls.i32 == 0) ls.i32 = 1;
                if (ls.i33 == 0) ls.i33 = 1;
                if (ls.i34 == 0) ls.i34 = 1;
                if (ls.i35 == 0) ls.i35 = 1;
                if (ls.i36 == 0) ls.i36 = 1;
                if (ls.i37 == 0) ls.i37 = 1;
                if (ls.i38 == 0) ls.i38 = 1;
                if (ls.i39 == 0) ls.i39 = 1;
                if (ls.i40 == 0) ls.i40 = 1;
                if (ls.i41 == 0) ls.i41 = 1;
                if (ls.i42 == 0) ls.i42 = 1;
                if (ls.i43 == 0) ls.i43 = 1;
                if (ls.i44 == 0) ls.i44 = 1;
                if (ls.i45 == 0) ls.i45 = 1;
                if (ls.i46 == 0) ls.i46 = 1;
                if (ls.i47 == 0) ls.i47 = 1;
                if (ls.i48 == 0) ls.i48 = 1;
                if (ls.i49 == 0) ls.i49 = 1;
                if (ls.i50 == 0) ls.i50 = 1;
                if (ls.i51 == 0) ls.i51 = 1;
                if (ls.i52 == 0) ls.i52 = 1;
                if (ls.i53 == 0) ls.i53 = 1;
                if (ls.i54 == 0) ls.i54 = 1;
                if (ls.i55 == 0) ls.i55 = 1;
                if (ls.i56 == 0) ls.i56 = 1;
                if (ls.i57 == 0) ls.i57 = 1;
                if (ls.i58 == 0) ls.i58 = 1;
                if (ls.i59 == 0) ls.i59 = 1;
                if (ls.i60 == 0) ls.i60 = 1;
                if (ls.i61 == 0) ls.i61 = 1;
                if (ls.i62 == 0) ls.i62 = 1;
                if (ls.i63 == 0) ls.i63 = 1;

                x.Add(ls);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return x;
        }

        public struct LongStruct
        {
            public string s1_0;
            public int i0;
            public int i1;
            public int i2;
            public int i3;
            public int i4;
            public int i5;
            public int i6;
            public int i7;
            public int i8;
            public int i9;
            public int i10;
            public int i11;
            public int i12;
            public int i13;
            public int i14;
            public int i15;
            public int i16;
            public int i17;
            public int i18;
            public int i19;
            public int i20;
            public int i21;
            public int i22;
            public int i23;
            public int i24;
            public int i25;
            public int i26;
            public int i27;
            public int i28;
            public int i29;
            public int i30;
            public int i31;
            public int i32;
            public int i33;
            public int i34;
            public int i35;
            public int i36;
            public int i37;
            public int i38;
            public int i39;
            public int i40;
            public int i41;
            public int i42;
            public int i43;
            public int i44;
            public int i45;
            public int i46;
            public int i47;
            public int i48;
            public int i49;
            public int i50;
            public int i51;
            public int i52;
            public int i53;
            public int i54;
            public int i55;
            public int i56;
            public int i57;
            public int i58;
            public int i59;
            public int i60;
            public int i61;
            public int i62;
            public int i63;
            public string s1_1;
        }
    }
}
