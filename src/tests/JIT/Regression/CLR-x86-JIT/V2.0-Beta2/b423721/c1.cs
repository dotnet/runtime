// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Test
{

    public class C1<T>
    {

        public static string GetString()
        {
            return "foo";
        }
    }

    public class C1Helper
    {
        public static bool IsFoo(string val)
        {
            return ((object)"foo" == (object)val);
        }
    }

}
