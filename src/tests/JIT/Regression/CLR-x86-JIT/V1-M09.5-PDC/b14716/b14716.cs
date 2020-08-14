// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.Collections;
using System.Globalization;


public class Bug
{
    public static int Main(String[] args)
    {
        Decimal[] dcmlSecValues = new Decimal[2] { 2, 3 };
        Int32 aa = 1;
        Decimal dcml1 = --dcmlSecValues[aa];
        return 100;
    }
}
