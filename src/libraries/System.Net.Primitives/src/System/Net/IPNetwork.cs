// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1648 // TODO: https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3595

namespace System.Net
{
    /// <summary>
    /// Represents an IP network in CIDR notation with a <see cref="IPAddress"/> and a prefix length.
    /// </summary>
    /// <remarks>
    /// This type disallows arbitrary IP/prefixLength CIDR pairs. <see cref="BaseAddress"/> must be defined so that all bits after the network prefix are set to zero.
    /// In other words, <see cref="BaseAddress"/> is always the first usable address of the network.
    /// The constructor and the parsing methods will throw in case there are non-zero bits after the prefix.
    /// </remarks>
    public readonly struct IPNetwork : IEquatable<IPNetwork>, ISpanFormattable, ISpanParsable<IPNetwork>
    {
        /// <summary>
        /// Gets the <see cref="IPAddress"/> that represents the prefix of the network.
        /// </summary>
        public IPAddress BaseAddress { get; }

        /// <summary>
        /// Gets the length of the network prefix in bits.
        /// </summary>
        public int PrefixLength { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetwork"/> class with the specified <see cref="IPAddress"/> and prefix length.
        /// </summary>
        /// <param name="baseAddress">The <see cref="IPAddress"/> that represents the prefix of the network.</param>
        /// <param name="prefixLength">The length of the prefix in bits.</param>
        /// <exception cref="ArgumentNullException">The specified <paramref name="baseAddress"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="prefixLength"/> is smaller than `0` or longer than maximum length of <paramref name="prefixLength"/>'s <see cref="AddressFamily"/>.</exception>
        /// <exception cref="ArgumentException">The specified <paramref name="baseAddress"/> has non-zero bits after the network prefix.</exception>
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

        /// <summary>
        /// Determines whether a given <see cref="IPAddress"/> is part of the network.
        /// </summary>
        /// <param name="address">The <see cref="IPAddress"/> to check.</param>
        /// <returns><see langword="true"/> if the <see cref="IPAddress"/> is part of the network; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified <paramref name="address"/> is <see langword="null"/>.</exception>
        public bool Contains(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (BaseAddress == null)
            {
                throw new InvalidOperationException(SR.net_uninitialized_ip_network_instance);
            }

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

        /// <summary>
        /// Converts a CIDR <see cref="string"/> to an <see cref="IPNetwork"/> instance.
        /// </summary>
        /// <param name="s">A <see cref="string"/> that defines an IP network in CIDR notation.</param>
        /// <returns>An <see cref="IPNetwork"/> instance.</returns>
        /// <exception cref="ArgumentNullException">The specified string is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not a valid CIDR network string, or the address contains non-zero bits after the network prefix.</exception>
        public static IPNetwork Parse(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s.AsSpan());
        }

        /// <summary>
        /// Converts a CIDR character span to an <see cref="IPNetwork"/> instance.
        /// </summary>
        /// <param name="s">A character span that defines an IP network in CIDR notation.</param>
        /// <returns>An <see cref="IPNetwork"/> instance.</returns>
        /// <exception cref="FormatException"><paramref name="s"/> is not a valid CIDR network string, or the address contains non-zero bits after the network prefix.</exception>
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

        /// <summary>
        /// Converts the specified CIDR string to an <see cref="IPNetwork"/> instance and returns a value indicating whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A <see cref="string"/> that defines an IP network in CIDR notation.</param>
        /// <param name="result">When the method returns, contains an <see cref="IPNetwork"/> instance if the conversion succeeds.</param>
        /// <returns><see langword="true"/> if the conversion was succesful; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, out IPNetwork result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), out result);
        }

        /// <summary>
        /// Converts the specified CIDR character span to an <see cref="IPNetwork"/> instance and returns a value indicating whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A <see cref="string"/> that defines an IP network in CIDR notation.</param>
        /// <param name="result">When the method returns, contains an <see cref="IPNetwork"/> instance if the conversion succeeds.</param>
        /// <returns><see langword="true"/> if the conversion was succesful; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Converts the instance to a string containing the <see cref="IPNetwork"/>'s CIDR notation.
        /// </summary>
        /// <returns>The <see cref="string"/> containing the <see cref="IPNetwork"/>'s CIDR notation.</returns>
        public override string ToString()
        {
            return $"{BaseAddress}/{PrefixLength}";
        }

        /// <summary>
        /// Attempts to write the <see cref="IPNetwork"/>'s CIDR notation to the given <paramref name="destination"/> span and returns a value indicating whether the operation succeeded.
        /// </summary>
        /// <param name="destination">The destination span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written to <paramref name="destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was succesful; otherwise <see langword="false"/>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            return destination.TryWrite($"{BaseAddress}/{PrefixLength}", out charsWritten);
        }

        /// <summary>
        /// Determines whether two <see cref="IPNetwork"/> instances are equal.
        /// </summary>
        /// <param name="other">The <see cref="IPNetwork"/> instance to compare to this instance.</param>
        /// <returns><see langword="true"/> if the networks are equal; otherwise <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Uninitialized <see cref="IPNetwork"/> instance.</exception>
        public bool Equals(IPNetwork other)
        {
            if (BaseAddress == null || other.BaseAddress == null)
            {
                throw new InvalidOperationException(SR.net_uninitialized_ip_network_instance);
            }

            return BaseAddress.Equals(other.BaseAddress) && PrefixLength == other.PrefixLength;
        }

        /// <summary>
        /// Determines whether two <see cref="IPNetwork"/> instances are equal.
        /// </summary>
        /// <param name="obj">The <see cref="IPNetwork"/> instance to compare to this instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is an <see cref="IPNetwork"/> instance and the networks are equal; otherwise <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Uninitialized <see cref="IPNetwork"/> instance.</exception>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not IPNetwork other)
            {
                return false;
            }

            return Equals(other);
        }

        /// <summary>
        /// Determines whether the specified instances of <see cref="IPNetwork"/> are equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns><see langword="true"/> if the networks are equal; otherwise <see langword="false"/>.</returns>
        public static bool operator ==(IPNetwork left, IPNetwork right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether the specified instances of <see cref="IPNetwork"/> are not equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns><see langword="true"/> if the networks are not equal; otherwise <see langword="false"/>.</returns>
        public static bool operator !=(IPNetwork left, IPNetwork right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>An integer hash value.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(BaseAddress, PrefixLength);
        }

        /// <inheritdoc />
        string IFormattable.ToString(string? format, IFormatProvider? provider)
        {
            return ToString();
        }

        /// <inheritdoc />
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        static IPNetwork IParsable<IPNetwork>.Parse([NotNull] string s, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s);
        }

        /// <inheritdoc />
        static bool IParsable<IPNetwork>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out IPNetwork result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        static IPNetwork ISpanParsable<IPNetwork>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        static bool ISpanParsable<IPNetwork>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out IPNetwork result)
        {
            return TryParse(s, out result);
        }
    }
}
