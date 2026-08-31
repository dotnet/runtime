// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_DelegInstanceFtn
{
    private delegate object MyDeleg(string s);

    private object f2(string s)
    {
        if (s == "test2")
            return 100;
        else
            return 1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Test_DelegInstanceFtn t = new Test_DelegInstanceFtn();
        MyDeleg d2 = new MyDeleg(t.f2);
        return Convert.ToInt32(d2("test2"));
    }
}
