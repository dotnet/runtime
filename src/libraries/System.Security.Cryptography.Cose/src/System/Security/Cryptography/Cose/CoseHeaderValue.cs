// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    public readonly struct CoseHeaderValue : IEquatable<CoseHeaderValue>
    {
        public readonly ReadOnlyMemory<byte> EncodedValue { get; }
        private CoseHeaderValue(ReadOnlyMemory<byte> encodedValue)
        {
            EncodedValue = encodedValue;
        }

        public static CoseHeaderValue FromEncodedValue(ReadOnlySpan<byte> encodedValue)
        {
            // We don't validate here as we need to know in which label the value is going to be used to validate even more semantics.
            var encodedValueCopy = new ReadOnlyMemory<byte>(encodedValue.ToArray());
            CoseHeaderValue value = new CoseHeaderValue(encodedValueCopy);
            return value;
        }

        public static CoseHeaderValue FromEncodedValue(byte[] encodedValue)
            => FromEncodedValue(encodedValue.AsSpan());

        public static CoseHeaderValue FromInt32(int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);

            return FromEncodedValue(writer.Encode());
        }

        public static CoseHeaderValue FromString(string value)
        {
            var writer = new CborWriter();
            writer.WriteTextString(value);

            return FromEncodedValue(writer.Encode());
        }

        public static CoseHeaderValue FromBytes(ReadOnlySpan<byte> value)
        {
            var writer = new CborWriter();
            writer.WriteByteString(value);

            return FromEncodedValue(writer.Encode());
        }

        public static CoseHeaderValue FromBytes(byte[] value)
        {
            var writer = new CborWriter();
            writer.WriteByteString(value);

            return FromEncodedValue(writer.Encode());
        }

        public int GetValueAsInt32()
        {
            var reader = new CborReader(EncodedValue);
            int retVal = reader.ReadInt32();

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public string GetValueAsString()
        {
            var reader = new CborReader(EncodedValue);
            string retVal = reader.ReadTextString();

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public byte[] GetValueAsBytes()
        {
            var reader = new CborReader(EncodedValue);
            byte[] retVal = reader.ReadByteString();

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public int GetValueAsBytes(Span<byte> destination)
        {
            var reader = new CborReader(EncodedValue);
            reader.TryReadByteString(destination, out int bytesWritten);

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return bytesWritten;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CoseHeaderValue otherObj && Equals(otherObj);

        public bool Equals(CoseHeaderValue other) => EncodedValue.Span.SequenceEqual(other.EncodedValue.Span);

        public override int GetHashCode() => EncodedValue.GetHashCode();

        public static bool operator ==(CoseHeaderValue left, CoseHeaderValue right) => left.Equals(right);

        public static bool operator !=(CoseHeaderValue left, CoseHeaderValue right) => !left.Equals(right);
    }
}
