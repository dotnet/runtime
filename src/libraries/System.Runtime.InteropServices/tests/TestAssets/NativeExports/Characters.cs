﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Characters
    {
        [UnmanagedCallersOnly(EntryPoint = "ushort_return_as_uint")]
        public static uint ReturnUnicodeAsUInt(ushort input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "char_return_as_uint")]
        public static uint ReturnUIntAsUInt(uint input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "char_return_as_refushort")]
        public static void ReturnUIntAsRefUInt(uint input, ushort* res)
        {
            *res = (ushort)input;
        }

        [UnmanagedCallersOnly(EntryPoint = "char_reverse_buffer_ref")]
        public static void ReverseBuffer(ushort *buffer, int len)
        {
            var span = new Span<ushort>(buffer, len);
            span.Reverse();
        }
    }
}
