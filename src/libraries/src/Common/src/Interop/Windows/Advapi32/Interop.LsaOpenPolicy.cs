// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaOpenPolicy", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint LsaOpenPolicy(
            ref UNICODE_STRING SystemName,
            ref OBJECT_ATTRIBUTES ObjectAttributes,
            int AccessMask,
            out SafeLsaPolicyHandle PolicyHandle
        );

        internal static unsafe uint LsaOpenPolicy(
            string SystemName,
            ref OBJECT_ATTRIBUTES Attributes,
            int AccessMask,
            out SafeLsaPolicyHandle PolicyHandle)
        {
            var systemNameUnicode = new UNICODE_STRING();
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
