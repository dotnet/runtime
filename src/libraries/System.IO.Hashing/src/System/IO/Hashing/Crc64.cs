// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Provides an implementation of the CRC-64 algorithm.
    ///   By default, this implementation uses the ECMA-182 parameter set,
    ///   but other parameter sets can also be specified.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     For methods that return byte arrays or that write into spans of bytes, this implementation
    ///     emits the answer in the byte order that maintains the CRC residue relationship
    ///     (CRC(message concat CRC(message)) is a fixed value).
    ///     For CRC-64 as described in ECMA-182, Annex B, this stable output is the byte sequence
    ///     <c>{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }</c>,
    ///     the Big Endian representation of <c>0x0000000000000000</c>.
    ///   </para>
    ///   <para>
    ///     There are multiple, incompatible, definitions of a 64-bit cyclic redundancy
    ///     check (CRC) algorithm. When interoperating with another system, ensure that you
    ///     are using the same definition. The definition used by this implementation is not
    ///     compatible with the cyclic redundancy check described in ISO 3309.
    ///   </para>
    /// </remarks>
    public sealed class Crc64 : NonCryptographicHashAlgorithm
    {
        private const int Size = sizeof(ulong);

        private ulong _crc;

        /// <summary>
        ///   Gets the parameter set used by this instance.
        /// </summary>
        /// <value>
        ///   The parameter set used by this instance.
        /// </value>
        public Crc64ParameterSet ParameterSet { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Crc64"/> class using the ECMA-182 parameters.
        /// </summary>
        public Crc64()
            : base(Size)
        {
            ParameterSet = Crc64ParameterSet.Crc64;
            _crc = ParameterSet.InitialValue;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Crc64"/> class using the specified parameters.
        /// </summary>
        /// <param name="parameterSet">
        ///   The parameters to use for the CRC computation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> is <see langword="null"/>.
        /// </exception>
        public Crc64(Crc64ParameterSet parameterSet)
            : base(Size)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);

            ParameterSet = parameterSet;
            _crc = parameterSet.InitialValue;
        }

        /// <summary>Initializes a new instance of the <see cref="Crc64"/> class using the state from another instance.</summary>
        private Crc64(ulong crc, Crc64ParameterSet parameterSet) : base(Size)
        {
            _crc = crc;
            ParameterSet = parameterSet;
        }

        /// <summary>Returns a clone of the current instance, with a copy of the current instance's internal state.</summary>
        /// <returns>A new instance that will produce the same sequence of values as the current instance.</returns>
        public Crc64 Clone() => new(_crc, ParameterSet);

        /// <summary>
        ///   Appends the contents of <paramref name="source"/> to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
        {
            _crc = ParameterSet.Update(_crc, source);
        }

        /// <summary>
        ///   Resets the hash computation to the initial state.
        /// </summary>
        public override void Reset()
        {
            _crc = ParameterSet.InitialValue;
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        protected override void GetCurrentHashCore(Span<byte> destination)
        {
            ParameterSet.WriteCrcToSpan(ParameterSet.Finalize(_crc), destination);
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   then clears the accumulated state.
        /// </summary>
        protected override void GetHashAndResetCore(Span<byte> destination)
        {
            ParameterSet.WriteCrcToSpan(ParameterSet.Finalize(_crc), destination);
            _crc = ParameterSet.InitialValue;
        }

        /// <summary>Gets the current computed hash value without modifying accumulated state.</summary>
        /// <returns>The hash value for the data already provided.</returns>
        [CLSCompliant(false)]
        public ulong GetCurrentHashAsUInt64() => ParameterSet.Finalize(_crc);

        /// <summary>
        ///   Computes the CRC-64 hash of the provided data, using the ECMA-182 parameters.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The CRC-64 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return Hash(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Computes the CRC-64 hash of the provided data, using the ECMA-182 parameters.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The CRC-64 hash of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source) =>
            HashCore(Crc64ParameterSet.Crc64, source);

        /// <summary>
        ///   Computes the CRC-64 hash value for the provided data using the specified parameter set.
        /// </summary>
        /// <param name="parameterSet">The parameters to use for the CRC computation.</param>
        /// <param name="source">The data to hash.</param>
        /// <returns>The CRC-64 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> or <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(Crc64ParameterSet parameterSet, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);
            ArgumentNullException.ThrowIfNull(source);

            return Hash(parameterSet, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Computes the CRC-64 hash value for the provided data using the specified parameter set.
        /// </summary>
        /// <param name="parameterSet">The parameters to use for the CRC computation.</param>
        /// <param name="source">The data to hash.</param>
        /// <returns>The CRC-64 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(Crc64ParameterSet parameterSet, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);

            return HashCore(parameterSet, source);
        }

        private static byte[] HashCore(Crc64ParameterSet parameterSet, ReadOnlySpan<byte> source)
        {
            byte[] ret = new byte[Size];
            ulong hash = HashToUInt64(parameterSet, source);
            parameterSet.WriteCrcToSpan(hash, ret);
            return ret;
        }

        /// <summary>
        ///   Attempts to compute the CRC-64 hash of the provided data, using the ECMA-182 parameters,
        ///   into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        ///   the computed hash value (8 bytes); otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            TryHashCore(Crc64ParameterSet.Crc64, source, destination, out bytesWritten);

        /// <summary>
        ///   Attempts to compute the CRC-64 hash of the provided data, using the specified parameter set,
        ///   into the provided destination.
        /// </summary>
        /// <param name="parameterSet">The parameters to use for the CRC computation.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        ///   the computed hash value (8 bytes); otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryHash(
            Crc64ParameterSet parameterSet,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);

            return TryHashCore(parameterSet, source, destination, out bytesWritten);
        }

        private static bool TryHashCore(
            Crc64ParameterSet parameterSet,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (destination.Length < Size)
            {
                bytesWritten = 0;
                return false;
            }

            ulong hash = HashToUInt64(parameterSet, source);
            parameterSet.WriteCrcToSpan(hash, destination);
            bytesWritten = Size;
            return true;
        }

        /// <summary>
        ///   Computes the CRC-64 hash of the provided data, using the ECMA-182 parameters,
        ///   into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination) =>
            HashCore(Crc64ParameterSet.Crc64, source, destination);

        /// <summary>
        ///   Computes the CRC-64 hash of the provided data, using the specified parameters,
        ///   into the provided destination.
        /// </summary>
        /// <param name="parameterSet">The parameters to use for the CRC computation.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> is <see langword="null"/>.
        /// </exception>
        public static int Hash(Crc64ParameterSet parameterSet, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);

            return HashCore(parameterSet, source, destination);
        }

        private static int HashCore(Crc64ParameterSet parameterSet, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < Size)
            {
                ThrowDestinationTooShort();
            }

            ulong hash = HashToUInt64(parameterSet, source);
            parameterSet.WriteCrcToSpan(hash, destination);
            return Size;
        }

        /// <summary>Computes the CRC-64 hash of the provided data, using the ECMA-182 parameters.</summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The computed CRC-64 hash.</returns>
        [CLSCompliant(false)]
        public static ulong HashToUInt64(ReadOnlySpan<byte> source)
        {
            // Rather than go through Crc64ParameterSet.Crc64 to end up in the optimized Update method here,
            // just call the Update method directly.
            // ECMA-182 uses a final XOR of zero, so directly return the result.
            Crc64ParameterSet parameterSet = Crc64ParameterSet.Crc64;
            return parameterSet.Update(parameterSet.InitialValue, source);
        }

        /// <summary>Computes the CRC-64 hash of the provided data, using the specified parameters.</summary>
        /// <param name="parameterSet">The parameters to use for the CRC computation.</param>
        /// <param name="source">The data to hash.</param>
        /// <returns>The computed CRC-64 hash.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameterSet"/> is <see langword="null"/>.
        /// </exception>
        [CLSCompliant(false)]
        public static ulong HashToUInt64(Crc64ParameterSet parameterSet, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(parameterSet);

            ulong crc = parameterSet.Update(parameterSet.InitialValue, source);
            return parameterSet.Finalize(crc);
        }
    }
}
