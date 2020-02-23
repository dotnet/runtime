using System;
using System.Linq;
using System.Runtime.CompilerServices;

public class Program
{
    public static int returnCode = 100;

    public static int Main(string[] args)
    {
        var testsWithOpt = new Tests();
        var testsWithoutOpt = new TestsWithoutOptimization();

        var allTestsWithOpt = testsWithOpt.GetType().GetMethods()
            .Where(m => m.Name.StartsWith("Test"))
            .OrderBy(m => m.Name)
            .ToArray();

        var allTestsWithoutOpt = testsWithoutOpt.GetType().GetMethods()
            .Where(m => m.Name.StartsWith("Test"))
            .OrderBy(m => m.Name)
            .ToArray();

        int[] testInput = Enumerable.Range(-100, 100)
            .Concat(new [] {int.MaxValue - 1, int.MaxValue})
            .ToArray();

        for (int i = 0; i < allTestsWithOpt.Length; i++)
        {
            for (int j = 0; j < testInput.Length; j++)
            {
                var invokeArgs = new object[] { testInput[j] };
                int actual = (int)allTestsWithOpt[i].Invoke(testsWithOpt, invokeArgs);
                int expected = (int)allTestsWithoutOpt[i].Invoke(testsWithoutOpt, invokeArgs);

                if (actual != expected)
                {
                    Console.WriteLine($"{allTestsWithOpt[i].Name}: {actual} != {expected}");
                    returnCode++;
                }
            }
        }

        Console.WriteLine(returnCode);
        Console.ReadKey();
        return returnCode;
    }
}


public class Tests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cns<T>(T t) => t; // don't want Roslyn to optimize anything

    public int Test1(int a) => a == 42 ? Cns(0) : Cns(1);
    public int Test2(int a) => a == 42 ? Cns(0) : Cns(0);
    public int Test3(int a) => a == 42 ? Cns(1) : Cns(0);
    public int Test4(int a) => a == 42 ? Cns(0) : Cns(-1);
    public int Test5(int a) => a == 42 ? Cns(-1) : Cns(0);
    public int Test6(int a) => a == 42 ? Cns(1) : Cns(2);
    public int Test7(int a) => a == 42 ? Cns(2) : Cns(2);
    public int Test8(int a) => a == 42 ? Cns(3) : Cns(2);
    public int Test9(int a) => a == 42 ? Cns(4) : Cns(2);
    public int Test10(int a) => a == 42 ? Cns(4) : Cns(6);
    public int Test11(int a) => a == 42 ? Cns(int.MaxValue) : Cns(int.MaxValue - 1);
    public int Test12(int a) => a == 42 ? Cns(int.MaxValue) : Cns(int.MaxValue);
    public int Test13(int a) => a == 42 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);

    public int Test14(int a) => a != 42 ? Cns(0) : Cns(1);
    public int Test15(int a) => a != 42 ? Cns(0) : Cns(0);
    public int Test16(int a) => a != 42 ? Cns(1) : Cns(0);
    public int Test17(int a) => a != 42 ? Cns(0) : Cns(-1);
    public int Test18(int a) => a != 42 ? Cns(-1) : Cns(0);
    public int Test19(int a) => a != 42 ? Cns(1) : Cns(2);
    public int Test20(int a) => a != 42 ? Cns(2) : Cns(2);
    public int Test21(int a) => a != 42 ? Cns(3) : Cns(2);
    public int Test22(int a) => a != 42 ? Cns(4) : Cns(2);
    public int Test23(int a) => a != 42 ? Cns(4) : Cns(6);
    public int Test24(int a) => a != 42 ? Cns(int.MaxValue) : Cns(int.MaxValue - 1);
    public int Test25(int a) => a != 42 ? Cns(int.MaxValue) : Cns(int.MaxValue);
    public int Test26(int a) => a != 42 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);

    public int Test27(int a) => a > 42 ? Cns(0) : Cns(1);
    public int Test28(int a) => a > 42 ? Cns(0) : Cns(0);
    public int Test29(int a) => a > 42 ? Cns(1) : Cns(0);
    public int Test30(int a) => a > 42 ? Cns(0) : Cns(-1);
    public int Test31(int a) => a > 42 ? Cns(-1) : Cns(0);
    public int Test32(int a) => a > 42 ? Cns(1) : Cns(2);
    public int Test33(int a) => a > 42 ? Cns(2) : Cns(2);
    public int Test34(int a) => a > 42 ? Cns(3) : Cns(2);
    public int Test35(int a) => a > 42 ? Cns(4) : Cns(2);
    public int Test36(int a) => a > 42 ? Cns(4) : Cns(6);
    public int Test37(int a) => a > 42 ? Cns(int.MaxValue) : Cns(int.MaxValue - 1);
    public int Test38(int a) => a > 42 ? Cns(int.MaxValue) : Cns(int.MaxValue);
    public int Test39(int a) => a > 42 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);

    // GT_TEST_/test instruction
    public int Test40(int a) => a == 0 ? Cns(0) : Cns(1);
    public int Test41(int a) => a == 0 ? Cns(0) : Cns(0);
    public int Test42(int a) => a == 0 ? Cns(1) : Cns(0);
    public int Test43(int a) => a == 0 ? Cns(0) : Cns(-1);
    public int Test44(int a) => a == 0 ? Cns(-1) : Cns(0);
    public int Test45(int a) => a == 0 ? Cns(1) : Cns(2);
    public int Test46(int a) => a == 0 ? Cns(2) : Cns(2);
    public int Test47(int a) => a == 0 ? Cns(3) : Cns(2);
    public int Test48(int a) => a == 0 ? Cns(4) : Cns(2);
    public int Test49(int a) => a == 0 ? Cns(4) : Cns(6);
    public int Test50(int a) => a == 0 ? Cns(int.MaxValue) : Cns(int.MaxValue - 1);
    public int Test51(int a) => a == 0 ? Cns(int.MaxValue) : Cns(int.MaxValue);
    public int Test52(int a) => a == 0 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);

    public int Test53(int a) => a != 0 ? Cns(0) : Cns(1);
    public int Test54(int a) => a != 0 ? Cns(0) : Cns(0);
    public int Test55(int a) => a != 0 ? Cns(1) : Cns(0);
    public int Test56(int a) => a != 0 ? Cns(0) : Cns(-1);
    public int Test57(int a) => a != 0 ? Cns(-1) : Cns(0);
    public int Test58(int a) => a != 0 ? Cns(1) : Cns(2);
    public int Test59(int a) => a != 0 ? Cns(2) : Cns(2);
    public int Test60(int a) => a != 0 ? Cns(3) : Cns(2);
    public int Test61(int a) => a != 0 ? Cns(4) : Cns(2);
    public int Test62(int a) => a != 0 ? Cns(4) : Cns(6);
    public int Test63(int a) => a != 0 ? Cns(int.MaxValue) : Cns(int.MaxValue - 1);
    public int Test64(int a) => a != 0 ? Cns(int.MaxValue) : Cns(int.MaxValue);
    public int Test65(int a) => a != 0 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);
}

public class TestsWithoutOptimization
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Var<T>(T t) => t;

    public int Test1(int a) => a == 42 ? Var(0) : Var(1);
    public int Test2(int a) => a == 42 ? Var(0) : Var(0);
    public int Test3(int a) => a == 42 ? Var(1) : Var(0);
    public int Test4(int a) => a == 42 ? Var(0) : Var(-1);
    public int Test5(int a) => a == 42 ? Var(-1) : Var(0);
    public int Test6(int a) => a == 42 ? Var(1) : Var(2);
    public int Test7(int a) => a == 42 ? Var(2) : Var(2);
    public int Test8(int a) => a == 42 ? Var(3) : Var(2);
    public int Test9(int a) => a == 42 ? Var(4) : Var(2);
    public int Test10(int a) => a == 42 ? Var(4) : Var(6);
    public int Test11(int a) => a == 42 ? Var(int.MaxValue) : Var(int.MaxValue - 1);
    public int Test12(int a) => a == 42 ? Var(int.MaxValue) : Var(int.MaxValue);
    public int Test13(int a) => a == 42 ? Var(int.MaxValue - 1) : Var(int.MaxValue);

    public int Test14(int a) => a != 42 ? Var(0) : Var(1);
    public int Test15(int a) => a != 42 ? Var(0) : Var(0);
    public int Test16(int a) => a != 42 ? Var(1) : Var(0);
    public int Test17(int a) => a != 42 ? Var(0) : Var(-1);
    public int Test18(int a) => a != 42 ? Var(-1) : Var(0);
    public int Test19(int a) => a != 42 ? Var(1) : Var(2);
    public int Test20(int a) => a != 42 ? Var(2) : Var(2);
    public int Test21(int a) => a != 42 ? Var(3) : Var(2);
    public int Test22(int a) => a != 42 ? Var(4) : Var(2);
    public int Test23(int a) => a != 42 ? Var(4) : Var(6);
    public int Test24(int a) => a != 42 ? Var(int.MaxValue) : Var(int.MaxValue - 1);
    public int Test25(int a) => a != 42 ? Var(int.MaxValue) : Var(int.MaxValue);
    public int Test26(int a) => a != 42 ? Var(int.MaxValue - 1) : Var(int.MaxValue);

    public int Test27(int a) => a > 42 ? Var(0) : Var(1);
    public int Test28(int a) => a > 42 ? Var(0) : Var(0);
    public int Test29(int a) => a > 42 ? Var(1) : Var(0);
    public int Test30(int a) => a > 42 ? Var(0) : Var(-1);
    public int Test31(int a) => a > 42 ? Var(-1) : Var(0);
    public int Test32(int a) => a > 42 ? Var(1) : Var(2);
    public int Test33(int a) => a > 42 ? Var(2) : Var(2);
    public int Test34(int a) => a > 42 ? Var(3) : Var(2);
    public int Test35(int a) => a > 42 ? Var(4) : Var(2);
    public int Test36(int a) => a > 42 ? Var(4) : Var(6);
    public int Test37(int a) => a > 42 ? Var(int.MaxValue) : Var(int.MaxValue - 1);
    public int Test38(int a) => a > 42 ? Var(int.MaxValue) : Var(int.MaxValue);
    public int Test39(int a) => a > 42 ? Var(int.MaxValue - 1) : Var(int.MaxValue);

    // GT_TEST_/test instruction
    public int Test40(int a) => a == 0 ? Var(0) : Var(1);
    public int Test41(int a) => a == 0 ? Var(0) : Var(0);
    public int Test42(int a) => a == 0 ? Var(1) : Var(0);
    public int Test43(int a) => a == 0 ? Var(0) : Var(-1);
    public int Test44(int a) => a == 0 ? Var(-1) : Var(0);
    public int Test45(int a) => a == 0 ? Var(1) : Var(2);
    public int Test46(int a) => a == 0 ? Var(2) : Var(2);
    public int Test47(int a) => a == 0 ? Var(3) : Var(2);
    public int Test48(int a) => a == 0 ? Var(4) : Var(2);
    public int Test49(int a) => a == 0 ? Var(4) : Var(6);
    public int Test50(int a) => a == 0 ? Var(int.MaxValue) : Var(int.MaxValue - 1);
    public int Test51(int a) => a == 0 ? Var(int.MaxValue) : Var(int.MaxValue);
    public int Test52(int a) => a == 0 ? Var(int.MaxValue - 1) : Var(int.MaxValue);

    public int Test53(int a) => a != 0 ? Var(0) : Var(1);
    public int Test54(int a) => a != 0 ? Var(0) : Var(0);
    public int Test55(int a) => a != 0 ? Var(1) : Var(0);
    public int Test56(int a) => a != 0 ? Var(0) : Var(-1);
    public int Test57(int a) => a != 0 ? Var(-1) : Var(0);
    public int Test58(int a) => a != 0 ? Var(1) : Var(2);
    public int Test59(int a) => a != 0 ? Var(2) : Var(2);
    public int Test60(int a) => a != 0 ? Var(3) : Var(2);
    public int Test61(int a) => a != 0 ? Var(4) : Var(2);
    public int Test62(int a) => a != 0 ? Var(4) : Var(6);
    public int Test63(int a) => a != 0 ? Var(int.MaxValue) : Var(int.MaxValue - 1);
    public int Test64(int a) => a != 0 ? Var(int.MaxValue) : Var(int.MaxValue);
    public int Test65(int a) => a != 0 ? Var(int.MaxValue - 1) : Var(int.MaxValue);
}