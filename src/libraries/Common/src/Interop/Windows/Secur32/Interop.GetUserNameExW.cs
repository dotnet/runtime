// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Secur32
    {
        [LibraryImport(Libraries.Secur32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial BOOLEAN GetUserNameExW(int NameFormat, ref char lpNameBuffer, ref uint lpnSize);

        internal const int NameSamCompatible = 2;
    }
}
