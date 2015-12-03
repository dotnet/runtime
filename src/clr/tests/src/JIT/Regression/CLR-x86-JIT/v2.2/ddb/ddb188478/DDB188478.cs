// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
