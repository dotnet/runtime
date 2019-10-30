// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal class Test
{
    private static int Main()
    {
        Test[] test = new Test[0];
        IList<Test> ls = (IList<Test>)test;
        ReadOnlyCollection<Test> roc = new ReadOnlyCollection<Test>(ls);
        Console.WriteLine(roc.Count);
        return 100;
    }
}
