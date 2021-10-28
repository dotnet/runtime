// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaOpenPolicy", CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial uint LsaOpenPolicy(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaOpenPolicy", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint LsaOpenPolicy(
#endif
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
