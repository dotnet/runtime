// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base
{
    public virtual int Foo() { return 33; }
    
    static BaseSealed s_Default = new BaseSealed();

    public static Base Default => s_Default;
}

sealed class BaseSealed : Base {}

// The jit can devirtualize the call to Foo when initializing y,
// but not when initializing x.

public class Test_sealeddefault
{
    [Fact]
    public static int TestEntryPoint()
    {
        Base b = Base.Default;
        int x = b.Foo();
        int y = Base.Default.Foo();
        return (x == 33 && y == 33 ? 100 : -1);
    }
}
