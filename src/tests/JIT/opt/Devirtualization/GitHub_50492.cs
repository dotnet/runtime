// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

class Base
{
    public virtual int Test(Base b1, Base b2, bool p) => 0;

    public virtual int Foo() => 1;
}

class ClassA : Base
{
    public override int Test(Base b1, Base b2, bool p) => p ? b2.Foo() : 42;
}

class ClassB : ClassA
{
    public override int Test(Base b1, Base b2, bool p) => b1.Test(b1, b2, p);
}

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            // Make sure it doesn't assert, see https://github.com/dotnet/runtime/issues/50492
            Test(new ClassB(), new ClassA(), new Base(), true);
            Thread.Sleep(15);
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(Base b0, Base b1, Base b2, bool p)
    {
        return b0.Test(b1, b2, p);
    }
}
