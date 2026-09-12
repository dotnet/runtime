// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.ExceptionServices
{
    public static partial class ExceptionHandling
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ExceptionHandling_TrySetFatalErrorHandler")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool TrySetFatalErrorHandler(IntPtr handler);
    }
}
