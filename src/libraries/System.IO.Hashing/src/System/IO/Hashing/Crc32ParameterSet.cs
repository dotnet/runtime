// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Represents a set of parameters that define the behavior of a CRC-32 hash algorithm.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The parameter-set instance precomputes values to be used in the CRC calculation.
    ///     As such, callers are expected to create a single instance of the parameter set and
    ///     reuse it for multiple hash calculations.
    ///   </para>
    /// </remarks>
    public partial class Crc32ParameterSet
    {
        /// <summary>Gets the polynomial value used for the CRC calculation.</summary>
        /// <value>The polynomial value used for the CRC calculation.</value>
        [CLSCompliant(false)]
        public uint Polynomial { get; }

        /// <summary>Gets the initial value (seed) for the CRC calculation.</summary>
        /// <value>The initial value (seed) for the CRC calculation.</value>
        [CLSCompliant(false)]
        public uint InitialValue { get; }

        /// <summary>Gets the value to XOR with the final CRC result.</summary>
        /// <value>The value to XOR with the final CRC result.</value>
        /// <remarks>For reflected-output CRC values, the final XOR is done after the bit-reflection.</remarks>
        [CLSCompliant(false)]
        public uint FinalXorValue { get; }

        /// <summary>
        ///   Gets a value indicating whether the input and output bytes are most-significant-bit (MSB) first, or last.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the MSB is the least significant bit of the last byte;
        ///   <see langword="false"/> if the MSB is the most significant bit of the first byte.
        /// </value>
        public bool ReflectValues { get; }

        private Crc32ParameterSet(uint polynomial, uint initialValue, uint finalXorValue, bool reflectValues)
        {
            Polynomial = polynomial;
            InitialValue = initialValue;
            FinalXorValue = finalXorValue;
            ReflectValues = reflectValues;
        }

        /// <summary>Creates a new <see cref="Crc32ParameterSet"/> with the specified parameters.</summary>
        /// <param name="polynomial">The polynomial value used for the CRC calculation.</param>
        /// <param name="initialValue">The initial value (seed) for the CRC calculation.</param>
        /// <param name="finalXorValue">The value to XOR with the final CRC result.</param>
        /// <param name="reflectValues">
        ///   <see langword="true"/> if the input values are least-significant-bit (LSB) first;
        ///   <see langword="false"/> if the input values are most-significant-bit (MSB) first.
        /// </param>
        /// <returns>A new <see cref="Crc32ParameterSet"/> instance.</returns>
        [CLSCompliant(false)]
        public static Crc32ParameterSet Create(
            uint polynomial,
            uint initialValue,
            uint finalXorValue,
            bool reflectValues)
        {
            return reflectValues ?
                new ReflectedTableBasedCrc32(polynomial, initialValue, finalXorValue) :
                new ForwardTableBasedCrc32(polynomial, initialValue, finalXorValue);
        }

        internal void WriteCrcToSpan(uint crc, Span<byte> destination)
        {
            if (ReflectValues)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(destination, crc);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(destination, crc);
            }
        }

        internal virtual uint Update(uint value, ReadOnlySpan<byte> source) =>
            throw new NotImplementedException();

        internal uint Finalize(uint value)
        {
            uint crc = value;

            // If, in the future, refIn!=refOut is supported, then the
            // answer should (probably) be bit-reversed here before the final XOR.

            return crc ^ FinalXorValue;
        }

        private static uint ReverseBits(uint value)
        {
#if NET
            if (System.Runtime.Intrinsics.Arm.ArmBase.IsSupported)
            {
                return System.Runtime.Intrinsics.Arm.ArmBase.ReverseElementBits(value);
            }
#endif

            value = ((value & 0xAAAAAAAA) >> 1) | ((value & 0x55555555) << 1);
            value = ((value & 0xCCCCCCCC) >> 2) | ((value & 0x33333333) << 2);
            value = ((value & 0xF0F0F0F0) >> 4) | ((value & 0x0F0F0F0F) << 4);

            return BinaryPrimitives.ReverseEndianness(value);
        }
    }
}
