// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        /// <summary>
        /// Converts a Unicode network interface name to the locally unique identifier (LUID) for the interface.
        /// </summary>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-convertinterfacenametoluidw"/>
        /// <param name="interfaceName">The NULL-terminated Unicode string containing the network interface name.</param>
        /// <param name="interfaceLuid">A pointer to the NET_LUID for this interface.</param>
        /// <returns></returns>
        [LibraryImport(Libraries.IpHlpApi, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "ConvertInterfaceNameToLuidW")]
        internal static unsafe partial uint ConvertInterfaceNameToLuid(ReadOnlySpan<char> interfaceName, ref ulong interfaceLuid);
    }
}
