// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IsAtomicNonInheritablePipeCreationSupported", SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsAtomicNonInheritablePipeCreationSupportedImpl();

        private static NullableBool s_atomicNonInheritablePipeCreationSupported;

        internal static bool IsAtomicNonInheritablePipeCreationSupported
        {
            get
            {
                NullableBool isSupported = s_atomicNonInheritablePipeCreationSupported;
                if (isSupported == NullableBool.Undefined)
                {
                    s_atomicNonInheritablePipeCreationSupported = isSupported = IsAtomicNonInheritablePipeCreationSupportedImpl() ? NullableBool.True : NullableBool.False;
                }
                return isSupported == NullableBool.True;
            }
        }
    }
}
