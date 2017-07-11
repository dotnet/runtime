// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Data
{
    internal static partial class LocalDBAPI
    {
        private const string const_localDbPrefix = @"(localdb)\";


        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private delegate int LocalDBFormatMessageDelegate(int hrLocalDB, UInt32 dwFlags, UInt32 dwLanguageId, StringBuilder buffer, ref UInt32 buflen);

        // check if name is in format (localdb)\<InstanceName - not empty> and return instance name if it is
        internal static string GetLocalDbInstanceNameFromServerName(string serverName)
        {
            if (serverName == null)
                return null;
            serverName = serverName.TrimStart(); // it can start with spaces if specified in quotes
            if (!serverName.StartsWith(const_localDbPrefix, StringComparison.OrdinalIgnoreCase))
                return null;
            string instanceName = serverName.Substring(const_localDbPrefix.Length).Trim();
            if (instanceName.Length == 0)
                return null;
            else
                return instanceName;
        }
    }
}
