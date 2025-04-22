// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Xunit;

namespace PrivateLib
{
    class Class1
    {
        static int StaticField;
        int InstanceField = 456;

        static Class1()
        {
            StaticField = 123;
        }

        Class1() { }

        static Class1 GetClass()
        {
            return new Class1();
        }

        List<Class2> GetListOfClass2()
        {
            return new List<Class2>();
        }
    }

    class Class2 { }
}