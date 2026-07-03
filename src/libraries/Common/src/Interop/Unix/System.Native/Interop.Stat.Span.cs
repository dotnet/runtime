// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Stat", SetLastError = true)]
        internal static partial int Stat([MarshalUsing(typeof(SpanOfCharAsUtf8StringMarshaller))] ReadOnlySpan<char> path, out FileStatus output);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LStat", SetLastError = true)]
        internal static partial int LStat([MarshalUsing(typeof(SpanOfCharAsUtf8StringMarshaller))] ReadOnlySpan<char> path, out FileStatus output);
    }
}
