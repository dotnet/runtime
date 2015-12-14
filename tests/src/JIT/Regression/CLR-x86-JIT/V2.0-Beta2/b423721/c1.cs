// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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