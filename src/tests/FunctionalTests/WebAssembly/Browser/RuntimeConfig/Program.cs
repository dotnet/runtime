// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestMeaning()
        {
            var config = AppContext.GetData("test_runtimeconfig_json");
            int result = ((string)config).Equals("25") ? 42 : 1;
            return result;
        }
    }
}