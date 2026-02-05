// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class AddFieldRVA
    {
        public static int LocalReadOnlySpan()
        {
            return -1;
        }

        class InnerClass
        {
            public int MemberFieldArray()
            {
                return -1;
            }
        }

        public static int MemberFieldArray()
        {
            return new InnerClass().MemberFieldArray();
        }

        public static int LocalStackAllocReadOnlySpan()
        {
            return -1;
        }

        public static ReadOnlySpan<byte> Utf8LiteralReadOnlySpan() => "0"u8;

        public static string StringLiteral() => "0";
    }
}
