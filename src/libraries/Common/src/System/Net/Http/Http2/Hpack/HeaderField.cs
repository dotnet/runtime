// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal readonly struct HeaderField
    {
        // http://httpwg.org/specs/rfc7541.html#rfc.section.4.1
        public const int RfcOverhead = 32;

        public HeaderField(string name, string value)
        {
            Debug.Assert(name.Length > 0);

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public int Length => GetLength(Name.Length, Value.Length);

        public static int GetLength(int nameLength, int valueLength) => nameLength + valueLength + RfcOverhead;

        public override string ToString() => $"{Name ?? "<empty>"}: {Value ?? "<empty>"}";

    }
}
