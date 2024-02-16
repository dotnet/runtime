// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
       int ret;
       ret = (new TestClass() as ITestInterface).PublicInterfaceProtectedVirtualClass();
       if (ret != 100) return ret;

       ret = (new TestClass() as ITestInterface).PublicInterfaceProtectedClass();
       if (ret != 100) return ret;

       ret = (new TestClass() as ITestInterface).PublicInterfacePublicClass();
       if (ret != 100) return ret;

       return ret;
    }
}

public interface ITestInterface
{
    public int PublicInterfaceProtectedVirtualClass()=> 100;
    public int PublicInterfaceProtectedClass()=> 100;
    public int PublicInterfacePublicClass()=> 3;
}

public class TestClass : ITestInterface
{
    protected virtual int PublicInterfaceProtectedVirtualClass()=> 1;
    protected int PublicInterfaceProtectedClass()=> 2;
    public int PublicInterfacePublicClass()=> 100;
}


