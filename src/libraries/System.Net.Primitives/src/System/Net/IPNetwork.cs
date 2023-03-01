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
        public IPAddress BaseAddress { get; }
        public int PrefixLength { get; }

        private const char AddressAndPrefixLengthSeparator = '/';
        private const int BitsPerByte = 8;

        private const string BaseAddressConstructorParamName = "baseAddress";
        private const string PrefixLengthConstructorParamName = "prefixLength";
        public IPNetwork(IPAddress baseAddress, int prefixLength)
            : this(baseAddress, prefixLength, validateAndThrow: true)
        {
        }

        private IPNetwork(IPAddress baseAddress, int prefixLength, bool validateAndThrow)
        {
            BaseAddress = baseAddress;
            PrefixLength = prefixLength;

            if (validateAndThrow)
            {
                ValidationError? validationError = Validate();
                if (validationError.HasValue)
                {
                    throw validationError.Value.ConstructorExceptionFactoryMethod();
                }
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
            if (expectedBytesCount * BitsPerByte == PrefixLength)
            {
                return BaseAddress.Equals(address);
            }

            Span<byte> baseAddressBytes = stackalloc byte[expectedBytesCount];
            bool written = BaseAddress.TryWriteBytes(baseAddressBytes, out int bytesWritten);
            Debug.Assert(written && bytesWritten == expectedBytesCount);

            Span<byte> otherAddressBytes = stackalloc byte[expectedBytesCount];
            written = address.TryWriteBytes(otherAddressBytes, out bytesWritten);
            Debug.Assert(written && bytesWritten == expectedBytesCount);

            for (int processedBitsCount = 0, i = 0; processedBitsCount < PrefixLength; processedBitsCount += BitsPerByte, i++)
            {
                var baseAddressByte = baseAddressBytes[i];
                var otherAddressByte = otherAddressBytes[i];

                var rightShiftAmount = Math.Max(0, BitsPerByte - (PrefixLength - processedBitsCount));
                if (rightShiftAmount != 0)
                {
                    baseAddressByte >>= rightShiftAmount;
                    otherAddressByte >>= rightShiftAmount;
                }

                if (baseAddressByte == otherAddressByte)
                {
                    continue;
                }

                return false;
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
            const int MaxCharsCountAfterIpAddress = 4;
            const int MinCharsRequired = 4;

            if (s.Length < MinCharsRequired)
            {
                error = SR.dns_bad_ip_network;
                result = default;
                return false;
            }

            int separatorExpectedStartingIndex = s.Length - MaxCharsCountAfterIpAddress;
            int separatorIndex = s
                .Slice(separatorExpectedStartingIndex)
                .IndexOf(AddressAndPrefixLengthSeparator);
            if (separatorIndex != -1)
            {
                separatorIndex += separatorExpectedStartingIndex;

                var ipAddressSpan = s.Slice(0, separatorIndex);
                var prefixLengthSpan = s.Slice(separatorIndex + 1);

                if (IPAddress.TryParse(ipAddressSpan, out var ipAddress) && int.TryParse(prefixLengthSpan, out var prefixLength))
                {
                    result = new IPNetwork(ipAddress, prefixLength, validateAndThrow: false);

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

        private readonly struct ValidationError
        {
            public ValidationError(Func<Exception> constructorExceptionFactoryMethod, string parseExceptionMessage)
            {
                ConstructorExceptionFactoryMethod = constructorExceptionFactoryMethod;
                ParseExceptionMessage = parseExceptionMessage;
            }

            public readonly Func<Exception> ConstructorExceptionFactoryMethod;
            public readonly string ParseExceptionMessage;
        }

        private static readonly ValidationError s_baseAddressIsNullError = new ValidationError(() => new ArgumentNullException(BaseAddressConstructorParamName), string.Empty);
        private static readonly ValidationError s_baseAddressHasSetBitsInMaskError = new ValidationError(() => new ArgumentException(BaseAddressConstructorParamName), SR.dns_bad_ip_network);
        private static readonly ValidationError s_prefixLengthLessThanZeroError = new ValidationError(() => new ArgumentOutOfRangeException(PrefixLengthConstructorParamName), SR.dns_bad_ip_network);
        private static readonly ValidationError s_prefixLengthGreaterThanAllowedError = new ValidationError(() => new ArgumentOutOfRangeException(PrefixLengthConstructorParamName), SR.dns_bad_ip_network);

        private ValidationError? Validate()
        {
            if (BaseAddress == null)
            {
                return s_baseAddressIsNullError;
            }

            if (PrefixLength < 0)
            {
                return s_prefixLengthLessThanZeroError;
            }

            if (PrefixLength > GetAddressFamilyByteLength(BaseAddress.AddressFamily) * BitsPerByte)
            {
                return s_prefixLengthGreaterThanAllowedError;
            }

            if (IsAnyMaskBitOnForBaseAddress())
            {
                return s_baseAddressHasSetBitsInMaskError;
            }

            return default;
        }

        /// <summary>
        /// Method to determine whether any bit in <see cref="BaseAddress"/>'s variable/mask part is 1.
        /// </summary>
        private bool IsAnyMaskBitOnForBaseAddress()
        {
            Span<byte> addressBytes = stackalloc byte[GetAddressFamilyByteLength(BaseAddress.AddressFamily)];

            bool written = BaseAddress.TryWriteBytes(addressBytes, out int bytesWritten);
            Debug.Assert(written && bytesWritten == addressBytes.Length);

            var addressBitsCount = addressBytes.Length * BitsPerByte;

            for (int addressBytesIndex = addressBytes.Length - 1, numberOfEndingBitsToBeOff = addressBitsCount - PrefixLength;
                addressBytesIndex >= 0 && numberOfEndingBitsToBeOff > 0;
                addressBytesIndex--, numberOfEndingBitsToBeOff -= BitsPerByte)
            {
                byte maskForByte = unchecked((byte)(byte.MaxValue << Math.Min(numberOfEndingBitsToBeOff, BitsPerByte)));
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
            return $"{BaseAddress}{AddressAndPrefixLengthSeparator}{PrefixLength}";
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (!BaseAddress.TryFormat(destination, out charsWritten))
            {
                charsWritten = 0;
                return false;
            }

            if (destination.Length < charsWritten + 2)
            {
                return false;
            }

            destination[charsWritten++] = AddressAndPrefixLengthSeparator;

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
