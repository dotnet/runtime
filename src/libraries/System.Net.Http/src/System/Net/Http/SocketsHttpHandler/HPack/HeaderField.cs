// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.HPack
{
    internal struct HeaderField
    {
        // http://httpwg.org/specs/rfc7541.html#rfc.section.4.1
        public const int RfcOverhead = 32;

        public HeaderField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            // TODO: We're allocating here on every new table entry.
            // That means a poorly-behaved server could cause us to allocate repeatedly.
            // We should revisit our allocation strategy here so we don't need to allocate per entry
            // and we have a cap to how much allocation can happen per dynamic table
            // (without limiting the number of table entries a server can provide within the table size limit).
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
