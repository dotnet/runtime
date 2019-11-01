// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal class My
{
    private static int Main()
    {
        My[] s = new My[0];
        IList<My> ls = (IList<My>)s;
        ReadOnlyCollection<My> roc = new ReadOnlyCollection<My>(ls);
        Console.WriteLine(roc.Count);
        return 100;
    }
}
