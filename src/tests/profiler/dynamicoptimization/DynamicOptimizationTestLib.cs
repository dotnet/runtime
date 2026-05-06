// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Profiler.Tests
{
    public class DynamicOptimizationTestLib
    {

        public static int Main(string[] args)
        {
            return Inlinee();
        }

        // this should get inlined except if module is loaded while profiler has inlining disabled
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Inlinee()
        {
            return 100;
        }
    }
}
