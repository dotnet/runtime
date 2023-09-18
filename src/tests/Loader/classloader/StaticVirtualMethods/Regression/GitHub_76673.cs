// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
// Regression test for a bug found in Roslyn testing; as of authoting
// this test CoreCLR static virtual method support apparently has a bug
// causing "Testing Test1" to return "I1.M1" from a call to M1 instead
// of the expected "Test2.M1".

interface I1
{
    static virtual string M1() { return "I1.M1"; }
    static abstract int M2();
}

public class Test1 : Test2, I1
{
    static public int M1() { return 0; }
    static public ref int M2() { throw null; }

    [Fact]
    public static int TestEntryPoint()
    {
        System.Console.WriteLine("Testing Test2");
        bool ok2 = Test<Test2>();
        System.Console.WriteLine("Testing Test1");
        bool ok1 = Test<Test1>();
        return ok2 && ok1 ? 100 : 1;
    }

    static bool Test<T>() where T : I1
    {
        string m1 = T.M1();
        int m2 = T.M2();
        System.Console.WriteLine("T.M1 returns {0} ('Test2.M1' expected); T.M2 return {1} (2 expected)", m1, m2);
        return (m1 == "Test2.M1" && m2 == 2);
    }

}

public class Test2 : I1
{
    static string I1.M1()
    {
        return "Test2.M1";
    }
    static int I1.M2() => 2;
}
