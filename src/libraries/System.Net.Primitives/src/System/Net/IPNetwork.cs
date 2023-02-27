// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Net
{
    /// <devdoc>
    ///   <para>
    ///     Provides an Internet Protocol (IP) subnet/range in CIDR notation.
    ///   </para>
    /// </devdoc>
    public readonly struct IPNetwork : IEquatable<IPNetwork>, ISpanFormattable, ISpanParsable<IPNetwork>
    {
        public IPAddress BaseAddress { get; private init; }
        public int PrefixLength { get; private init; }

        private const int bitsPerByte = 8;
        private const string
            baseAddressParamName = "baseAddress",
            prefixLengthParamName = "prefixLength";
        public IPNetwork(IPAddress baseAddress, int prefixLength)
        {
            BaseAddress = baseAddress;
            PrefixLength = prefixLength;

            var validationError = Validate();
            if (validationError.HasValue)
            {
                throw validationError.Value.CtorExceptionFactoryMethod();
            }
        }

        public bool Contains(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.AddressFamily != BaseAddress.AddressFamily)
            {
                return false;
            }

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
                    var shiftAmount = bitsPerByte - PrefixLength + processedBitsCount;
                    baseAddressByte >>= shiftAmount;
                    otherAddressByte >>= shiftAmount;
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
            if(TryParseInternal(s, out var result, out var error))
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

        #region Private and Internal methods
        internal static bool TryParseInternal(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out IPNetwork result, [NotNullWhen(false)] out string? error)
        {
            int slashExpectedStartingIndex = s.Length - 4;
            if (s.Slice(slashExpectedStartingIndex).IndexOf('/') != -1)
            {
                var ipAddressSpan = s.Slice(0, slashExpectedStartingIndex);
                var prefixLengthSpan = s.Slice(slashExpectedStartingIndex + 1);

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
            Func<Exception> CtorExceptionFactoryMethod,
            string ParseExceptionMessage);

        private static readonly ValidationError
            _baseAddressIsNullError = new ValidationError(() => new ArgumentNullException(baseAddressParamName), string.Empty),
            _baseAddressHasSetBitsInMaskError = new ValidationError(() => new ArgumentException(baseAddressParamName), SR.dns_bad_ip_network),
            _prefixLengthLessThanZeroError = new ValidationError(() => new ArgumentOutOfRangeException(prefixLengthParamName), SR.dns_bad_ip_network),
            _prefixLengthGreaterThanAllowedError = new ValidationError(() => new ArgumentOutOfRangeException(prefixLengthParamName), SR.dns_bad_ip_network);

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

            if (!AreMaskBitsAfterPrefixUnset())
            {
                return _baseAddressHasSetBitsInMaskError;
            }

            return default;
        }

        private bool AreMaskBitsAfterPrefixUnset()
        {
            Span<byte> addressBytes = stackalloc byte[GetAddressFamilyByteLength(BaseAddress.AddressFamily)];
            if (!BaseAddress.TryWriteBytes(addressBytes, out int bytesWritten))
            {
                throw new UnreachableException();
            }

            for (int bitsCount = bytesWritten * bitsPerByte, i = bytesWritten - 1; bitsCount >= 0; bitsCount -= bitsPerByte, i--)
            {
                var numberOfEndingBitsToBeUnset = bitsCount - PrefixLength;
                byte segment = addressBytes[i];
                if (numberOfEndingBitsToBeUnset > bitsPerByte)
                {
                    if (segment == 0)
                        continue;

                    return false;
                }

                if ((segment & (1 << numberOfEndingBitsToBeUnset)) == segment)
                {
                    return true;
                }

                return false;
            }

            throw new UnreachableException();
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
        static IPNetwork IParsable<IPNetwork>.Parse(string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s);
        }
        static bool IParsable<IPNetwork>.TryParse(string? s, IFormatProvider? provider, out IPNetwork result)
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
