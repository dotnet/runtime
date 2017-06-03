// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        static partial void GetUserName(ref string username)
        {
            // Use GetUserNameExW, as GetUserNameW isn't available on all platforms, e.g. Win7
            var domainName = new StringBuilder(1024);
            uint domainNameLen = (uint)domainName.Capacity;
            if (Interop.Secur32.GetUserNameExW(Interop.Secur32.NameSamCompatible, domainName, ref domainNameLen) == 1)
            {
                string samName = domainName.ToString();
                int index = samName.IndexOf('\\');
                if (index != -1)
                {
                    username = samName.Substring(index + 1);
                    return;
                }
            }

            username = string.Empty;
        }

        static partial void GetDomainName(ref string userDomainName)
        {
            var domainName = new StringBuilder(1024);
            uint domainNameLen = (uint)domainName.Capacity;
            if (Interop.Secur32.GetUserNameExW(Interop.Secur32.NameSamCompatible, domainName, ref domainNameLen) == 1)
            {
                string samName = domainName.ToString();
                int index = samName.IndexOf('\\');
                if (index != -1)
                {
                    userDomainName = samName.Substring(0, index);
                    return;
                }
            }
            domainNameLen = (uint)domainName.Capacity;

            byte[] sid = new byte[1024];
            int sidLen = sid.Length;
            int peUse;
            if (!Interop.Advapi32.LookupAccountNameW(null, UserName, sid, ref sidLen, domainName, ref domainNameLen, out peUse))
            {
                throw new InvalidOperationException(Win32Marshal.GetExceptionForLastWin32Error().Message);
            }

            userDomainName = domainName.ToString();
        }
    }
}
