using System;
using System.Linq;
using System.Reflection;

// Test constant folding for the following patterns:
//  1) (x <op> cns1) <op> (y <op> cns2)
//  2) (x <op> cns1) <op> cns2

class Program
{
    static int Main(string[] args)
    {
        int errors = 0;

        int[] intTestValues = { int.MinValue, int.MinValue / 2, int.MinValue / 2 - 1, -10, 0, 1, 10, int.MaxValue / 2, int.MaxValue / 2 + 1, int.MaxValue };
        ulong[] ulongTestValues = { 0, 1, 10, ulong.MaxValue / 2 - 1, ulong.MaxValue / 2, ulong.MaxValue / 2 + 1, ulong.MaxValue - 1, ulong.MaxValue };

        var obj1 = new TestPattern1_int();
        foreach (MethodInfo method in typeof(TestPattern1_int).GetMethods().Where(m => m.Name.EndsWith("_cns")))
        {
            foreach (int testValue in intTestValues)
            {
                if (!(bool) method.Invoke(obj1, new object[] {testValue}))
                {
                    errors++;
                }
            }
        }

        var obj2 = new TestPattern1_ulong();
        foreach (MethodInfo method in typeof(TestPattern1_ulong).GetMethods().Where(m => m.Name.EndsWith("_cns")))
        {
            foreach (ulong testValue in ulongTestValues)
            {
                if (!(bool)method.Invoke(obj2, new object[] { testValue }))
                {
                    errors++;
                }
            }
        }

        var obj3 = new TestPattern2_int();
        foreach (MethodInfo method in typeof(TestPattern2_int).GetMethods().Where(m => m.Name.EndsWith("_cns")))
        {
            foreach (int testValue1 in intTestValues)
            {
                foreach (int testValue2 in intTestValues)
                {
                    if (!(bool)method.Invoke(obj3, new object[] { testValue1, testValue2 }))
                    {
                        errors++;
                    }
                }
            }
        }


        var obj4 = new TestPattern2_ulong();
        foreach (MethodInfo method in typeof(TestPattern2_ulong).GetMethods().Where(m => m.Name.EndsWith("_cns")))
        {
            foreach (ulong testValue1 in ulongTestValues)
            {
                foreach (ulong testValue2 in ulongTestValues)
                {
                    if (!(bool)method.Invoke(obj4, new object[] { testValue1, testValue2 }))
                    {
                        errors++;
                    }
                }
            }
        }

        new OverflowTests().Test();

        return 100 + errors;
    }
}

// Test constant folding for
// "(x <op> cns1) <op> cns2" pattern
public class TestPattern1_int
{
    public bool Test1_cns(int x) => ((x + 3) + 5) == Test1_var(x);
    public int Test1_var(int x) => (x + ToVar(3)) + ToVar(5);

    public bool Test2_cns(int x) => ((x + 3) - 5) == Test2_var(x);
    public int Test2_var(int x) => (x + ToVar(3)) - ToVar(5);

    public bool Test3_cns(int x) => ((x + 3) | 5) == Test3_var(x);
    public int Test3_var(int x) => (x + ToVar(3)) | ToVar(5);

    public bool Test4_cns(int x) => ((x + 3) & 5) == Test4_var(x);
    public int Test4_var(int x) => (x + ToVar(3)) & ToVar(5);

    public bool Test5_cns(int x) => ((x + 3) ^ 5) == Test5_var(x);
    public int Test5_var(int x) => (x + ToVar(3)) ^ ToVar(5);

    public bool Test6_cns(int x) => ((x - 3) + 5) == Test6_var(x);
    public int Test6_var(int x) => (x - ToVar(3)) + ToVar(5);

    public bool Test7_cns(int x) => ((x - 3) - 5) == Test7_var(x);
    public int Test7_var(int x) => (x - ToVar(3)) - ToVar(5);

    public bool Test8_cns(int x) => ((x - 3) | 5) == Test8_var(x);
    public int Test8_var(int x) => (x - ToVar(3)) | ToVar(5);

    public bool Test9_cns(int x) => ((x - 3) & 5) == Test9_var(x);
    public int Test9_var(int x) => (x - ToVar(3)) & ToVar(5);

    public bool Test10_cns(int x) => ((x - 3) ^ 5) == Test10_var(x);
    public int Test10_var(int x) => (x - ToVar(3)) ^ ToVar(5);

    public bool Test11_cns(int x) => ((x | 3) + 5) == Test11_var(x);
    public int Test11_var(int x) => (x | ToVar(3)) + ToVar(5);

    public bool Test12_cns(int x) => ((x | 3) - 5) == Test12_var(x);
    public int Test12_var(int x) => (x | ToVar(3)) - ToVar(5);

    public bool Test13_cns(int x) => ((x | 3) | 5) == Test13_var(x);
    public int Test13_var(int x) => (x | ToVar(3)) | ToVar(5);

    public bool Test14_cns(int x) => ((x | 3) & 5) == Test14_var(x);
    public int Test14_var(int x) => (x | ToVar(3)) & ToVar(5);

    public bool Test15_cns(int x) => ((x | 3) ^ 5) == Test15_var(x);
    public int Test15_var(int x) => (x | ToVar(3)) ^ ToVar(5);

    public bool Test16_cns(int x) => ((x & 3) + 5) == Test16_var(x);
    public int Test16_var(int x) => (x & ToVar(3)) + ToVar(5);

    public bool Test17_cns(int x) => ((x & 3) - 5) == Test17_var(x);
    public int Test17_var(int x) => (x & ToVar(3)) - ToVar(5);

    public bool Test18_cns(int x) => ((x & 3) | 5) == Test18_var(x);
    public int Test18_var(int x) => (x & ToVar(3)) | ToVar(5);

    public bool Test19_cns(int x) => ((x & 3) & 5) == Test19_var(x);
    public int Test19_var(int x) => (x & ToVar(3)) & ToVar(5);

    public bool Test20_cns(int x) => ((x & 3) ^ 5) == Test20_var(x);
    public int Test20_var(int x) => (x & ToVar(3)) ^ ToVar(5);

    public bool Test21_cns(int x) => ((x ^ 3) + 5) == Test21_var(x);
    public int Test21_var(int x) => (x ^ ToVar(3)) + ToVar(5);

    public bool Test22_cns(int x) => ((x ^ 3) - 5) == Test22_var(x);
    public int Test22_var(int x) => (x ^ ToVar(3)) - ToVar(5);

    public bool Test23_cns(int x) => ((x ^ 3) | 5) == Test23_var(x);
    public int Test23_var(int x) => (x ^ ToVar(3)) | ToVar(5);

    public bool Test24_cns(int x) => ((x ^ 3) & 5) == Test24_var(x);
    public int Test24_var(int x) => (x ^ ToVar(3)) & ToVar(5);

    public bool Test25_cns(int x) => ((x ^ 3) ^ 5) == Test25_var(x);
    public int Test25_var(int x) => (x ^ ToVar(3)) ^ ToVar(5);

    public static T ToVar<T>(T v) => v;
}

// Test constant folding for
// "(x <op> cns1) <op> cns2" pattern
public class TestPattern1_ulong
{
    public bool Test1_cns(ulong x) => ((x + 49UL) + 1024UL) == Test1_var(x);
    public ulong Test1_var(ulong x) => (x + ToVar(49UL)) + ToVar(1024UL);

    public bool Test2_cns(ulong x) => ((x + 49UL) - 1024UL) == Test2_var(x);
    public ulong Test2_var(ulong x) => (x + ToVar(49UL)) - ToVar(1024UL);

    public bool Test3_cns(ulong x) => ((x + 49UL) | 1024UL) == Test3_var(x);
    public ulong Test3_var(ulong x) => (x + ToVar(49UL)) | ToVar(1024UL);

    public bool Test4_cns(ulong x) => ((x + 49UL) & 1024UL) == Test4_var(x);
    public ulong Test4_var(ulong x) => (x + ToVar(49UL)) & ToVar(1024UL);

    public bool Test5_cns(ulong x) => ((x + 49UL) ^ 1024UL) == Test5_var(x);
    public ulong Test5_var(ulong x) => (x + ToVar(49UL)) ^ ToVar(1024UL);

    public bool Test6_cns(ulong x) => ((x - 49UL) + 1024UL) == Test6_var(x);
    public ulong Test6_var(ulong x) => (x - ToVar(49UL)) + ToVar(1024UL);

    public bool Test7_cns(ulong x) => ((x - 49UL) - 1024UL) == Test7_var(x);
    public ulong Test7_var(ulong x) => (x - ToVar(49UL)) - ToVar(1024UL);

    public bool Test8_cns(ulong x) => ((x - 49UL) | 1024UL) == Test8_var(x);
    public ulong Test8_var(ulong x) => (x - ToVar(49UL)) | ToVar(1024UL);

    public bool Test9_cns(ulong x) => ((x - 49UL) & 1024UL) == Test9_var(x);
    public ulong Test9_var(ulong x) => (x - ToVar(49UL)) & ToVar(1024UL);

    public bool Test10_cns(ulong x) => ((x - 49UL) ^ 1024UL) == Test10_var(x);
    public ulong Test10_var(ulong x) => (x - ToVar(49UL)) ^ ToVar(1024UL);

    public bool Test11_cns(ulong x) => ((x | 49UL) + 1024UL) == Test11_var(x);
    public ulong Test11_var(ulong x) => (x | ToVar(49UL)) + ToVar(1024UL);

    public bool Test12_cns(ulong x) => ((x | 49UL) - 1024UL) == Test12_var(x);
    public ulong Test12_var(ulong x) => (x | ToVar(49UL)) - ToVar(1024UL);

    public bool Test13_cns(ulong x) => ((x | 49UL) | 1024UL) == Test13_var(x);
    public ulong Test13_var(ulong x) => (x | ToVar(49UL)) | ToVar(1024UL);

    public bool Test14_cns(ulong x) => ((x | 49UL) & 1024UL) == Test14_var(x);
    public ulong Test14_var(ulong x) => (x | ToVar(49UL)) & ToVar(1024UL);

    public bool Test15_cns(ulong x) => ((x | 49UL) ^ 1024UL) == Test15_var(x);
    public ulong Test15_var(ulong x) => (x | ToVar(49UL)) ^ ToVar(1024UL);

    public bool Test16_cns(ulong x) => ((x & 49UL) + 1024UL) == Test16_var(x);
    public ulong Test16_var(ulong x) => (x & ToVar(49UL)) + ToVar(1024UL);

    public bool Test17_cns(ulong x) => ((x & 49UL) - 1024UL) == Test17_var(x);
    public ulong Test17_var(ulong x) => (x & ToVar(49UL)) - ToVar(1024UL);

    public bool Test18_cns(ulong x) => ((x & 49UL) | 1024UL) == Test18_var(x);
    public ulong Test18_var(ulong x) => (x & ToVar(49UL)) | ToVar(1024UL);

    public bool Test19_cns(ulong x) => ((x & 49UL) & 1024UL) == Test19_var(x);
    public ulong Test19_var(ulong x) => (x & ToVar(49UL)) & ToVar(1024UL);

    public bool Test20_cns(ulong x) => ((x & 49UL) ^ 1024UL) == Test20_var(x);
    public ulong Test20_var(ulong x) => (x & ToVar(49UL)) ^ ToVar(1024UL);

    public bool Test21_cns(ulong x) => ((x ^ 49UL) + 1024UL) == Test21_var(x);
    public ulong Test21_var(ulong x) => (x ^ ToVar(49UL)) + ToVar(1024UL);

    public bool Test22_cns(ulong x) => ((x ^ 49UL) - 1024UL) == Test22_var(x);
    public ulong Test22_var(ulong x) => (x ^ ToVar(49UL)) - ToVar(1024UL);

    public bool Test23_cns(ulong x) => ((x ^ 49UL) | 1024UL) == Test23_var(x);
    public ulong Test23_var(ulong x) => (x ^ ToVar(49UL)) | ToVar(1024UL);

    public bool Test24_cns(ulong x) => ((x ^ 49UL) & 1024UL) == Test24_var(x);
    public ulong Test24_var(ulong x) => (x ^ ToVar(49UL)) & ToVar(1024UL);

    public bool Test25_cns(ulong x) => ((x ^ 49UL) ^ 1024UL) == Test25_var(x);
    public ulong Test25_var(ulong x) => (x ^ ToVar(49UL)) ^ ToVar(1024UL);

    public static T ToVar<T>(T v) => v;
}

// Test constant folding for
// "(x <op> cns1) <op> (y <op> cns2)" pattern
public class TestPattern2_int
{
    public bool Test1_cns(int x, int y) => ((x + 3) + (y + 5)) == Test1_var(x, y);
    public int Test1_var(int x, int y) => (x + ToVar(3)) + (y + ToVar(5));

    public bool Test2_cns(int x, int y) => ((x + 3) + (y - 5)) == Test2_var(x, y);
    public int Test2_var(int x, int y) => (x + ToVar(3)) + (y - ToVar(5));

    public bool Test3_cns(int x, int y) => ((x + 3) + (y | 5)) == Test3_var(x, y);
    public int Test3_var(int x, int y) => (x + ToVar(3)) + (y | ToVar(5));

    public bool Test4_cns(int x, int y) => ((x + 3) + (y & 5)) == Test4_var(x, y);
    public int Test4_var(int x, int y) => (x + ToVar(3)) + (y & ToVar(5));

    public bool Test5_cns(int x, int y) => ((x + 3) + (y ^ 5)) == Test5_var(x, y);
    public int Test5_var(int x, int y) => (x + ToVar(3)) + (y ^ ToVar(5));

    public bool Test6_cns(int x, int y) => ((x + 3) - (y + 5)) == Test6_var(x, y);
    public int Test6_var(int x, int y) => (x + ToVar(3)) - (y + ToVar(5));

    public bool Test7_cns(int x, int y) => ((x + 3) - (y - 5)) == Test7_var(x, y);
    public int Test7_var(int x, int y) => (x + ToVar(3)) - (y - ToVar(5));

    public bool Test8_cns(int x, int y) => ((x + 3) - (y | 5)) == Test8_var(x, y);
    public int Test8_var(int x, int y) => (x + ToVar(3)) - (y | ToVar(5));

    public bool Test9_cns(int x, int y) => ((x + 3) - (y & 5)) == Test9_var(x, y);
    public int Test9_var(int x, int y) => (x + ToVar(3)) - (y & ToVar(5));

    public bool Test10_cns(int x, int y) => ((x + 3) - (y ^ 5)) == Test10_var(x, y);
    public int Test10_var(int x, int y) => (x + ToVar(3)) - (y ^ ToVar(5));

    public bool Test11_cns(int x, int y) => ((x + 3) | (y + 5)) == Test11_var(x, y);
    public int Test11_var(int x, int y) => (x + ToVar(3)) | (y + ToVar(5));

    public bool Test12_cns(int x, int y) => ((x + 3) | (y - 5)) == Test12_var(x, y);
    public int Test12_var(int x, int y) => (x + ToVar(3)) | (y - ToVar(5));

    public bool Test13_cns(int x, int y) => ((x + 3) | (y | 5)) == Test13_var(x, y);
    public int Test13_var(int x, int y) => (x + ToVar(3)) | (y | ToVar(5));

    public bool Test14_cns(int x, int y) => ((x + 3) | (y & 5)) == Test14_var(x, y);
    public int Test14_var(int x, int y) => (x + ToVar(3)) | (y & ToVar(5));

    public bool Test15_cns(int x, int y) => ((x + 3) | (y ^ 5)) == Test15_var(x, y);
    public int Test15_var(int x, int y) => (x + ToVar(3)) | (y ^ ToVar(5));

    public bool Test16_cns(int x, int y) => ((x + 3) & (y + 5)) == Test16_var(x, y);
    public int Test16_var(int x, int y) => (x + ToVar(3)) & (y + ToVar(5));

    public bool Test17_cns(int x, int y) => ((x + 3) & (y - 5)) == Test17_var(x, y);
    public int Test17_var(int x, int y) => (x + ToVar(3)) & (y - ToVar(5));

    public bool Test18_cns(int x, int y) => ((x + 3) & (y | 5)) == Test18_var(x, y);
    public int Test18_var(int x, int y) => (x + ToVar(3)) & (y | ToVar(5));

    public bool Test19_cns(int x, int y) => ((x + 3) & (y & 5)) == Test19_var(x, y);
    public int Test19_var(int x, int y) => (x + ToVar(3)) & (y & ToVar(5));

    public bool Test20_cns(int x, int y) => ((x + 3) & (y ^ 5)) == Test20_var(x, y);
    public int Test20_var(int x, int y) => (x + ToVar(3)) & (y ^ ToVar(5));

    public bool Test21_cns(int x, int y) => ((x + 3) ^ (y + 5)) == Test21_var(x, y);
    public int Test21_var(int x, int y) => (x + ToVar(3)) ^ (y + ToVar(5));

    public bool Test22_cns(int x, int y) => ((x + 3) ^ (y - 5)) == Test22_var(x, y);
    public int Test22_var(int x, int y) => (x + ToVar(3)) ^ (y - ToVar(5));

    public bool Test23_cns(int x, int y) => ((x + 3) ^ (y | 5)) == Test23_var(x, y);
    public int Test23_var(int x, int y) => (x + ToVar(3)) ^ (y | ToVar(5));

    public bool Test24_cns(int x, int y) => ((x + 3) ^ (y & 5)) == Test24_var(x, y);
    public int Test24_var(int x, int y) => (x + ToVar(3)) ^ (y & ToVar(5));

    public bool Test25_cns(int x, int y) => ((x + 3) ^ (y ^ 5)) == Test25_var(x, y);
    public int Test25_var(int x, int y) => (x + ToVar(3)) ^ (y ^ ToVar(5));

    public bool Test26_cns(int x, int y) => ((x - 3) + (y + 5)) == Test26_var(x, y);
    public int Test26_var(int x, int y) => (x - ToVar(3)) + (y + ToVar(5));

    public bool Test27_cns(int x, int y) => ((x - 3) + (y - 5)) == Test27_var(x, y);
    public int Test27_var(int x, int y) => (x - ToVar(3)) + (y - ToVar(5));

    public bool Test28_cns(int x, int y) => ((x - 3) + (y | 5)) == Test28_var(x, y);
    public int Test28_var(int x, int y) => (x - ToVar(3)) + (y | ToVar(5));

    public bool Test29_cns(int x, int y) => ((x - 3) + (y & 5)) == Test29_var(x, y);
    public int Test29_var(int x, int y) => (x - ToVar(3)) + (y & ToVar(5));

    public bool Test30_cns(int x, int y) => ((x - 3) + (y ^ 5)) == Test30_var(x, y);
    public int Test30_var(int x, int y) => (x - ToVar(3)) + (y ^ ToVar(5));

    public bool Test31_cns(int x, int y) => ((x - 3) - (y + 5)) == Test31_var(x, y);
    public int Test31_var(int x, int y) => (x - ToVar(3)) - (y + ToVar(5));

    public bool Test32_cns(int x, int y) => ((x - 3) - (y - 5)) == Test32_var(x, y);
    public int Test32_var(int x, int y) => (x - ToVar(3)) - (y - ToVar(5));

    public bool Test33_cns(int x, int y) => ((x - 3) - (y | 5)) == Test33_var(x, y);
    public int Test33_var(int x, int y) => (x - ToVar(3)) - (y | ToVar(5));

    public bool Test34_cns(int x, int y) => ((x - 3) - (y & 5)) == Test34_var(x, y);
    public int Test34_var(int x, int y) => (x - ToVar(3)) - (y & ToVar(5));

    public bool Test35_cns(int x, int y) => ((x - 3) - (y ^ 5)) == Test35_var(x, y);
    public int Test35_var(int x, int y) => (x - ToVar(3)) - (y ^ ToVar(5));

    public bool Test36_cns(int x, int y) => ((x - 3) | (y + 5)) == Test36_var(x, y);
    public int Test36_var(int x, int y) => (x - ToVar(3)) | (y + ToVar(5));

    public bool Test37_cns(int x, int y) => ((x - 3) | (y - 5)) == Test37_var(x, y);
    public int Test37_var(int x, int y) => (x - ToVar(3)) | (y - ToVar(5));

    public bool Test38_cns(int x, int y) => ((x - 3) | (y | 5)) == Test38_var(x, y);
    public int Test38_var(int x, int y) => (x - ToVar(3)) | (y | ToVar(5));

    public bool Test39_cns(int x, int y) => ((x - 3) | (y & 5)) == Test39_var(x, y);
    public int Test39_var(int x, int y) => (x - ToVar(3)) | (y & ToVar(5));

    public bool Test40_cns(int x, int y) => ((x - 3) | (y ^ 5)) == Test40_var(x, y);
    public int Test40_var(int x, int y) => (x - ToVar(3)) | (y ^ ToVar(5));

    public bool Test41_cns(int x, int y) => ((x - 3) & (y + 5)) == Test41_var(x, y);
    public int Test41_var(int x, int y) => (x - ToVar(3)) & (y + ToVar(5));

    public bool Test42_cns(int x, int y) => ((x - 3) & (y - 5)) == Test42_var(x, y);
    public int Test42_var(int x, int y) => (x - ToVar(3)) & (y - ToVar(5));

    public bool Test43_cns(int x, int y) => ((x - 3) & (y | 5)) == Test43_var(x, y);
    public int Test43_var(int x, int y) => (x - ToVar(3)) & (y | ToVar(5));

    public bool Test44_cns(int x, int y) => ((x - 3) & (y & 5)) == Test44_var(x, y);
    public int Test44_var(int x, int y) => (x - ToVar(3)) & (y & ToVar(5));

    public bool Test45_cns(int x, int y) => ((x - 3) & (y ^ 5)) == Test45_var(x, y);
    public int Test45_var(int x, int y) => (x - ToVar(3)) & (y ^ ToVar(5));

    public bool Test46_cns(int x, int y) => ((x - 3) ^ (y + 5)) == Test46_var(x, y);
    public int Test46_var(int x, int y) => (x - ToVar(3)) ^ (y + ToVar(5));

    public bool Test47_cns(int x, int y) => ((x - 3) ^ (y - 5)) == Test47_var(x, y);
    public int Test47_var(int x, int y) => (x - ToVar(3)) ^ (y - ToVar(5));

    public bool Test48_cns(int x, int y) => ((x - 3) ^ (y | 5)) == Test48_var(x, y);
    public int Test48_var(int x, int y) => (x - ToVar(3)) ^ (y | ToVar(5));

    public bool Test49_cns(int x, int y) => ((x - 3) ^ (y & 5)) == Test49_var(x, y);
    public int Test49_var(int x, int y) => (x - ToVar(3)) ^ (y & ToVar(5));

    public bool Test50_cns(int x, int y) => ((x - 3) ^ (y ^ 5)) == Test50_var(x, y);
    public int Test50_var(int x, int y) => (x - ToVar(3)) ^ (y ^ ToVar(5));

    public bool Test51_cns(int x, int y) => ((x | 3) + (y + 5)) == Test51_var(x, y);
    public int Test51_var(int x, int y) => (x | ToVar(3)) + (y + ToVar(5));

    public bool Test52_cns(int x, int y) => ((x | 3) + (y - 5)) == Test52_var(x, y);
    public int Test52_var(int x, int y) => (x | ToVar(3)) + (y - ToVar(5));

    public bool Test53_cns(int x, int y) => ((x | 3) + (y | 5)) == Test53_var(x, y);
    public int Test53_var(int x, int y) => (x | ToVar(3)) + (y | ToVar(5));

    public bool Test54_cns(int x, int y) => ((x | 3) + (y & 5)) == Test54_var(x, y);
    public int Test54_var(int x, int y) => (x | ToVar(3)) + (y & ToVar(5));

    public bool Test55_cns(int x, int y) => ((x | 3) + (y ^ 5)) == Test55_var(x, y);
    public int Test55_var(int x, int y) => (x | ToVar(3)) + (y ^ ToVar(5));

    public bool Test56_cns(int x, int y) => ((x | 3) - (y + 5)) == Test56_var(x, y);
    public int Test56_var(int x, int y) => (x | ToVar(3)) - (y + ToVar(5));

    public bool Test57_cns(int x, int y) => ((x | 3) - (y - 5)) == Test57_var(x, y);
    public int Test57_var(int x, int y) => (x | ToVar(3)) - (y - ToVar(5));

    public bool Test58_cns(int x, int y) => ((x | 3) - (y | 5)) == Test58_var(x, y);
    public int Test58_var(int x, int y) => (x | ToVar(3)) - (y | ToVar(5));

    public bool Test59_cns(int x, int y) => ((x | 3) - (y & 5)) == Test59_var(x, y);
    public int Test59_var(int x, int y) => (x | ToVar(3)) - (y & ToVar(5));

    public bool Test60_cns(int x, int y) => ((x | 3) - (y ^ 5)) == Test60_var(x, y);
    public int Test60_var(int x, int y) => (x | ToVar(3)) - (y ^ ToVar(5));

    public bool Test61_cns(int x, int y) => ((x | 3) | (y + 5)) == Test61_var(x, y);
    public int Test61_var(int x, int y) => (x | ToVar(3)) | (y + ToVar(5));

    public bool Test62_cns(int x, int y) => ((x | 3) | (y - 5)) == Test62_var(x, y);
    public int Test62_var(int x, int y) => (x | ToVar(3)) | (y - ToVar(5));

    public bool Test63_cns(int x, int y) => ((x | 3) | (y | 5)) == Test63_var(x, y);
    public int Test63_var(int x, int y) => (x | ToVar(3)) | (y | ToVar(5));

    public bool Test64_cns(int x, int y) => ((x | 3) | (y & 5)) == Test64_var(x, y);
    public int Test64_var(int x, int y) => (x | ToVar(3)) | (y & ToVar(5));

    public bool Test65_cns(int x, int y) => ((x | 3) | (y ^ 5)) == Test65_var(x, y);
    public int Test65_var(int x, int y) => (x | ToVar(3)) | (y ^ ToVar(5));

    public bool Test66_cns(int x, int y) => ((x | 3) & (y + 5)) == Test66_var(x, y);
    public int Test66_var(int x, int y) => (x | ToVar(3)) & (y + ToVar(5));

    public bool Test67_cns(int x, int y) => ((x | 3) & (y - 5)) == Test67_var(x, y);
    public int Test67_var(int x, int y) => (x | ToVar(3)) & (y - ToVar(5));

    public bool Test68_cns(int x, int y) => ((x | 3) & (y | 5)) == Test68_var(x, y);
    public int Test68_var(int x, int y) => (x | ToVar(3)) & (y | ToVar(5));

    public bool Test69_cns(int x, int y) => ((x | 3) & (y & 5)) == Test69_var(x, y);
    public int Test69_var(int x, int y) => (x | ToVar(3)) & (y & ToVar(5));

    public bool Test70_cns(int x, int y) => ((x | 3) & (y ^ 5)) == Test70_var(x, y);
    public int Test70_var(int x, int y) => (x | ToVar(3)) & (y ^ ToVar(5));

    public bool Test71_cns(int x, int y) => ((x | 3) ^ (y + 5)) == Test71_var(x, y);
    public int Test71_var(int x, int y) => (x | ToVar(3)) ^ (y + ToVar(5));

    public bool Test72_cns(int x, int y) => ((x | 3) ^ (y - 5)) == Test72_var(x, y);
    public int Test72_var(int x, int y) => (x | ToVar(3)) ^ (y - ToVar(5));

    public bool Test73_cns(int x, int y) => ((x | 3) ^ (y | 5)) == Test73_var(x, y);
    public int Test73_var(int x, int y) => (x | ToVar(3)) ^ (y | ToVar(5));

    public bool Test74_cns(int x, int y) => ((x | 3) ^ (y & 5)) == Test74_var(x, y);
    public int Test74_var(int x, int y) => (x | ToVar(3)) ^ (y & ToVar(5));

    public bool Test75_cns(int x, int y) => ((x | 3) ^ (y ^ 5)) == Test75_var(x, y);
    public int Test75_var(int x, int y) => (x | ToVar(3)) ^ (y ^ ToVar(5));

    public bool Test76_cns(int x, int y) => ((x & 3) + (y + 5)) == Test76_var(x, y);
    public int Test76_var(int x, int y) => (x & ToVar(3)) + (y + ToVar(5));

    public bool Test77_cns(int x, int y) => ((x & 3) + (y - 5)) == Test77_var(x, y);
    public int Test77_var(int x, int y) => (x & ToVar(3)) + (y - ToVar(5));

    public bool Test78_cns(int x, int y) => ((x & 3) + (y | 5)) == Test78_var(x, y);
    public int Test78_var(int x, int y) => (x & ToVar(3)) + (y | ToVar(5));

    public bool Test79_cns(int x, int y) => ((x & 3) + (y & 5)) == Test79_var(x, y);
    public int Test79_var(int x, int y) => (x & ToVar(3)) + (y & ToVar(5));

    public bool Test80_cns(int x, int y) => ((x & 3) + (y ^ 5)) == Test80_var(x, y);
    public int Test80_var(int x, int y) => (x & ToVar(3)) + (y ^ ToVar(5));

    public bool Test81_cns(int x, int y) => ((x & 3) - (y + 5)) == Test81_var(x, y);
    public int Test81_var(int x, int y) => (x & ToVar(3)) - (y + ToVar(5));

    public bool Test82_cns(int x, int y) => ((x & 3) - (y - 5)) == Test82_var(x, y);
    public int Test82_var(int x, int y) => (x & ToVar(3)) - (y - ToVar(5));

    public bool Test83_cns(int x, int y) => ((x & 3) - (y | 5)) == Test83_var(x, y);
    public int Test83_var(int x, int y) => (x & ToVar(3)) - (y | ToVar(5));

    public bool Test84_cns(int x, int y) => ((x & 3) - (y & 5)) == Test84_var(x, y);
    public int Test84_var(int x, int y) => (x & ToVar(3)) - (y & ToVar(5));

    public bool Test85_cns(int x, int y) => ((x & 3) - (y ^ 5)) == Test85_var(x, y);
    public int Test85_var(int x, int y) => (x & ToVar(3)) - (y ^ ToVar(5));

    public bool Test86_cns(int x, int y) => ((x & 3) | (y + 5)) == Test86_var(x, y);
    public int Test86_var(int x, int y) => (x & ToVar(3)) | (y + ToVar(5));

    public bool Test87_cns(int x, int y) => ((x & 3) | (y - 5)) == Test87_var(x, y);
    public int Test87_var(int x, int y) => (x & ToVar(3)) | (y - ToVar(5));

    public bool Test88_cns(int x, int y) => ((x & 3) | (y | 5)) == Test88_var(x, y);
    public int Test88_var(int x, int y) => (x & ToVar(3)) | (y | ToVar(5));

    public bool Test89_cns(int x, int y) => ((x & 3) | (y & 5)) == Test89_var(x, y);
    public int Test89_var(int x, int y) => (x & ToVar(3)) | (y & ToVar(5));

    public bool Test90_cns(int x, int y) => ((x & 3) | (y ^ 5)) == Test90_var(x, y);
    public int Test90_var(int x, int y) => (x & ToVar(3)) | (y ^ ToVar(5));

    public bool Test91_cns(int x, int y) => ((x & 3) & (y + 5)) == Test91_var(x, y);
    public int Test91_var(int x, int y) => (x & ToVar(3)) & (y + ToVar(5));

    public bool Test92_cns(int x, int y) => ((x & 3) & (y - 5)) == Test92_var(x, y);
    public int Test92_var(int x, int y) => (x & ToVar(3)) & (y - ToVar(5));

    public bool Test93_cns(int x, int y) => ((x & 3) & (y | 5)) == Test93_var(x, y);
    public int Test93_var(int x, int y) => (x & ToVar(3)) & (y | ToVar(5));

    public bool Test94_cns(int x, int y) => ((x & 3) & (y & 5)) == Test94_var(x, y);
    public int Test94_var(int x, int y) => (x & ToVar(3)) & (y & ToVar(5));

    public bool Test95_cns(int x, int y) => ((x & 3) & (y ^ 5)) == Test95_var(x, y);
    public int Test95_var(int x, int y) => (x & ToVar(3)) & (y ^ ToVar(5));

    public bool Test96_cns(int x, int y) => ((x & 3) ^ (y + 5)) == Test96_var(x, y);
    public int Test96_var(int x, int y) => (x & ToVar(3)) ^ (y + ToVar(5));

    public bool Test97_cns(int x, int y) => ((x & 3) ^ (y - 5)) == Test97_var(x, y);
    public int Test97_var(int x, int y) => (x & ToVar(3)) ^ (y - ToVar(5));

    public bool Test98_cns(int x, int y) => ((x & 3) ^ (y | 5)) == Test98_var(x, y);
    public int Test98_var(int x, int y) => (x & ToVar(3)) ^ (y | ToVar(5));

    public bool Test99_cns(int x, int y) => ((x & 3) ^ (y & 5)) == Test99_var(x, y);
    public int Test99_var(int x, int y) => (x & ToVar(3)) ^ (y & ToVar(5));

    public bool Test100_cns(int x, int y) => ((x & 3) ^ (y ^ 5)) == Test100_var(x, y);
    public int Test100_var(int x, int y) => (x & ToVar(3)) ^ (y ^ ToVar(5));

    public bool Test101_cns(int x, int y) => ((x ^ 3) + (y + 5)) == Test101_var(x, y);
    public int Test101_var(int x, int y) => (x ^ ToVar(3)) + (y + ToVar(5));

    public bool Test102_cns(int x, int y) => ((x ^ 3) + (y - 5)) == Test102_var(x, y);
    public int Test102_var(int x, int y) => (x ^ ToVar(3)) + (y - ToVar(5));

    public bool Test103_cns(int x, int y) => ((x ^ 3) + (y | 5)) == Test103_var(x, y);
    public int Test103_var(int x, int y) => (x ^ ToVar(3)) + (y | ToVar(5));

    public bool Test104_cns(int x, int y) => ((x ^ 3) + (y & 5)) == Test104_var(x, y);
    public int Test104_var(int x, int y) => (x ^ ToVar(3)) + (y & ToVar(5));

    public bool Test105_cns(int x, int y) => ((x ^ 3) + (y ^ 5)) == Test105_var(x, y);
    public int Test105_var(int x, int y) => (x ^ ToVar(3)) + (y ^ ToVar(5));

    public bool Test106_cns(int x, int y) => ((x ^ 3) - (y + 5)) == Test106_var(x, y);
    public int Test106_var(int x, int y) => (x ^ ToVar(3)) - (y + ToVar(5));

    public bool Test107_cns(int x, int y) => ((x ^ 3) - (y - 5)) == Test107_var(x, y);
    public int Test107_var(int x, int y) => (x ^ ToVar(3)) - (y - ToVar(5));

    public bool Test108_cns(int x, int y) => ((x ^ 3) - (y | 5)) == Test108_var(x, y);
    public int Test108_var(int x, int y) => (x ^ ToVar(3)) - (y | ToVar(5));

    public bool Test109_cns(int x, int y) => ((x ^ 3) - (y & 5)) == Test109_var(x, y);
    public int Test109_var(int x, int y) => (x ^ ToVar(3)) - (y & ToVar(5));

    public bool Test110_cns(int x, int y) => ((x ^ 3) - (y ^ 5)) == Test110_var(x, y);
    public int Test110_var(int x, int y) => (x ^ ToVar(3)) - (y ^ ToVar(5));

    public bool Test111_cns(int x, int y) => ((x ^ 3) | (y + 5)) == Test111_var(x, y);
    public int Test111_var(int x, int y) => (x ^ ToVar(3)) | (y + ToVar(5));

    public bool Test112_cns(int x, int y) => ((x ^ 3) | (y - 5)) == Test112_var(x, y);
    public int Test112_var(int x, int y) => (x ^ ToVar(3)) | (y - ToVar(5));

    public bool Test113_cns(int x, int y) => ((x ^ 3) | (y | 5)) == Test113_var(x, y);
    public int Test113_var(int x, int y) => (x ^ ToVar(3)) | (y | ToVar(5));

    public bool Test114_cns(int x, int y) => ((x ^ 3) | (y & 5)) == Test114_var(x, y);
    public int Test114_var(int x, int y) => (x ^ ToVar(3)) | (y & ToVar(5));

    public bool Test115_cns(int x, int y) => ((x ^ 3) | (y ^ 5)) == Test115_var(x, y);
    public int Test115_var(int x, int y) => (x ^ ToVar(3)) | (y ^ ToVar(5));

    public bool Test116_cns(int x, int y) => ((x ^ 3) & (y + 5)) == Test116_var(x, y);
    public int Test116_var(int x, int y) => (x ^ ToVar(3)) & (y + ToVar(5));

    public bool Test117_cns(int x, int y) => ((x ^ 3) & (y - 5)) == Test117_var(x, y);
    public int Test117_var(int x, int y) => (x ^ ToVar(3)) & (y - ToVar(5));

    public bool Test118_cns(int x, int y) => ((x ^ 3) & (y | 5)) == Test118_var(x, y);
    public int Test118_var(int x, int y) => (x ^ ToVar(3)) & (y | ToVar(5));

    public bool Test119_cns(int x, int y) => ((x ^ 3) & (y & 5)) == Test119_var(x, y);
    public int Test119_var(int x, int y) => (x ^ ToVar(3)) & (y & ToVar(5));

    public bool Test120_cns(int x, int y) => ((x ^ 3) & (y ^ 5)) == Test120_var(x, y);
    public int Test120_var(int x, int y) => (x ^ ToVar(3)) & (y ^ ToVar(5));

    public bool Test121_cns(int x, int y) => ((x ^ 3) ^ (y + 5)) == Test121_var(x, y);
    public int Test121_var(int x, int y) => (x ^ ToVar(3)) ^ (y + ToVar(5));

    public bool Test122_cns(int x, int y) => ((x ^ 3) ^ (y - 5)) == Test122_var(x, y);
    public int Test122_var(int x, int y) => (x ^ ToVar(3)) ^ (y - ToVar(5));

    public bool Test123_cns(int x, int y) => ((x ^ 3) ^ (y | 5)) == Test123_var(x, y);
    public int Test123_var(int x, int y) => (x ^ ToVar(3)) ^ (y | ToVar(5));

    public bool Test124_cns(int x, int y) => ((x ^ 3) ^ (y & 5)) == Test124_var(x, y);
    public int Test124_var(int x, int y) => (x ^ ToVar(3)) ^ (y & ToVar(5));

    public bool Test125_cns(int x, int y) => ((x ^ 3) ^ (y ^ 5)) == Test125_var(x, y);
    public int Test125_var(int x, int y) => (x ^ ToVar(3)) ^ (y ^ ToVar(5));

    public static T ToVar<T>(T v) => v;
}

// Test constant folding for
// "(x <op> cns1) <op> (y <op> cns2)" pattern
public class TestPattern2_ulong
{
    public bool Test1_cns(ulong x, ulong y) => ((x + 12345UL) + (y + 49UL)) == Test1_var(x, y);
    public ulong Test1_var(ulong x, ulong y) => (x + ToVar(12345UL)) + (y + ToVar(49UL));

    public bool Test2_cns(ulong x, ulong y) => ((x + 12345UL) + (y - 49UL)) == Test2_var(x, y);
    public ulong Test2_var(ulong x, ulong y) => (x + ToVar(12345UL)) + (y - ToVar(49UL));

    public bool Test3_cns(ulong x, ulong y) => ((x + 12345UL) + (y | 49UL)) == Test3_var(x, y);
    public ulong Test3_var(ulong x, ulong y) => (x + ToVar(12345UL)) + (y | ToVar(49UL));

    public bool Test4_cns(ulong x, ulong y) => ((x + 12345UL) + (y & 49UL)) == Test4_var(x, y);
    public ulong Test4_var(ulong x, ulong y) => (x + ToVar(12345UL)) + (y & ToVar(49UL));

    public bool Test5_cns(ulong x, ulong y) => ((x + 12345UL) + (y ^ 49UL)) == Test5_var(x, y);
    public ulong Test5_var(ulong x, ulong y) => (x + ToVar(12345UL)) + (y ^ ToVar(49UL));

    public bool Test6_cns(ulong x, ulong y) => ((x + 12345UL) - (y + 49UL)) == Test6_var(x, y);
    public ulong Test6_var(ulong x, ulong y) => (x + ToVar(12345UL)) - (y + ToVar(49UL));

    public bool Test7_cns(ulong x, ulong y) => ((x + 12345UL) - (y - 49UL)) == Test7_var(x, y);
    public ulong Test7_var(ulong x, ulong y) => (x + ToVar(12345UL)) - (y - ToVar(49UL));

    public bool Test8_cns(ulong x, ulong y) => ((x + 12345UL) - (y | 49UL)) == Test8_var(x, y);
    public ulong Test8_var(ulong x, ulong y) => (x + ToVar(12345UL)) - (y | ToVar(49UL));

    public bool Test9_cns(ulong x, ulong y) => ((x + 12345UL) - (y & 49UL)) == Test9_var(x, y);
    public ulong Test9_var(ulong x, ulong y) => (x + ToVar(12345UL)) - (y & ToVar(49UL));

    public bool Test10_cns(ulong x, ulong y) => ((x + 12345UL) - (y ^ 49UL)) == Test10_var(x, y);
    public ulong Test10_var(ulong x, ulong y) => (x + ToVar(12345UL)) - (y ^ ToVar(49UL));

    public bool Test11_cns(ulong x, ulong y) => ((x + 12345UL) | (y + 49UL)) == Test11_var(x, y);
    public ulong Test11_var(ulong x, ulong y) => (x + ToVar(12345UL)) | (y + ToVar(49UL));

    public bool Test12_cns(ulong x, ulong y) => ((x + 12345UL) | (y - 49UL)) == Test12_var(x, y);
    public ulong Test12_var(ulong x, ulong y) => (x + ToVar(12345UL)) | (y - ToVar(49UL));

    public bool Test13_cns(ulong x, ulong y) => ((x + 12345UL) | (y | 49UL)) == Test13_var(x, y);
    public ulong Test13_var(ulong x, ulong y) => (x + ToVar(12345UL)) | (y | ToVar(49UL));

    public bool Test14_cns(ulong x, ulong y) => ((x + 12345UL) | (y & 49UL)) == Test14_var(x, y);
    public ulong Test14_var(ulong x, ulong y) => (x + ToVar(12345UL)) | (y & ToVar(49UL));

    public bool Test15_cns(ulong x, ulong y) => ((x + 12345UL) | (y ^ 49UL)) == Test15_var(x, y);
    public ulong Test15_var(ulong x, ulong y) => (x + ToVar(12345UL)) | (y ^ ToVar(49UL));

    public bool Test16_cns(ulong x, ulong y) => ((x + 12345UL) & (y + 49UL)) == Test16_var(x, y);
    public ulong Test16_var(ulong x, ulong y) => (x + ToVar(12345UL)) & (y + ToVar(49UL));

    public bool Test17_cns(ulong x, ulong y) => ((x + 12345UL) & (y - 49UL)) == Test17_var(x, y);
    public ulong Test17_var(ulong x, ulong y) => (x + ToVar(12345UL)) & (y - ToVar(49UL));

    public bool Test18_cns(ulong x, ulong y) => ((x + 12345UL) & (y | 49UL)) == Test18_var(x, y);
    public ulong Test18_var(ulong x, ulong y) => (x + ToVar(12345UL)) & (y | ToVar(49UL));

    public bool Test19_cns(ulong x, ulong y) => ((x + 12345UL) & (y & 49UL)) == Test19_var(x, y);
    public ulong Test19_var(ulong x, ulong y) => (x + ToVar(12345UL)) & (y & ToVar(49UL));

    public bool Test20_cns(ulong x, ulong y) => ((x + 12345UL) & (y ^ 49UL)) == Test20_var(x, y);
    public ulong Test20_var(ulong x, ulong y) => (x + ToVar(12345UL)) & (y ^ ToVar(49UL));

    public bool Test21_cns(ulong x, ulong y) => ((x + 12345UL) ^ (y + 49UL)) == Test21_var(x, y);
    public ulong Test21_var(ulong x, ulong y) => (x + ToVar(12345UL)) ^ (y + ToVar(49UL));

    public bool Test22_cns(ulong x, ulong y) => ((x + 12345UL) ^ (y - 49UL)) == Test22_var(x, y);
    public ulong Test22_var(ulong x, ulong y) => (x + ToVar(12345UL)) ^ (y - ToVar(49UL));

    public bool Test23_cns(ulong x, ulong y) => ((x + 12345UL) ^ (y | 49UL)) == Test23_var(x, y);
    public ulong Test23_var(ulong x, ulong y) => (x + ToVar(12345UL)) ^ (y | ToVar(49UL));

    public bool Test24_cns(ulong x, ulong y) => ((x + 12345UL) ^ (y & 49UL)) == Test24_var(x, y);
    public ulong Test24_var(ulong x, ulong y) => (x + ToVar(12345UL)) ^ (y & ToVar(49UL));

    public bool Test25_cns(ulong x, ulong y) => ((x + 12345UL) ^ (y ^ 49UL)) == Test25_var(x, y);
    public ulong Test25_var(ulong x, ulong y) => (x + ToVar(12345UL)) ^ (y ^ ToVar(49UL));

    public bool Test26_cns(ulong x, ulong y) => ((x - 12345UL) + (y + 49UL)) == Test26_var(x, y);
    public ulong Test26_var(ulong x, ulong y) => (x - ToVar(12345UL)) + (y + ToVar(49UL));

    public bool Test27_cns(ulong x, ulong y) => ((x - 12345UL) + (y - 49UL)) == Test27_var(x, y);
    public ulong Test27_var(ulong x, ulong y) => (x - ToVar(12345UL)) + (y - ToVar(49UL));

    public bool Test28_cns(ulong x, ulong y) => ((x - 12345UL) + (y | 49UL)) == Test28_var(x, y);
    public ulong Test28_var(ulong x, ulong y) => (x - ToVar(12345UL)) + (y | ToVar(49UL));

    public bool Test29_cns(ulong x, ulong y) => ((x - 12345UL) + (y & 49UL)) == Test29_var(x, y);
    public ulong Test29_var(ulong x, ulong y) => (x - ToVar(12345UL)) + (y & ToVar(49UL));

    public bool Test30_cns(ulong x, ulong y) => ((x - 12345UL) + (y ^ 49UL)) == Test30_var(x, y);
    public ulong Test30_var(ulong x, ulong y) => (x - ToVar(12345UL)) + (y ^ ToVar(49UL));

    public bool Test31_cns(ulong x, ulong y) => ((x - 12345UL) - (y + 49UL)) == Test31_var(x, y);
    public ulong Test31_var(ulong x, ulong y) => (x - ToVar(12345UL)) - (y + ToVar(49UL));

    public bool Test32_cns(ulong x, ulong y) => ((x - 12345UL) - (y - 49UL)) == Test32_var(x, y);
    public ulong Test32_var(ulong x, ulong y) => (x - ToVar(12345UL)) - (y - ToVar(49UL));

    public bool Test33_cns(ulong x, ulong y) => ((x - 12345UL) - (y | 49UL)) == Test33_var(x, y);
    public ulong Test33_var(ulong x, ulong y) => (x - ToVar(12345UL)) - (y | ToVar(49UL));

    public bool Test34_cns(ulong x, ulong y) => ((x - 12345UL) - (y & 49UL)) == Test34_var(x, y);
    public ulong Test34_var(ulong x, ulong y) => (x - ToVar(12345UL)) - (y & ToVar(49UL));

    public bool Test35_cns(ulong x, ulong y) => ((x - 12345UL) - (y ^ 49UL)) == Test35_var(x, y);
    public ulong Test35_var(ulong x, ulong y) => (x - ToVar(12345UL)) - (y ^ ToVar(49UL));

    public bool Test36_cns(ulong x, ulong y) => ((x - 12345UL) | (y + 49UL)) == Test36_var(x, y);
    public ulong Test36_var(ulong x, ulong y) => (x - ToVar(12345UL)) | (y + ToVar(49UL));

    public bool Test37_cns(ulong x, ulong y) => ((x - 12345UL) | (y - 49UL)) == Test37_var(x, y);
    public ulong Test37_var(ulong x, ulong y) => (x - ToVar(12345UL)) | (y - ToVar(49UL));

    public bool Test38_cns(ulong x, ulong y) => ((x - 12345UL) | (y | 49UL)) == Test38_var(x, y);
    public ulong Test38_var(ulong x, ulong y) => (x - ToVar(12345UL)) | (y | ToVar(49UL));

    public bool Test39_cns(ulong x, ulong y) => ((x - 12345UL) | (y & 49UL)) == Test39_var(x, y);
    public ulong Test39_var(ulong x, ulong y) => (x - ToVar(12345UL)) | (y & ToVar(49UL));

    public bool Test40_cns(ulong x, ulong y) => ((x - 12345UL) | (y ^ 49UL)) == Test40_var(x, y);
    public ulong Test40_var(ulong x, ulong y) => (x - ToVar(12345UL)) | (y ^ ToVar(49UL));

    public bool Test41_cns(ulong x, ulong y) => ((x - 12345UL) & (y + 49UL)) == Test41_var(x, y);
    public ulong Test41_var(ulong x, ulong y) => (x - ToVar(12345UL)) & (y + ToVar(49UL));

    public bool Test42_cns(ulong x, ulong y) => ((x - 12345UL) & (y - 49UL)) == Test42_var(x, y);
    public ulong Test42_var(ulong x, ulong y) => (x - ToVar(12345UL)) & (y - ToVar(49UL));

    public bool Test43_cns(ulong x, ulong y) => ((x - 12345UL) & (y | 49UL)) == Test43_var(x, y);
    public ulong Test43_var(ulong x, ulong y) => (x - ToVar(12345UL)) & (y | ToVar(49UL));

    public bool Test44_cns(ulong x, ulong y) => ((x - 12345UL) & (y & 49UL)) == Test44_var(x, y);
    public ulong Test44_var(ulong x, ulong y) => (x - ToVar(12345UL)) & (y & ToVar(49UL));

    public bool Test45_cns(ulong x, ulong y) => ((x - 12345UL) & (y ^ 49UL)) == Test45_var(x, y);
    public ulong Test45_var(ulong x, ulong y) => (x - ToVar(12345UL)) & (y ^ ToVar(49UL));

    public bool Test46_cns(ulong x, ulong y) => ((x - 12345UL) ^ (y + 49UL)) == Test46_var(x, y);
    public ulong Test46_var(ulong x, ulong y) => (x - ToVar(12345UL)) ^ (y + ToVar(49UL));

    public bool Test47_cns(ulong x, ulong y) => ((x - 12345UL) ^ (y - 49UL)) == Test47_var(x, y);
    public ulong Test47_var(ulong x, ulong y) => (x - ToVar(12345UL)) ^ (y - ToVar(49UL));

    public bool Test48_cns(ulong x, ulong y) => ((x - 12345UL) ^ (y | 49UL)) == Test48_var(x, y);
    public ulong Test48_var(ulong x, ulong y) => (x - ToVar(12345UL)) ^ (y | ToVar(49UL));

    public bool Test49_cns(ulong x, ulong y) => ((x - 12345UL) ^ (y & 49UL)) == Test49_var(x, y);
    public ulong Test49_var(ulong x, ulong y) => (x - ToVar(12345UL)) ^ (y & ToVar(49UL));

    public bool Test50_cns(ulong x, ulong y) => ((x - 12345UL) ^ (y ^ 49UL)) == Test50_var(x, y);
    public ulong Test50_var(ulong x, ulong y) => (x - ToVar(12345UL)) ^ (y ^ ToVar(49UL));

    public bool Test51_cns(ulong x, ulong y) => ((x | 12345UL) + (y + 49UL)) == Test51_var(x, y);
    public ulong Test51_var(ulong x, ulong y) => (x | ToVar(12345UL)) + (y + ToVar(49UL));

    public bool Test52_cns(ulong x, ulong y) => ((x | 12345UL) + (y - 49UL)) == Test52_var(x, y);
    public ulong Test52_var(ulong x, ulong y) => (x | ToVar(12345UL)) + (y - ToVar(49UL));

    public bool Test53_cns(ulong x, ulong y) => ((x | 12345UL) + (y | 49UL)) == Test53_var(x, y);
    public ulong Test53_var(ulong x, ulong y) => (x | ToVar(12345UL)) + (y | ToVar(49UL));

    public bool Test54_cns(ulong x, ulong y) => ((x | 12345UL) + (y & 49UL)) == Test54_var(x, y);
    public ulong Test54_var(ulong x, ulong y) => (x | ToVar(12345UL)) + (y & ToVar(49UL));

    public bool Test55_cns(ulong x, ulong y) => ((x | 12345UL) + (y ^ 49UL)) == Test55_var(x, y);
    public ulong Test55_var(ulong x, ulong y) => (x | ToVar(12345UL)) + (y ^ ToVar(49UL));

    public bool Test56_cns(ulong x, ulong y) => ((x | 12345UL) - (y + 49UL)) == Test56_var(x, y);
    public ulong Test56_var(ulong x, ulong y) => (x | ToVar(12345UL)) - (y + ToVar(49UL));

    public bool Test57_cns(ulong x, ulong y) => ((x | 12345UL) - (y - 49UL)) == Test57_var(x, y);
    public ulong Test57_var(ulong x, ulong y) => (x | ToVar(12345UL)) - (y - ToVar(49UL));

    public bool Test58_cns(ulong x, ulong y) => ((x | 12345UL) - (y | 49UL)) == Test58_var(x, y);
    public ulong Test58_var(ulong x, ulong y) => (x | ToVar(12345UL)) - (y | ToVar(49UL));

    public bool Test59_cns(ulong x, ulong y) => ((x | 12345UL) - (y & 49UL)) == Test59_var(x, y);
    public ulong Test59_var(ulong x, ulong y) => (x | ToVar(12345UL)) - (y & ToVar(49UL));

    public bool Test60_cns(ulong x, ulong y) => ((x | 12345UL) - (y ^ 49UL)) == Test60_var(x, y);
    public ulong Test60_var(ulong x, ulong y) => (x | ToVar(12345UL)) - (y ^ ToVar(49UL));

    public bool Test61_cns(ulong x, ulong y) => ((x | 12345UL) | (y + 49UL)) == Test61_var(x, y);
    public ulong Test61_var(ulong x, ulong y) => (x | ToVar(12345UL)) | (y + ToVar(49UL));

    public bool Test62_cns(ulong x, ulong y) => ((x | 12345UL) | (y - 49UL)) == Test62_var(x, y);
    public ulong Test62_var(ulong x, ulong y) => (x | ToVar(12345UL)) | (y - ToVar(49UL));

    public bool Test63_cns(ulong x, ulong y) => ((x | 12345UL) | (y | 49UL)) == Test63_var(x, y);
    public ulong Test63_var(ulong x, ulong y) => (x | ToVar(12345UL)) | (y | ToVar(49UL));

    public bool Test64_cns(ulong x, ulong y) => ((x | 12345UL) | (y & 49UL)) == Test64_var(x, y);
    public ulong Test64_var(ulong x, ulong y) => (x | ToVar(12345UL)) | (y & ToVar(49UL));

    public bool Test65_cns(ulong x, ulong y) => ((x | 12345UL) | (y ^ 49UL)) == Test65_var(x, y);
    public ulong Test65_var(ulong x, ulong y) => (x | ToVar(12345UL)) | (y ^ ToVar(49UL));

    public bool Test66_cns(ulong x, ulong y) => ((x | 12345UL) & (y + 49UL)) == Test66_var(x, y);
    public ulong Test66_var(ulong x, ulong y) => (x | ToVar(12345UL)) & (y + ToVar(49UL));

    public bool Test67_cns(ulong x, ulong y) => ((x | 12345UL) & (y - 49UL)) == Test67_var(x, y);
    public ulong Test67_var(ulong x, ulong y) => (x | ToVar(12345UL)) & (y - ToVar(49UL));

    public bool Test68_cns(ulong x, ulong y) => ((x | 12345UL) & (y | 49UL)) == Test68_var(x, y);
    public ulong Test68_var(ulong x, ulong y) => (x | ToVar(12345UL)) & (y | ToVar(49UL));

    public bool Test69_cns(ulong x, ulong y) => ((x | 12345UL) & (y & 49UL)) == Test69_var(x, y);
    public ulong Test69_var(ulong x, ulong y) => (x | ToVar(12345UL)) & (y & ToVar(49UL));

    public bool Test70_cns(ulong x, ulong y) => ((x | 12345UL) & (y ^ 49UL)) == Test70_var(x, y);
    public ulong Test70_var(ulong x, ulong y) => (x | ToVar(12345UL)) & (y ^ ToVar(49UL));

    public bool Test71_cns(ulong x, ulong y) => ((x | 12345UL) ^ (y + 49UL)) == Test71_var(x, y);
    public ulong Test71_var(ulong x, ulong y) => (x | ToVar(12345UL)) ^ (y + ToVar(49UL));

    public bool Test72_cns(ulong x, ulong y) => ((x | 12345UL) ^ (y - 49UL)) == Test72_var(x, y);
    public ulong Test72_var(ulong x, ulong y) => (x | ToVar(12345UL)) ^ (y - ToVar(49UL));

    public bool Test73_cns(ulong x, ulong y) => ((x | 12345UL) ^ (y | 49UL)) == Test73_var(x, y);
    public ulong Test73_var(ulong x, ulong y) => (x | ToVar(12345UL)) ^ (y | ToVar(49UL));

    public bool Test74_cns(ulong x, ulong y) => ((x | 12345UL) ^ (y & 49UL)) == Test74_var(x, y);
    public ulong Test74_var(ulong x, ulong y) => (x | ToVar(12345UL)) ^ (y & ToVar(49UL));

    public bool Test75_cns(ulong x, ulong y) => ((x | 12345UL) ^ (y ^ 49UL)) == Test75_var(x, y);
    public ulong Test75_var(ulong x, ulong y) => (x | ToVar(12345UL)) ^ (y ^ ToVar(49UL));

    public bool Test76_cns(ulong x, ulong y) => ((x & 12345UL) + (y + 49UL)) == Test76_var(x, y);
    public ulong Test76_var(ulong x, ulong y) => (x & ToVar(12345UL)) + (y + ToVar(49UL));

    public bool Test77_cns(ulong x, ulong y) => ((x & 12345UL) + (y - 49UL)) == Test77_var(x, y);
    public ulong Test77_var(ulong x, ulong y) => (x & ToVar(12345UL)) + (y - ToVar(49UL));

    public bool Test78_cns(ulong x, ulong y) => ((x & 12345UL) + (y | 49UL)) == Test78_var(x, y);
    public ulong Test78_var(ulong x, ulong y) => (x & ToVar(12345UL)) + (y | ToVar(49UL));

    public bool Test79_cns(ulong x, ulong y) => ((x & 12345UL) + (y & 49UL)) == Test79_var(x, y);
    public ulong Test79_var(ulong x, ulong y) => (x & ToVar(12345UL)) + (y & ToVar(49UL));

    public bool Test80_cns(ulong x, ulong y) => ((x & 12345UL) + (y ^ 49UL)) == Test80_var(x, y);
    public ulong Test80_var(ulong x, ulong y) => (x & ToVar(12345UL)) + (y ^ ToVar(49UL));

    public bool Test81_cns(ulong x, ulong y) => ((x & 12345UL) - (y + 49UL)) == Test81_var(x, y);
    public ulong Test81_var(ulong x, ulong y) => (x & ToVar(12345UL)) - (y + ToVar(49UL));

    public bool Test82_cns(ulong x, ulong y) => ((x & 12345UL) - (y - 49UL)) == Test82_var(x, y);
    public ulong Test82_var(ulong x, ulong y) => (x & ToVar(12345UL)) - (y - ToVar(49UL));

    public bool Test83_cns(ulong x, ulong y) => ((x & 12345UL) - (y | 49UL)) == Test83_var(x, y);
    public ulong Test83_var(ulong x, ulong y) => (x & ToVar(12345UL)) - (y | ToVar(49UL));

    public bool Test84_cns(ulong x, ulong y) => ((x & 12345UL) - (y & 49UL)) == Test84_var(x, y);
    public ulong Test84_var(ulong x, ulong y) => (x & ToVar(12345UL)) - (y & ToVar(49UL));

    public bool Test85_cns(ulong x, ulong y) => ((x & 12345UL) - (y ^ 49UL)) == Test85_var(x, y);
    public ulong Test85_var(ulong x, ulong y) => (x & ToVar(12345UL)) - (y ^ ToVar(49UL));

    public bool Test86_cns(ulong x, ulong y) => ((x & 12345UL) | (y + 49UL)) == Test86_var(x, y);
    public ulong Test86_var(ulong x, ulong y) => (x & ToVar(12345UL)) | (y + ToVar(49UL));

    public bool Test87_cns(ulong x, ulong y) => ((x & 12345UL) | (y - 49UL)) == Test87_var(x, y);
    public ulong Test87_var(ulong x, ulong y) => (x & ToVar(12345UL)) | (y - ToVar(49UL));

    public bool Test88_cns(ulong x, ulong y) => ((x & 12345UL) | (y | 49UL)) == Test88_var(x, y);
    public ulong Test88_var(ulong x, ulong y) => (x & ToVar(12345UL)) | (y | ToVar(49UL));

    public bool Test89_cns(ulong x, ulong y) => ((x & 12345UL) | (y & 49UL)) == Test89_var(x, y);
    public ulong Test89_var(ulong x, ulong y) => (x & ToVar(12345UL)) | (y & ToVar(49UL));

    public bool Test90_cns(ulong x, ulong y) => ((x & 12345UL) | (y ^ 49UL)) == Test90_var(x, y);
    public ulong Test90_var(ulong x, ulong y) => (x & ToVar(12345UL)) | (y ^ ToVar(49UL));

    public bool Test91_cns(ulong x, ulong y) => ((x & 12345UL) & (y + 49UL)) == Test91_var(x, y);
    public ulong Test91_var(ulong x, ulong y) => (x & ToVar(12345UL)) & (y + ToVar(49UL));

    public bool Test92_cns(ulong x, ulong y) => ((x & 12345UL) & (y - 49UL)) == Test92_var(x, y);
    public ulong Test92_var(ulong x, ulong y) => (x & ToVar(12345UL)) & (y - ToVar(49UL));

    public bool Test93_cns(ulong x, ulong y) => ((x & 12345UL) & (y | 49UL)) == Test93_var(x, y);
    public ulong Test93_var(ulong x, ulong y) => (x & ToVar(12345UL)) & (y | ToVar(49UL));

    public bool Test94_cns(ulong x, ulong y) => ((x & 12345UL) & (y & 49UL)) == Test94_var(x, y);
    public ulong Test94_var(ulong x, ulong y) => (x & ToVar(12345UL)) & (y & ToVar(49UL));

    public bool Test95_cns(ulong x, ulong y) => ((x & 12345UL) & (y ^ 49UL)) == Test95_var(x, y);
    public ulong Test95_var(ulong x, ulong y) => (x & ToVar(12345UL)) & (y ^ ToVar(49UL));

    public bool Test96_cns(ulong x, ulong y) => ((x & 12345UL) ^ (y + 49UL)) == Test96_var(x, y);
    public ulong Test96_var(ulong x, ulong y) => (x & ToVar(12345UL)) ^ (y + ToVar(49UL));

    public bool Test97_cns(ulong x, ulong y) => ((x & 12345UL) ^ (y - 49UL)) == Test97_var(x, y);
    public ulong Test97_var(ulong x, ulong y) => (x & ToVar(12345UL)) ^ (y - ToVar(49UL));

    public bool Test98_cns(ulong x, ulong y) => ((x & 12345UL) ^ (y | 49UL)) == Test98_var(x, y);
    public ulong Test98_var(ulong x, ulong y) => (x & ToVar(12345UL)) ^ (y | ToVar(49UL));

    public bool Test99_cns(ulong x, ulong y) => ((x & 12345UL) ^ (y & 49UL)) == Test99_var(x, y);
    public ulong Test99_var(ulong x, ulong y) => (x & ToVar(12345UL)) ^ (y & ToVar(49UL));

    public bool Test100_cns(ulong x, ulong y) => ((x & 12345UL) ^ (y ^ 49UL)) == Test100_var(x, y);
    public ulong Test100_var(ulong x, ulong y) => (x & ToVar(12345UL)) ^ (y ^ ToVar(49UL));

    public bool Test101_cns(ulong x, ulong y) => ((x ^ 12345UL) + (y + 49UL)) == Test101_var(x, y);
    public ulong Test101_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) + (y + ToVar(49UL));

    public bool Test102_cns(ulong x, ulong y) => ((x ^ 12345UL) + (y - 49UL)) == Test102_var(x, y);
    public ulong Test102_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) + (y - ToVar(49UL));

    public bool Test103_cns(ulong x, ulong y) => ((x ^ 12345UL) + (y | 49UL)) == Test103_var(x, y);
    public ulong Test103_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) + (y | ToVar(49UL));

    public bool Test104_cns(ulong x, ulong y) => ((x ^ 12345UL) + (y & 49UL)) == Test104_var(x, y);
    public ulong Test104_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) + (y & ToVar(49UL));

    public bool Test105_cns(ulong x, ulong y) => ((x ^ 12345UL) + (y ^ 49UL)) == Test105_var(x, y);
    public ulong Test105_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) + (y ^ ToVar(49UL));

    public bool Test106_cns(ulong x, ulong y) => ((x ^ 12345UL) - (y + 49UL)) == Test106_var(x, y);
    public ulong Test106_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) - (y + ToVar(49UL));

    public bool Test107_cns(ulong x, ulong y) => ((x ^ 12345UL) - (y - 49UL)) == Test107_var(x, y);
    public ulong Test107_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) - (y - ToVar(49UL));

    public bool Test108_cns(ulong x, ulong y) => ((x ^ 12345UL) - (y | 49UL)) == Test108_var(x, y);
    public ulong Test108_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) - (y | ToVar(49UL));

    public bool Test109_cns(ulong x, ulong y) => ((x ^ 12345UL) - (y & 49UL)) == Test109_var(x, y);
    public ulong Test109_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) - (y & ToVar(49UL));

    public bool Test110_cns(ulong x, ulong y) => ((x ^ 12345UL) - (y ^ 49UL)) == Test110_var(x, y);
    public ulong Test110_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) - (y ^ ToVar(49UL));

    public bool Test111_cns(ulong x, ulong y) => ((x ^ 12345UL) | (y + 49UL)) == Test111_var(x, y);
    public ulong Test111_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) | (y + ToVar(49UL));

    public bool Test112_cns(ulong x, ulong y) => ((x ^ 12345UL) | (y - 49UL)) == Test112_var(x, y);
    public ulong Test112_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) | (y - ToVar(49UL));

    public bool Test113_cns(ulong x, ulong y) => ((x ^ 12345UL) | (y | 49UL)) == Test113_var(x, y);
    public ulong Test113_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) | (y | ToVar(49UL));

    public bool Test114_cns(ulong x, ulong y) => ((x ^ 12345UL) | (y & 49UL)) == Test114_var(x, y);
    public ulong Test114_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) | (y & ToVar(49UL));

    public bool Test115_cns(ulong x, ulong y) => ((x ^ 12345UL) | (y ^ 49UL)) == Test115_var(x, y);
    public ulong Test115_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) | (y ^ ToVar(49UL));

    public bool Test116_cns(ulong x, ulong y) => ((x ^ 12345UL) & (y + 49UL)) == Test116_var(x, y);
    public ulong Test116_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) & (y + ToVar(49UL));

    public bool Test117_cns(ulong x, ulong y) => ((x ^ 12345UL) & (y - 49UL)) == Test117_var(x, y);
    public ulong Test117_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) & (y - ToVar(49UL));

    public bool Test118_cns(ulong x, ulong y) => ((x ^ 12345UL) & (y | 49UL)) == Test118_var(x, y);
    public ulong Test118_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) & (y | ToVar(49UL));

    public bool Test119_cns(ulong x, ulong y) => ((x ^ 12345UL) & (y & 49UL)) == Test119_var(x, y);
    public ulong Test119_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) & (y & ToVar(49UL));

    public bool Test120_cns(ulong x, ulong y) => ((x ^ 12345UL) & (y ^ 49UL)) == Test120_var(x, y);
    public ulong Test120_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) & (y ^ ToVar(49UL));

    public bool Test121_cns(ulong x, ulong y) => ((x ^ 12345UL) ^ (y + 49UL)) == Test121_var(x, y);
    public ulong Test121_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) ^ (y + ToVar(49UL));

    public bool Test122_cns(ulong x, ulong y) => ((x ^ 12345UL) ^ (y - 49UL)) == Test122_var(x, y);
    public ulong Test122_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) ^ (y - ToVar(49UL));

    public bool Test123_cns(ulong x, ulong y) => ((x ^ 12345UL) ^ (y | 49UL)) == Test123_var(x, y);
    public ulong Test123_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) ^ (y | ToVar(49UL));

    public bool Test124_cns(ulong x, ulong y) => ((x ^ 12345UL) ^ (y & 49UL)) == Test124_var(x, y);
    public ulong Test124_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) ^ (y & ToVar(49UL));

    public bool Test125_cns(ulong x, ulong y) => ((x ^ 12345UL) ^ (y ^ 49UL)) == Test125_var(x, y);
    public ulong Test125_var(ulong x, ulong y) => (x ^ ToVar(12345UL)) ^ (y ^ ToVar(49UL));

    private static T ToVar<T>(T v) => v;
}

// Make sure JIT optimizations don't hide possible overflows
public class OverflowTests
{
    public void Test()
    {
        ThrowsOverflowException(() => Test0(0));
        ThrowsOverflowException(() => Test1(0, 0));
        ThrowsOverflowException(() => Test2(0));
        ThrowsOverflowException(() => Test3(0, 0));
        ThrowsOverflowException(() => Test4(0));
        ThrowsOverflowException(() => Test5(0));
        ThrowsOverflowException(() => Test6(0, 0));
        ThrowsOverflowException(() => Test7(0, 0));
    }

    public int Test0(int x) => checked((x + int.MaxValue + 1));
    public int Test1(int x, int y) => checked((x + int.MaxValue / 2 + 1) + (y + (int.MaxValue / 2 + 1)));
    public byte Test2(byte x) => checked((byte)(x + byte.MaxValue + 1));
    public byte Test3(byte x, byte y) => checked((byte)((x + byte.MaxValue / 2 + 1) + (y + (byte.MaxValue / 2 + 1))));
    public ulong Test4(ulong x) => checked((x + ulong.MaxValue + 1));
    public ulong Test5(ulong x) => checked((1 + x + ulong.MaxValue));
    public ulong Test6(ulong x, ulong y) => checked((x + ulong.MaxValue / 2 + 1) + (y + (ulong.MaxValue / 2 + 1)));
    public ulong Test7(ulong x, ulong y) => checked((1 + x + ulong.MaxValue / 2) + (1 + y + (ulong.MaxValue / 2)));

    static void ThrowsOverflowException<T>(Func<T> action)
    {
        try
        {
            action();
        }
        catch (OverflowException)
        {
            return;
        }
        throw new Exception("OverflowException was expected");
    }
}