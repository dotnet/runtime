// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test, when S2 struct is returned from Method1, incorrect "str" instruction was being
//       generated while storing the return result into s_s2_16.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
namespace Test_StructWithSingleFloat
{
public class TestClass
{
    public struct S2
    {
        public float float_2;
    }
    static decimal s_decimal_3 = 2.0512820512820512820512820513m;
    static S2 s_s2_16 = new S2();
    decimal decimal_3 = 2.3m;
    public S2 Method1(out decimal p_decimal_0)
    {
        unchecked
        {
            S2 s2_17 = new S2();
            s2_17.float_2 = 1.5f;
            p_decimal_0 = s_decimal_3 + 15 + 4;
            return s2_17;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Method0()
    {
        unchecked
        {
            s_s2_16 = Method1(out decimal_3);
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new TestClass().Method0();
        return s_s2_16.float_2 == 1.5f ? 100 : 0;
    }
}
}
