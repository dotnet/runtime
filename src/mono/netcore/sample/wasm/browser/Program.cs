// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Test
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Runtime.InteropServices.JavaScript.Runtime", "System.Private.Runtime.InteropServices.JavaScript")]
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestMeaning()
        {
            return 42;
        }
    }
}
