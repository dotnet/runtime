// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Xunit;

namespace PrivateLib
{
    class Class1
    {
        public static Class1 GetClass()
        {
            return new Class1();
        }

        public static List<Class1> GetListOfClass()
        {
            return new List<Class1>();
        }
    }
}