// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a COSE header value.
    /// </summary>
    public readonly struct CoseHeaderValue : IEquatable<CoseHeaderValue>
    {
        /// <summary>
        /// Gets the CBOR-encoded value of this instance.
        /// </summary>
        /// <value>A view of the CBOR-encoded value as a contiguous region of memory.</value>
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

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a CBOR-encoded value.
        /// </summary>
        /// <param name="encodedValue">A CBOR-encoded value to represent.</param>
        /// <returns>An instance that represents the encoded value.</returns>
        public static CoseHeaderValue FromEncodedValue(ReadOnlySpan<byte> encodedValue)
        {
            var encodedValueCopy = new ReadOnlyMemory<byte>(encodedValue.ToArray());
            return FromEncodedValue(encodedValueCopy);
        }

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a CBOR-encoded value.
        /// </summary>
        /// <param name="encodedValue">A CBOR-encoded value to represent.</param>
        /// <returns>An instance that represents the encoded value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encodedValue"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a signed integer.
        /// </summary>
        /// <param name="value">The value to represent.</param>
        /// <returns>An instance that represents the specified value.</returns>
        public static CoseHeaderValue FromInt32(int value)
        {
            var writer = new CborWriter();
            writer.WriteInt32(value);

            return FromEncodedValue(Encode(writer));
        }

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a string.
        /// </summary>
        /// <param name="value">The value to represent.</param>
        /// <returns>An instance that represents the specified value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a span of bytes.
        /// </summary>
        /// <param name="value">The bytes to be encoded and that the instance will represent.</param>
        /// <returns>An instance that represents the CBOR-encoded <paramref name="value"/>.</returns>
        /// <seealso cref="FromEncodedValue(ReadOnlySpan{byte})"/>
        public static CoseHeaderValue FromBytes(ReadOnlySpan<byte> value)
        {
            var writer = new CborWriter();
            writer.WriteByteString(value);

            return FromEncodedValue(Encode(writer));
        }

        /// <summary>
        /// Creates a <see cref="CoseHeaderValue"/> instance from a byte array.
        /// </summary>
        /// <param name="value">The bytes to be encoded and that the instance will represent.</param>
        /// <returns>An instance that represents the CBOR-encoded <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <seealso cref="FromEncodedValue(byte[])"/>
        public static CoseHeaderValue FromBytes(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return FromBytes(value.AsSpan());
        }

        /// <summary>
        /// Gets the value as a signed integer.
        /// </summary>
        /// <returns>The value as a signed integer.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as a 32-bit signed integer.</exception>
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

        /// <summary>
        /// Gets the value as a text string.
        /// </summary>
        /// <returns>The value as a text string.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as text string.</exception>
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

        /// <summary>
        /// Gets the CBOR-encoded value as a byte string.
        /// </summary>
        /// <returns>The decoded value as a byte array.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as byte string.</exception>
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

        /// <summary>
        /// Gets the CBOR-encoded value as a byte string.
        /// </summary>
        /// <param name="destination">The buffer in which to write the decoded value.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too small to hold the value.</exception>
        /// <exception cref="InvalidOperationException">The value could not be decoded as byte string.</exception>
        public int GetValueAsBytes(Span<byte> destination)
        {
            var reader = new CborReader(EncodedValue);
            int bytesWritten;

            try
            {
                if (!reader.TryReadByteString(destination, out bytesWritten))
                {
                    throw new ArgumentException(SR.Argument_DestinationTooSmall, nameof(destination));
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

        /// <summary>
        /// Returns a value indicating whether this instance is equal to the specified instance.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if the value parameter equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CoseHeaderValue otherObj && Equals(otherObj);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if value is an instance of <see cref="CoseHeaderValue"/> and equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CoseHeaderValue other) => EncodedValue.Span.SequenceEqual(other.EncodedValue.Span);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            HashCode hashCode = default;
#if NET
            hashCode.AddBytes(EncodedValue.Span);
#else
            foreach (byte b in EncodedValue.Span)
            {
                hashCode.Add(b);
            }
#endif
            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Determines whether two specified header value instances are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if left and right represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(CoseHeaderValue left, CoseHeaderValue right) => left.Equals(right);

        /// <summary>
        /// Determines whether two specified header value instances are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if left and right do not represent the same value; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(CoseHeaderValue left, CoseHeaderValue right) => !left.Equals(right);
    }
}
