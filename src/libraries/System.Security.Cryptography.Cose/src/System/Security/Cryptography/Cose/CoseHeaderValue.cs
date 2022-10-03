// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private static CoseHeaderValue FromEncodedValue(ReadOnlyMemory<byte> encodedValue)
        {
            // We don't validate here as we need to know in which label the value is going to be used to validate even more semantics.
            CoseHeaderValue value = new CoseHeaderValue(encodedValue);
            return value;
        }

        public static CoseHeaderValue FromEncodedValue(ReadOnlySpan<byte> encodedValue)
        {
            var encodedValueCopy = new ReadOnlyMemory<byte>(encodedValue.ToArray());
            return FromEncodedValue(encodedValueCopy);
        }

        public static CoseHeaderValue FromEncodedValue(byte[] encodedValue)
        {
            if (encodedValue == null)
            {
                throw new ArgumentNullException(nameof(encodedValue));
            }

            return FromEncodedValue(encodedValue.AsSpan());
        }

        private static ReadOnlyMemory<byte> Encode(CborWriter writer)
        {
            byte[] buffer = new byte[writer.BytesWritten];
            writer.Encode(buffer);

            return buffer.AsMemory();
        }

        public static CoseHeaderValue FromInt32(int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);

            return FromEncodedValue(Encode(writer));
        }

        public static CoseHeaderValue FromString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var writer = new CborWriter();
            writer.WriteTextString(value);

            return FromEncodedValue(Encode(writer));
        }

        public static CoseHeaderValue FromBytes(ReadOnlySpan<byte> value)
        {
            var writer = new CborWriter();
            writer.WriteByteString(value);

            return FromEncodedValue(Encode(writer));
        }

        public static CoseHeaderValue FromBytes(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return FromBytes(value.AsSpan());
        }

        public int GetValueAsInt32()
        {
            var reader = new CborReader(EncodedValue);
            int retVal;

            try
            {
                retVal = reader.ReadInt32();
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException or OverflowException)
            {
                throw new InvalidOperationException(SR.CoseHeaderValueErrorWhileDecoding, ex);
            }

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public string GetValueAsString()
        {
            var reader = new CborReader(EncodedValue);
            string retVal;

            try
            {
                retVal = reader.ReadTextString();
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new InvalidOperationException(SR.CoseHeaderValueErrorWhileDecoding, ex);
            }

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public byte[] GetValueAsBytes()
        {
            var reader = new CborReader(EncodedValue);
            byte[] retVal;

            try
            {
                retVal = reader.ReadByteString();
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new InvalidOperationException(SR.CoseHeaderValueErrorWhileDecoding, ex);
            }

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return retVal;
        }

        public int GetValueAsBytes(Span<byte> destination)
        {
            var reader = new CborReader(EncodedValue);
            int bytesWritten;

            try
            {
                if (!reader.TryReadByteString(destination, out bytesWritten))
                {
                    throw new ArgumentException(SR.Argument_EncodeDestinationTooSmall);
                }
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new InvalidOperationException(SR.CoseHeaderValueErrorWhileDecoding, ex);
            }

            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid));
            }

            return bytesWritten;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CoseHeaderValue otherObj && Equals(otherObj);

        public bool Equals(CoseHeaderValue other) => EncodedValue.Span.SequenceEqual(other.EncodedValue.Span);

        public override int GetHashCode()
        {
            HashCode hashCode = default;
            foreach (byte b in EncodedValue.Span)
            {
                hashCode.Add(b);
            }
            return hashCode.ToHashCode();
        }

        public static bool operator ==(CoseHeaderValue left, CoseHeaderValue right) => left.Equals(right);

        public static bool operator !=(CoseHeaderValue left, CoseHeaderValue right) => !left.Equals(right);
    }
}
