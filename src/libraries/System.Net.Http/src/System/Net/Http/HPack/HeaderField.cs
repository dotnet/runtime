// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace System.Net.Http.HPack
{
    internal struct HeaderField
    {
        // http://httpwg.org/specs/rfc7541.html#rfc.section.4.1
        public const int RfcOverhead = 32;

        public HeaderField(Span<byte> name, Span<byte> value)
        {
            Name = new byte[name.Length];
            name.CopyTo(Name);

            Value = new byte[value.Length];
            value.CopyTo(Value);
        }

        public byte[] Name { get; }

        public byte[] Value { get; }

        public int Length => GetLength(Name.Length, Value.Length);

        public static int GetLength(int nameLength, int valueLenth) => nameLength + valueLenth + 32;
    }
}
