// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestMeaning()
        {
            return 42;
        }
    }
}
