// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static class ApplicationModel
    {
#if FEATURE_APPX
        // Cache the value in readonly static that can be optimized out by the JIT
        internal readonly static bool IsUap = IsAppXProcess() != Interop.BOOL.FALSE;

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL IsAppXProcess();
#endif
    }
}
