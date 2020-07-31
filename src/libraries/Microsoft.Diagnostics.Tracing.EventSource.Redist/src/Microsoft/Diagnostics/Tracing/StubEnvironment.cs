// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Diagnostics.Tracing
{
    internal static class Environment
    {
        public static readonly int ProcessId = GetCurrentProcessId();

        private static int GetCurrentProcessId()
        {
            new SecurityPermission(PermissionState.Unrestricted).Assert();
            return (int)Interop.Kernel32.GetCurrentProcessId();
        }
    }

    internal static class BitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));

        public static int PopCount(uint value)
        {
            const uint c1 = 0x_55555555u;
            const uint c2 = 0x_33333333u;
            const uint c3 = 0x_0F0F0F0Fu;
            const uint c4 = 0x_01010101u;

            value = value - ((value >> 1) & c1);
            value = (value & c2) + ((value >> 2) & c2);
            value = (((value + (value >> 4)) & c3) * c4) >> 24;

            return (int)value;
        }

        public static int TrailingZeroCount(uint value)
        {
            if (value == 0)
                return 32;

            int count = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                count++;
            }
            return count;
        }
    }
}

internal static partial class Interop
{
    [SuppressUnmanagedCodeSecurityAttribute]
    internal static partial class Kernel32
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern uint GetCurrentProcessId();
    }
}
