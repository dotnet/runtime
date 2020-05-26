// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;

public class Test
{
    public static void Main (string arg) {
        var client = new HttpClient();
        Console.WriteLine ($"Hello, {arg}!");
    }
}
