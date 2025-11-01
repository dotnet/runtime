// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "BundleNative_TryGetBundleInformation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TryGetBundleInformation([MarshalAs(UnmanagedType.Bool)] out bool isExtracted, StringHandleOnStack extractPath);
    }
}
