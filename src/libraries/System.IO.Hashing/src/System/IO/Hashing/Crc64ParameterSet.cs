// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Represents a set of parameters that define the behavior of a CRC-64 hash algorithm.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The parameter-set instance precomputes values to be used in the CRC calculation.
    ///     As such, callers are expected to create a single instance of the parameter set and
    ///     reuse it for multiple hash calculations.
    ///   </para>
    /// </remarks>
    public partial class Crc64ParameterSet
    {
        /// <summary>Gets the polynomial value used for the CRC calculation.</summary>
        /// <value>The polynomial value used for the CRC calculation.</value>
        [CLSCompliant(false)]
        public ulong Polynomial { get; }

        /// <summary>Gets the initial value (seed) for the CRC calculation.</summary>
        /// <value>The initial value (seed) for the CRC calculation.</value>
        [CLSCompliant(false)]
        public ulong InitialValue { get; }

        /// <summary>Gets the value to XOR with the final CRC result.</summary>
        /// <value>The value to XOR with the final CRC result.</value>
        /// <remarks>For reflected-output CRC values, the final XOR is done after the bit-reflection.</remarks>
        [CLSCompliant(false)]
        public ulong FinalXorValue { get; }

        /// <summary>
        ///   Gets a value indicating whether the input and output bytes are most-significant-bit (MSB) first, or last.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the MSB is the least significant bit of the last byte;
        ///   <see langword="false"/> if the MSB is the most significant bit of the first byte.
        /// </value>
        public bool ReflectValues { get; }

        private Crc64ParameterSet(ulong polynomial, ulong initialValue, ulong finalXorValue, bool reflectValues)
        {
            Polynomial = polynomial;
            InitialValue = initialValue;
            FinalXorValue = finalXorValue;
            ReflectValues = reflectValues;
        }

        /// <summary>Creates a new <see cref="Crc64ParameterSet"/> with the specified parameters.</summary>
        /// <param name="polynomial">The polynomial value used for the CRC calculation.</param>
        /// <param name="initialValue">The initial value (seed) for the CRC calculation.</param>
        /// <param name="finalXorValue">The value to XOR with the final CRC result.</param>
        /// <param name="reflectValues">
        ///   <see langword="true"/> if the input values are least-significant-bit (LSB) first;
        ///   <see langword="false"/> if the input values are most-significant-bit (MSB) first.
        /// </param>
        /// <returns>A new <see cref="Crc64ParameterSet"/> instance.</returns>
        [CLSCompliant(false)]
        public static Crc64ParameterSet Create(
            ulong polynomial,
            ulong initialValue,
            ulong finalXorValue,
            bool reflectValues)
        {
            return reflectValues ?
                new ReflectedTableBasedCrc64(polynomial, initialValue, finalXorValue) :
                new ForwardTableBasedCrc64(polynomial, initialValue, finalXorValue);
        }

        internal void WriteCrcToSpan(ulong crc, Span<byte> destination)
        {
            if (ReflectValues)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(destination, crc);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(destination, crc);
            }
        }

        internal virtual ulong Update(ulong value, ReadOnlySpan<byte> source) =>
            throw new NotImplementedException();

        internal ulong Finalize(ulong value)
        {
            ulong crc = value;

            // If, in the future, refIn!=refOut is supported, then the
            // answer should (probably) be bit-reversed here before the final XOR.

            return crc ^ FinalXorValue;
        }

        private static ulong ReverseBits(ulong value)
        {
#if NET
            if (System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported)
            {
                return System.Runtime.Intrinsics.Arm.ArmBase.Arm64.ReverseElementBits(value);
            }
#endif

            value = ((value & 0xAAAAAAAAAAAAAAAA) >> 1) | ((value & 0x5555555555555555) << 1);
            value = ((value & 0xCCCCCCCCCCCCCCCC) >> 2) | ((value & 0x3333333333333333) << 2);
            value = ((value & 0xF0F0F0F0F0F0F0F0) >> 4) | ((value & 0x0F0F0F0F0F0F0F0F) << 4);

            return BinaryPrimitives.ReverseEndianness(value);
        }

        private abstract partial class ForwardCrc64 : Crc64ParameterSet
        {
            private readonly bool _canVectorize;

            partial void InitializeVectorized(ref bool canVectorize);
            partial void UpdateVectorized(ref ulong crc, ReadOnlySpan<byte> source, ref int bytesConsumed);

            protected ForwardCrc64(ulong polynomial, ulong initialValue, ulong finalXorValue)
                : base(polynomial, initialValue, finalXorValue, reflectValues: false)
            {
                InitializeVectorized(ref _canVectorize);
            }

            protected abstract ulong UpdateScalar(ulong value, ReadOnlySpan<byte> source);

            internal sealed override ulong Update(ulong value, ReadOnlySpan<byte> source)
            {
                if (_canVectorize)
                {
                    int consumed = 0;
                    UpdateVectorized(ref value, source, ref consumed);

                    if (consumed == source.Length)
                    {
                        return value;
                    }

                    source = source.Slice(consumed);
                }

                return UpdateScalar(value, source);
            }
        }
    }
}
