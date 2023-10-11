// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret;
        ITestInterface itf = new TestClass();

        // The calls need to made twice as the first time is the trampoline
        // the section is post build_imt_slots

        ret = itf.TestMethod1(10);
        if (ret != 100) return ret;

        ret = itf.TestMethod1(10);
        if (ret != 100) return ret;

        ret = itf.TestMethod2(20);
        if (ret != 100) return ret;

        ret = itf.TestMethod2(20);
        if (ret != 100) return ret;

        ret = itf.TestMethod3(30);
        if (ret != 100) return ret;

        ret = itf.TestMethod3(30);
        if (ret != 100) return ret;

        return ret;
    }
}

public interface ITestInterface
{
    int TestMethod1(int arg) => arg + 90;

    // This static generic non virtual method was causing a mis-calculation of the vtable slot
    // in mono build_imt_slots
    static int StaticGenericMethod<T>()
    {
        return 1;
    }
    int TestMethod2(int arg) => arg + 80;
    int TestMethod3(int arg) => arg + 70;
}

public class TestClass : ITestInterface
{

}