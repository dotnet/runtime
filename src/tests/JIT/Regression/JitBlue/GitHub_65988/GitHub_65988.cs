// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test case, we were skipping the first block that is an alignment candidate,
//       but were not unmarking it such. As a result, we would hit assert during backedge setup.
// Found by Antigen
public class TestClass_65988
{
    public struct S1
    {
        public decimal decimal_0;
    }
    public struct S2
    {
        public S1 s1_2;
    }
    static int s_int_9 = 1;
    decimal decimal_22 = 0.08m;
    S1 s1_33 = new S1();
    public decimal LeafMethod3() => 67.1m;

    public void Method0()
    {
        unchecked
        {
            int int_85 = 76;
            S1 s1_93 = new S1();
            for (; ; )
            {
                if ((15 << 4) < (s_int_9 *= int_85))
                {
                    s1_33.decimal_0 += decimal_22 -= 15 * 4 - -2147483646.9375m;
                }
                s1_93.decimal_0 /= (s1_33.decimal_0 /= LeafMethod3()) + 28;
                if (int_85++ == 77000)
                {
                    break;
                }
            }
            return;
        }
    }
    public static int Main(string[] args)
    {
        new TestClass_65988().Method0();
        return 100;
    }
}