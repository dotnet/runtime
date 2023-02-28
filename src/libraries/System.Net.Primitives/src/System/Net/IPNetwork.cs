// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net
{
    /// <summary>
    /// Provides an Internet Protocol (IP) subnet/range in CIDR notation.
    /// </summary>
    public readonly struct IPNetwork : IEquatable<IPNetwork>, ISpanFormattable, ISpanParsable<IPNetwork>
    {
        public IPAddress BaseAddress { get; private init; }
        public int PrefixLength { get; private init; }

        /*
         We can also add two more easy to implement properties:
         * AddressCount - a lazily calculated property showing the amount of potential address this instance "Contains". 2^(128_OR_32 - PrefixLength)
            * 2^128 is going to fit into BigInteger, which causes a concern for memory allocation, since for actually big integers BigInteger seems to allocate a byte array.
            * We can just have a Nullable<BigInteger> backing field for the lazy property. But we should be fine with the increased byte size for IPNetwork type itself caused by that.
         * Since BaseAddress represents the first address in the range, does it make sense to have a property that represents the last address?
         */

        private const int bitsPerByte = 8;
        private const string
            baseAddressConstructorParamName = "baseAddress",
            prefixLengthConstructorParamName = "prefixLength";
        public IPNetwork(IPAddress baseAddress, int prefixLength)
        {
            BaseAddress = baseAddress;
            PrefixLength = prefixLength;

            var validationError = Validate();
            if (validationError.HasValue)
            {
                throw validationError.Value.ConstructorExceptionFactoryMethod();
            }
        }

        public bool Contains(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.AddressFamily != BaseAddress.AddressFamily)
            {
                return false;
            }

            // TODO: this can be made much easier and potentially more performant for IPv4 if IPAddress.PrivateAddress is made internal (currently private)
            // to be discussed in the PR

            var expectedBytesCount = GetAddressFamilyByteLength(BaseAddress.AddressFamily);
            if (expectedBytesCount * bitsPerByte == PrefixLength)
            {
                return BaseAddress.Equals(address);
            }

            Span<byte> baseAddressBytes = stackalloc byte[expectedBytesCount];
            if (!BaseAddress.TryWriteBytes(baseAddressBytes, out _))
            {
                throw new UnreachableException();
            }

            Span<byte> otherAddressBytes = stackalloc byte[expectedBytesCount];
            if (!address.TryWriteBytes(otherAddressBytes, out _))
            {
                throw new UnreachableException();
            }

            for (int processedBitsCount = 0, i = 0; processedBitsCount < PrefixLength; processedBitsCount += bitsPerByte, i++)
            {
                var baseAddressByte = baseAddressBytes[i];
                var otherAddressByte = otherAddressBytes[i];

                if (processedBitsCount + bitsPerByte <= PrefixLength)
                {
                    if (baseAddressByte == otherAddressByte)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var rightShiftAmount = bitsPerByte - (PrefixLength - processedBitsCount);
                    baseAddressByte >>= rightShiftAmount;
                    otherAddressByte >>= rightShiftAmount;
                    if (baseAddressByte == otherAddressByte)
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        #region Parsing (public)
        public static IPNetwork Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s.AsSpan());
        }
        public static IPNetwork Parse(ReadOnlySpan<char> s)
        {
            if (TryParseInternal(s, out var result, out var error))
            {
                return result;
            }

            throw new FormatException(error);
        }

        public static bool TryParse(string? s, out IPNetwork result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParseInternal(s.AsSpan(), out result, out _);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IPNetwork result)
        {
            return TryParseInternal(s, out result, out _);
        }
        #endregion

        #region Private methods
        private static bool TryParseInternal(ReadOnlySpan<char> s, out IPNetwork result, [NotNullWhen(false)] out string? error)
        {
            const char separator = '/';
            const int maxCharsCountAfterIpAddress = 4;

            int separatorExpectedStartingIndex = s.Length - maxCharsCountAfterIpAddress;
            int separatorIndex = s
                .Slice(separatorExpectedStartingIndex)
                .IndexOf(separator);
            if (separatorIndex != -1)
            {
                separatorIndex += separatorExpectedStartingIndex;

                var ipAddressSpan = s.Slice(0, separatorIndex);
                var prefixLengthSpan = s.Slice(separatorIndex + 1);

                if (IPAddress.TryParse(ipAddressSpan, out var ipAddress) && int.TryParse(prefixLengthSpan, out var prefixLength))
                {
                    result = new IPNetwork
                    {
                        BaseAddress = ipAddress,
                        PrefixLength = prefixLength
                    };

                    error = result.Validate()?.ParseExceptionMessage;
                    return error == null;
                }
                else
                {
                    error = SR.dns_bad_ip_network;
                    result = default;
                    return false;
                }
            }
            else
            {
                error = SR.dns_bad_ip_network;
                result = default;
                return false;
            }
        }

        private readonly record struct ValidationError(
            Func<Exception> ConstructorExceptionFactoryMethod,
            string ParseExceptionMessage);

        private static readonly ValidationError
            _baseAddressIsNullError = new ValidationError(() => new ArgumentNullException(baseAddressConstructorParamName), string.Empty),
            _baseAddressHasSetBitsInMaskError = new ValidationError(() => new ArgumentException(baseAddressConstructorParamName), SR.dns_bad_ip_network),
            _prefixLengthLessThanZeroError = new ValidationError(() => new ArgumentOutOfRangeException(prefixLengthConstructorParamName), SR.dns_bad_ip_network),
            _prefixLengthGreaterThanAllowedError = new ValidationError(() => new ArgumentOutOfRangeException(prefixLengthConstructorParamName), SR.dns_bad_ip_network);

        private ValidationError? Validate()
        {
            if (BaseAddress == null)
            {
                return _baseAddressIsNullError;
            }

            if (PrefixLength < 0)
            {
                return _prefixLengthLessThanZeroError;
            }

            if (PrefixLength > GetAddressFamilyByteLength(BaseAddress.AddressFamily) * bitsPerByte)
            {
                return _prefixLengthGreaterThanAllowedError;
            }

            if (IsAnyMaskBitOnForBaseAddress())
            {
                return _baseAddressHasSetBitsInMaskError;
            }

            return default;
        }

        /// <summary>
        /// Method to determine whether any bit in <see cref="BaseAddress"/>'s variable/mask part is 1.
        /// </summary>
        private bool IsAnyMaskBitOnForBaseAddress()
        {
            // TODO: same as with Contains method - this can be made much easier and potentially more performant for IPv4 if IPAddress.PrivateAddress is made internal (currently private)
            // to be discussed in the PR

            Span<byte> addressBytes = stackalloc byte[GetAddressFamilyByteLength(BaseAddress.AddressFamily)];
            if (!BaseAddress.TryWriteBytes(addressBytes, out int bytesWritten))
            {
                throw new UnreachableException();
            }

            var addressBitsCount = addressBytes.Length * bitsPerByte;

            for (int addressBytesIndex = addressBytes.Length - 1, numberOfEndingBitsToBeOff = addressBitsCount - PrefixLength;
                addressBytesIndex >= 0 && numberOfEndingBitsToBeOff > 0;
                addressBytesIndex--, numberOfEndingBitsToBeOff -= bitsPerByte)
            {
                byte maskForByte = unchecked((byte)(byte.MaxValue << Math.Min(numberOfEndingBitsToBeOff, bitsPerByte)));
                var addressByte = addressBytes[addressBytesIndex];
                if ((addressByte & maskForByte) != addressByte)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetAddressFamilyByteLength(AddressFamily addressFamily)
            => addressFamily switch
            {
                AddressFamily.InterNetwork => IPAddressParserStatics.IPv4AddressBytes,
                AddressFamily.InterNetworkV6 => IPAddressParserStatics.IPv6AddressBytes,
                _ => throw new UnreachableException("Unknown address family")
            };
        #endregion

        #region Formatting (public)
        public override string ToString()
        {
            return $"{BaseAddress}/{PrefixLength}";
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (!BaseAddress.TryFormat(destination, out charsWritten))
            {
                charsWritten = 0;
                return false;
            }

            if (!PrefixLength.TryFormat(destination.Slice(charsWritten), out var prefixLengthCharsWritten))
            {
                return false;
            }

            charsWritten += prefixLengthCharsWritten;
            return true;
        }
        #endregion

        #region Equality and GetHashCode
        public bool Equals(IPNetwork other)
        {
            return BaseAddress.Equals(other.BaseAddress) && PrefixLength == other.PrefixLength;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not IPNetwork other)
            {
                return false;
            }

            return Equals(other);
        }

        public static bool operator ==(IPNetwork left, IPNetwork right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(IPNetwork left, IPNetwork right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BaseAddress, PrefixLength);
        }
        #endregion

        #region Explicit ISpanFormattable, ISpanParsable
        string IFormattable.ToString(string? format, IFormatProvider? provider)
        {
            return ToString();
        }
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }
        static IPNetwork IParsable<IPNetwork>.Parse([NotNull] string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s);
        }
        static bool IParsable<IPNetwork>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out IPNetwork result)
        {
            return TryParse(s, out result);
        }
        static IPNetwork ISpanParsable<IPNetwork>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }
        static bool ISpanParsable<IPNetwork>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out IPNetwork result)
        {
            return TryParse(s, out result);
        }
        #endregion
    }
}
