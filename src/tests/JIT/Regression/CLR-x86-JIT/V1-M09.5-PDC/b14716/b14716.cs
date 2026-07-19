// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b14716;

using System;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Xunit;


public class Bug
{
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        Decimal[] dcmlSecValues = new Decimal[2] { 2, 3 };
        Int32 aa = 1;
        Decimal dcml1 = --dcmlSecValues[aa];
    }
}
