// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

public class Test
{
    public int foo;

    public override bool Equals(object o) => false;
    public override int GetHashCode() => 0;

    public static bool operator ==(Test t1, Test t2) {
        if (ReferenceEquals(t1, t1))
            return true;
        return t1.foo == t2.foo;
    }

    public static bool operator !=(Test t1, Test t2) {
        if (ReferenceEquals(t1, t1))
            return true;
        return t1.foo == t2.foo;
    }

    [Fact]
    public static int TestEntryPoint() {
        var t1 = new Test () { foo = 1 };
        var t2 = new Test () { foo = 2 };
        if (t1 == t2)
            return 100;
        return 100;
    }
}
