// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public sealed partial class AppDomain
    {
        static partial void SetFirstChanceExceptionHandler()
            => SetFirstChanceExceptionHandlerInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AppDomain_SetFirstChanceExceptionHandler")]
        [SuppressGCTransition]
        private static partial void SetFirstChanceExceptionHandlerInternal();
    }
}
