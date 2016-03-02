// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

class Test
{
    static int Main()
    {
        Lazy<int> l = new Lazy<int>(() => 100);
        Console.WriteLine(l.Value);
        return l.Value;
    }
}