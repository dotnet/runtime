// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class AddFieldRVA
    {
        public static int LocalReadOnlySpan()
        {
            ReadOnlySpan<int> s = [1, 2, 3];
            return s.Length;
        }

        class InnerClass
        {
            byte[] _byteArray = { 1, 2, 3, 4 };
            public int MemberFieldArray()
            {
                return _byteArray.Length;
            }
        }

        public static int MemberFieldArray()
        {
            return new InnerClass().MemberFieldArray();
        }

        // public static int LocalStackAllocReadOnlySpan()
        // {
        //     ReadOnlySpan<byte> s = stackalloc byte[] { 1, 2, 3, 4, 5 };
        //     return s.Length;
        // }

        public static ReadOnlySpan<byte> Utf8LiteralReadOnlySpan() => "123456"u8;

        public static string StringLiteral() => "1234567";
    }
}
