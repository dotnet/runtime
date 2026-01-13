// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.IO.Hashing
{
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
        ///   Gets a value indicating whether the input value is treated as most significant bit (MSB) first, or last.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the MSB is the least significant bit of the last byte;
        ///   <see langword="false"/> if the MSB is the most significant bit of the first byte.
        /// </value>
        public bool ReflectInput { get; }

        /// <summary>Gets a value indicating whether the output CRC is reflected (reversed bit order) before applying the final XOR.</summary>
        /// <value><see langword="true"/> if the output CRC is reflected; otherwise, <see langword="false"/>.</value>
        public bool ReflectOutput { get; }

        private Crc32ParameterSet(uint polynomial, uint initialValue, uint finalXorValue, bool reflectInput, bool reflectOutput)
        {
            Polynomial = polynomial;
            InitialValue = initialValue;
            FinalXorValue = finalXorValue;
            ReflectInput = reflectInput;
            ReflectOutput = reflectOutput;
        }

        /// <summary>Creates a new <see cref="Crc32ParameterSet"/> with the specified parameters.</summary>
        /// <param name="polynomial">The polynomial value used for the CRC calculation.</param>
        /// <param name="initialValue">The initial value (seed) for the CRC calculation.</param>
        /// <param name="finalXorValue">The value to XOR with the final CRC result.</param>
        /// <param name="reflectInput">Whether the input bytes are reflected (reversed bit order) before processing.</param>
        /// <param name="reflectOutput">Whether the output CRC is reflected (reversed bit order) before applying the final XOR.</param>
        /// <returns>A new <see cref="Crc32ParameterSet"/> instance.</returns>
        [CLSCompliant(false)]
        public static Crc32ParameterSet Create(
            uint polynomial,
            uint initialValue,
            uint finalXorValue,
            bool reflectInput,
            bool reflectOutput)
        {
            Crc32ParameterSet set = reflectInput switch
            {
                false => new ForwardTableBasedCrc32(polynomial, initialValue, finalXorValue, reflectOutput),
                _ => new ReflectedTableBasedCrc32(polynomial, initialValue, finalXorValue, reflectOutput),
            };

            return set;
        }

        internal void WriteCrcToSpan(uint crc, Span<byte> destination)
        {
            if (ReflectOutput)
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

            if (ReflectOutput != ReflectInput)
            {
                crc = ReverseBits(crc);
            }

            return crc ^ FinalXorValue;
        }

        private static uint ReverseBits(uint value)
        {
            value = ((value & 0xAAAAAAAA) >> 1) | ((value & 0x55555555) << 1);
            value = ((value & 0xCCCCCCCC) >> 2) | ((value & 0x33333333) << 2);
            value = ((value & 0xF0F0F0F0) >> 4) | ((value & 0x0F0F0F0F) << 4);
            value = ((value & 0xFF00FF00) >> 8) | ((value & 0x00FF00FF) << 8);
            return (value >> 16) | (value << 16);
        }
    }
}
