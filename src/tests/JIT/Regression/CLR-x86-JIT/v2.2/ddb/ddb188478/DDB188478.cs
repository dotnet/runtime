// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

public class Test_DDB188478
{
    [Fact]
    public static int TestEntryPoint()
    {
        Test_DDB188478[] test = new Test_DDB188478[0];
        IList<Test_DDB188478> ls = (IList<Test_DDB188478>)test;
        ReadOnlyCollection<Test_DDB188478> roc = new ReadOnlyCollection<Test_DDB188478>(ls);
        Console.WriteLine(roc.Count);
        return 100;
    }
}
