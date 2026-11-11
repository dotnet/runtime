// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace ApplyUpdateReferencedAssembly
{
    public class MethodBody1 {
        // The linker must not fold calls to the baseline before updates are applied.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StaticMethod1 () {
            return "OLD STRING";
        }
    }
}
