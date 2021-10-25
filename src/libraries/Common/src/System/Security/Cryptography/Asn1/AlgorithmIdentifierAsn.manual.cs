// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    internal partial struct AlgorithmIdentifierAsn
    {
        internal static readonly ReadOnlyMemory<byte> ExplicitDerNull = new byte[] { 0x05, 0x00 };

        internal bool Equals(ref AlgorithmIdentifierAsn other)
        {
            if (Algorithm != other.Algorithm)
            {
                return false;
            }

            bool isNull = RepresentsNull(Parameters);
            bool isOtherNull = RepresentsNull(other.Parameters);

            if (isNull != isOtherNull)
            {
                return false;
            }

            if (isNull)
            {
                return true;
            }

            return Parameters!.Value.Span.SequenceEqual(other.Parameters!.Value.Span);
        }

        internal readonly bool HasNullEquivalentParameters()
        {
            return RepresentsNull(Parameters);
        }

        internal static bool RepresentsNull(ReadOnlyMemory<byte>? parameters)
        {
            if (parameters == null)
            {
                return true;
            }

            ReadOnlySpan<byte> span = parameters.Value.Span;

            if (span.Length != 2)
            {
                return false;
            }

            if (span[0] != 0x05)
            {
                return false;
            }

            return span[1] == 0;
        }
    }
}
