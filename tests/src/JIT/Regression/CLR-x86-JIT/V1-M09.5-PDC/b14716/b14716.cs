// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
