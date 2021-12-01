// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

/// <summary>
/// Tests trimming an app that uses DispatchProxy works correctly
/// even if not all the members on the interface and base type are used.
/// </summary>
class Program
{
    static int Main()
    {
        IFoo foo = CountingProxy.Wrap(new Foo());

        foo.Property1 = 5;
        foo.Method1();
        foo.Method2();

        if (((CountingProxy)foo).InvocationCount != 3)
        {
            return -1;
        }

        return 100;
    }
}

public interface IFoo
{
    public int Property1 { get; set; }
    public int UnusedProperty { get; set; }

    public void Method1();
    public void Method2();
    public void UnusedMethod3();
}

class Foo : IFoo
{
    public int Property1 { get; set; }
    public int UnusedProperty { get; set; }

    public void Method1() { }
    public void Method2() { }
    public void UnusedMethod3() { }
}

public class CountingProxy : DispatchProxy
{
    private IFoo _inner;
    public int InvocationCount { get; private set; }

    public static IFoo Wrap(IFoo inner)
    {
        IFoo wrapped = Create<IFoo, CountingProxy>();
        ((CountingProxy)wrapped)._inner = inner;
        return wrapped;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        InvocationCount++;

        return targetMethod.Invoke(_inner, args);
    }

    public static void UnusedStatic() { }
    public void UnusedInstance() { }
}
