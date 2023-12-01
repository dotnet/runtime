// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal struct RawIndentation(byte? @byte, Memory<byte> bytes) : IEquatable<RawIndentation>
    {
        public readonly byte Byte => @byte ?? JsonConstants.Space;
        public readonly ReadOnlySpan<byte> Bytes => bytes.Span;

        public readonly bool Equals(RawIndentation other) => Byte == other.Byte && Bytes.SequenceEqual(other.Bytes);

        public override bool Equals(object? obj) => obj is RawIndentation indentation && Equals(indentation);

        public override readonly int GetHashCode()
        {
            HashCode hc = default;
            hc.Add(Byte);
            hc.Add(Bytes.GetHashCode());
            return hc.ToHashCode();
        }

#if !NETCOREAPP
        /// <summary>
        /// Polyfill for System.HashCode.
        /// </summary>
        private struct HashCode
        {
            private int _hashCode;
            public void Add<T>(T? value) => _hashCode = (_hashCode, value).GetHashCode();
            public int ToHashCode() => _hashCode;
        }
#endif
    }
}
