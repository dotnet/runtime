// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    /// <summary>
    /// Provides an Internet Protocol (IP) subnet/range in CIDR notation.
    /// </summary>
    public readonly struct IPNetwork : IEquatable<IPNetwork>, ISpanFormattable, ISpanParsable<IPNetwork>
    {
        public IPAddress BaseAddress { get; }
        public int PrefixLength { get; }

        public IPNetwork(IPAddress baseAddress, int prefixLength)
        {
            ArgumentNullException.ThrowIfNull(baseAddress);
            Validate(baseAddress, prefixLength, throwOnFailure: true);

            BaseAddress = baseAddress;
            PrefixLength = prefixLength;
        }

        // Non-validating ctor
        private IPNetwork(IPAddress baseAddress, int prefixLength, bool _)
        {
            BaseAddress = baseAddress;
            PrefixLength = prefixLength;
        }

        public bool Contains(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.AddressFamily != BaseAddress.AddressFamily)
            {
                return false;
            }

            if (PrefixLength == 0)
            {
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                uint mask = uint.MaxValue << 32 - PrefixLength;

                if (BitConverter.IsLittleEndian)
                {
                    mask = BinaryPrimitives.ReverseEndianness(mask);
                }

                return BaseAddress.PrivateAddress == (address.PrivateAddress & mask);
            }
            else
            {
                Unsafe.SkipInit(out UInt128 baseAddressValue);
                Unsafe.SkipInit(out UInt128 otherAddressValue);

                BaseAddress.TryWriteBytes(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref baseAddressValue, 1)), out _);
                address.TryWriteBytes(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref otherAddressValue, 1)), out _);

                UInt128 mask = UInt128.MaxValue << 128 - PrefixLength;
                if (BitConverter.IsLittleEndian)
                {
                    mask = BinaryPrimitives.ReverseEndianness(mask);
                }

                return baseAddressValue == (otherAddressValue & mask);
            }
        }

        public static IPNetwork Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s.AsSpan());
        }
        public static IPNetwork Parse(ReadOnlySpan<char> s)
        {
            if (!TryParseCore(s, out IPAddress? address, out int prefixLength))
            {
                throw new FormatException(SR.net_bad_ip_network);
            }

            try
            {
                return new IPNetwork(address, prefixLength);
            }
            catch (Exception inner)
            {
                throw new FormatException(inner.Message, inner);
            }
        }

        public static bool TryParse(string? s, out IPNetwork result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, out IPNetwork result)
        {
            if (TryParseCore(s, out IPAddress? address, out int prefixLength) && Validate(address, prefixLength, throwOnFailure: false))
            {
                result = new IPNetwork(address, prefixLength, false);
                return true;
            }

            result = default;
            return false;
        }


        private static bool TryParseCore(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPAddress? address, out int prefixLength)
        {
            const int MaxCharsCountAfterIpAddress = 4;
            const int MinCharsRequired = 4;

            if (s.Length < MinCharsRequired)
            {
                goto Failure;
            }

            int separatorExpectedStartingIndex = s.Length - MaxCharsCountAfterIpAddress;
            int separatorIndex = s
                .Slice(separatorExpectedStartingIndex)
                .IndexOf('/');

            if (separatorIndex != -1)
            {
                separatorIndex += separatorExpectedStartingIndex;

                ReadOnlySpan<char> ipAddressSpan = s.Slice(0, separatorIndex);
                ReadOnlySpan<char> prefixLengthSpan = s.Slice(separatorIndex + 1);

                if (IPAddress.TryParse(ipAddressSpan, out address) && int.TryParse(prefixLengthSpan, out prefixLength))
                {
                    return true;
                }
            }

        Failure:
            address = default;
            prefixLength = default;
            return false;
        }

        private static bool Validate(IPAddress baseAddress, int prefixLength, bool throwOnFailure)
        {
            int maxPrefixLength = baseAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            if (prefixLength < 0 || prefixLength > maxPrefixLength)
            {
                if (throwOnFailure)
                {
                    ThrowArgumentOutOfRangeException();
                }

                return false;
            }

            if (HasNonZeroBitsAfterNetworkPrefix(baseAddress, prefixLength))
            {
                if (throwOnFailure)
                {
                    ThrowInvalidBaseAddressException();
                }

                return false;
            }

            return true;

            [DoesNotReturn]
            static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException(nameof(prefixLength));

            [DoesNotReturn]
            static void ThrowInvalidBaseAddressException() => throw new ArgumentException(SR.net_bad_ip_network_invalid_baseaddress, nameof(baseAddress));
        }

        private static bool HasNonZeroBitsAfterNetworkPrefix(IPAddress baseAddress, int prefixLength)
        {
            if (baseAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                uint mask = (uint)((long)uint.MaxValue << 32 - prefixLength);
                if (BitConverter.IsLittleEndian)
                {
                    mask = BinaryPrimitives.ReverseEndianness(mask);
                }

                return (baseAddress.PrivateAddress & mask) != baseAddress.PrivateAddress;
            }
            else
            {
                Unsafe.SkipInit(out UInt128 value);
                baseAddress.TryWriteBytes(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)), out _);
                if (prefixLength == 0)
                {
                    return value != UInt128.Zero;
                }

                UInt128 mask = UInt128.MaxValue << 128 - prefixLength;
                if (BitConverter.IsLittleEndian)
                {
                    mask = BinaryPrimitives.ReverseEndianness(mask);
                }

                return (value & mask) != value;
            }
        }


        public override string ToString()
        {
            return $"{BaseAddress}/{PrefixLength}";
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            return destination.TryWrite($"{BaseAddress}/{PrefixLength}", out charsWritten);
        }

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
    }
}
