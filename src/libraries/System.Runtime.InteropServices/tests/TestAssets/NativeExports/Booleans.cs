// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Booleans
    {
        [UnmanagedCallersOnly(EntryPoint = "bytebool_return_as_uint")]
        public static uint ReturnByteAsUInt(byte input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "variantbool_return_as_uint")]
        public static uint ReturnUShortAsUInt(ushort input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "winbool_return_as_uint")]
        public static uint ReturnUIntAsUInt(uint input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "winbool_return_as_refuint")]
        public static void ReturnUIntAsRefUInt(uint input, uint* res)
        {
            *res = input;
        }
    }
}
