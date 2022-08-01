// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [Flags]
        internal enum PolicyRights
        {
            POLICY_VIEW_LOCAL_INFORMATION = 0x00000001,
            POLICY_VIEW_AUDIT_INFORMATION = 0x00000002,
            POLICY_GET_PRIVATE_INFORMATION = 0x00000004,
            POLICY_TRUST_ADMIN = 0x00000008,
            POLICY_CREATE_ACCOUNT = 0x00000010,
            POLICY_CREATE_SECRET = 0x00000020,
            POLICY_CREATE_PRIVILEGE = 0x00000040,
            POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080,
            POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100,
            POLICY_AUDIT_LOG_ADMIN = 0x00000200,
            POLICY_SERVER_ADMIN = 0x00000400,
            POLICY_LOOKUP_NAMES = 0x00000800,
            POLICY_NOTIFICATION = 0x00001000,
        }

        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "LsaOpenPolicy", SetLastError = true)]
        private static partial uint LsaOpenPolicy(
            ref UNICODE_STRING SystemName,
            ref OBJECT_ATTRIBUTES ObjectAttributes,
            int AccessMask,
            out SafeLsaPolicyHandle PolicyHandle
        );

        internal static unsafe uint LsaOpenPolicy(
            string? SystemName,
            ref OBJECT_ATTRIBUTES Attributes,
            int AccessMask,
            out SafeLsaPolicyHandle PolicyHandle)
        {
            UNICODE_STRING systemNameUnicode = default;
            if (SystemName != null)
            {
                fixed (char* c = SystemName)
                {
                    systemNameUnicode.Length = checked((ushort)(SystemName.Length * sizeof(char)));
                    systemNameUnicode.MaximumLength = checked((ushort)(SystemName.Length * sizeof(char)));
                    systemNameUnicode.Buffer = (IntPtr)c;
                    return LsaOpenPolicy(ref systemNameUnicode, ref Attributes, AccessMask, out PolicyHandle);
                }
            }
            else
            {
                return LsaOpenPolicy(ref systemNameUnicode, ref Attributes, AccessMask, out PolicyHandle);
            }
        }
    }
}
