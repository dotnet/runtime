// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public class Runtime_81081
{
    [Fact]
    public static int TestEntryPoint()
    {
        Test(1234, default);
        return 100;
    }

    static int Test(int count, S16 s)
    {
        object o = "1234";
        if (count == 0 || o.GetHashCode() == 1234)
            return 42;

        return Test(count - 1, s);
    }

    struct S16
    {
        public object A, B;
    }
}
