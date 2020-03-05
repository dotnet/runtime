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

        long[] testInput = Enumerable.Range(-256, 256)
            .Select(i => (long)i)
            .Concat(new [] { (long)int.MaxValue - 1, int.MaxValue, long.MaxValue - 1, long.MaxValue })
            .ToArray();

        for (int i = 0; i < allTestsWithOpt.Length; i++)
        {
            for (int j = 0; j < testInput.Length; j++)
            {
                object[] invokeArgs;
                Type argType = allTestsWithOpt[i].GetParameters()[0].ParameterType;
                if (argType == typeof(int))
                    invokeArgs = new object[] {unchecked((int) testInput[j])};
                else
                    invokeArgs = new object[] {testInput[j]};

                object actualBoxed = allTestsWithOpt[i].Invoke(testsWithOpt, invokeArgs);
                object expectedBoxed = allTestsWithoutOpt[i].Invoke(testsWithoutOpt, invokeArgs);

                if (!actualBoxed.Equals(expectedBoxed))
                {
                    Console.WriteLine($"{allTestsWithOpt[i].Name}: {actualBoxed} != {expectedBoxed}");
                    returnCode++;
                }
            }
        }
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
    public int Test12(int a) => a == 42 ? Cns(int.MaxValue) : Cns(int.MinValue);
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
    public int Test25(int a) => a != 42 ? Cns(int.MaxValue) : Cns(int.MinValue);
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
    public int Test38(int a) => a > 42 ? Cns(int.MaxValue) : Cns(int.MinValue);
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
    public int Test51(int a) => a == 0 ? Cns(int.MaxValue) : Cns(int.MinValue);
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
    public int Test64(int a) => a != 0 ? Cns(int.MaxValue) : Cns(int.MinValue);
    public int Test65(int a) => a != 0 ? Cns(int.MaxValue - 1) : Cns(int.MaxValue);

    public long Test66(long a) => a != 0 ? Cns(0) : Cns(1);
    public long Test67(long a) => a != 0 ? Cns(0) : Cns(0);
    public long Test68(long a) => a != 0 ? Cns(1) : Cns(0);
    public long Test69(long a) => a != 0 ? Cns(0) : Cns(-1);
    public long Test70(long a) => a != 0 ? Cns(-1) : Cns(0);
    public long Test71(long a) => a != 0 ? Cns(1) : Cns(2);
    public long Test72(long a) => a != 0 ? Cns(2) : Cns(2);
    public long Test73(long a) => a != 0 ? Cns(3) : Cns(2);
    public long Test74(long a) => a != 0 ? Cns(4) : Cns(2);
    public long Test75(long a) => a != 0 ? Cns(4) : Cns(6);
    public long Test76(long a) => a != 0 ? Cns(long.MaxValue) : Cns(long.MaxValue - 1);
    public long Test77(long a) => a != 0 ? Cns(long.MaxValue) : Cns(long.MinValue);
    public long Test78(long a) => a != 0 ? Cns(long.MaxValue - 1) : Cns(long.MaxValue);

    public bool Test79(long a)
    {
        if (a == 0)
            return Cns(true);
        return Cns(false);
    }
    public bool Test80(long a)
    {
        if (a == 42)
            return Cns(false);
        return Cns(true);
    }
    public int Test81(long a)
    {
        if (a != 42)
            return Cns(1);
        return Cns(0);
    }
    public int Test82(long a)
    {
        if (a > 42)
            return Cns(0);
        return Cns(1);
    }
}

public class TestsWithoutOptimization
{
    [MethodImpl(MethodImplOptions.NoInlining)]
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
    public int Test12(int a) => a == 42 ? Var(int.MaxValue) : Var(int.MinValue);
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
    public int Test25(int a) => a != 42 ? Var(int.MaxValue) : Var(int.MinValue);
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
    public int Test38(int a) => a > 42 ? Var(int.MaxValue) : Var(int.MinValue);
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
    public int Test51(int a) => a == 0 ? Var(int.MaxValue) : Var(int.MinValue);
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
    public int Test64(int a) => a != 0 ? Var(int.MaxValue) : Var(int.MinValue);
    public int Test65(int a) => a != 0 ? Var(int.MaxValue - 1) : Var(int.MaxValue);

    public long Test66(long a) => a != 0 ? Var(0) : Var(1);
    public long Test67(long a) => a != 0 ? Var(0) : Var(0);
    public long Test68(long a) => a != 0 ? Var(1) : Var(0);
    public long Test69(long a) => a != 0 ? Var(0) : Var(-1);
    public long Test70(long a) => a != 0 ? Var(-1) : Var(0);
    public long Test71(long a) => a != 0 ? Var(1) : Var(2);
    public long Test72(long a) => a != 0 ? Var(2) : Var(2);
    public long Test73(long a) => a != 0 ? Var(3) : Var(2);
    public long Test74(long a) => a != 0 ? Var(4) : Var(2);
    public long Test75(long a) => a != 0 ? Var(4) : Var(6);
    public long Test76(long a) => a != 0 ? Var(long.MaxValue) : Var(long.MaxValue - 1);
    public long Test77(long a) => a != 0 ? Var(long.MaxValue) : Var(long.MinValue);
    public long Test78(long a) => a != 0 ? Var(long.MaxValue - 1) : Var(long.MaxValue);

    public bool Test79(long a)
    {
        if (a == 0)
            return Var(true);
        return Var(false);
    }
    public bool Test80(long a)
    {
        if (a == 42)
            return Var(false);
        return Var(true);
    }
    public int Test81(long a)
    {
        if (a != 42)
            return Var(1);
        return Var(0);
    }
    public int Test82(long a)
    {
        if (a > 42)
            return Var(0);
        return Var(1);
    }
}
