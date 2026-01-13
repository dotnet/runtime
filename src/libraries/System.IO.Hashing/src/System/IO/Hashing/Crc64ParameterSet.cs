// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.IO.Hashing
{
    public abstract partial class Crc64ParameterSet
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

        /// <summary>Gets a value indicating whether the input bytes are reflected (reversed bit order) before processing.</summary>
        /// <value><see langword="true"/> if the input bytes are reflected; otherwise, <see langword="false"/>.</value>
        public bool ReflectInput { get; }

        /// <summary>Gets a value indicating whether the output CRC is reflected (reversed bit order) before applying the final XOR.</summary>
        /// <value><see langword="true"/> if the output CRC is reflected; otherwise, <see langword="false"/>.</value>
        public bool ReflectOutput { get; }

        private Crc64ParameterSet(ulong polynomial, ulong initialValue, ulong finalXorValue, bool reflectInput, bool reflectOutput)
        {
            Polynomial = polynomial;
            InitialValue = initialValue;
            FinalXorValue = finalXorValue;
            ReflectInput = reflectInput;
            ReflectOutput = reflectOutput;
        }

        /// <summary>Creates a new <see cref="Crc64ParameterSet"/> with the specified parameters.</summary>
        /// <param name="polynomial">The polynomial value used for the CRC calculation.</param>
        /// <param name="initialValue">The initial value (seed) for the CRC calculation.</param>
        /// <param name="finalXorValue">The value to XOR with the final CRC result.</param>
        /// <param name="reflectInput">Whether the input bytes are reflected (reversed bit order) before processing.</param>
        /// <param name="reflectOutput">Whether the output CRC is reflected (reversed bit order) before applying the final XOR.</param>
        /// <returns>A new <see cref="Crc64ParameterSet"/> instance.</returns>
        [CLSCompliant(false)]
        public static Crc64ParameterSet Create(
            ulong polynomial,
            ulong initialValue,
            ulong finalXorValue,
            bool reflectInput,
            bool reflectOutput)
        {
            Crc64ParameterSet set = reflectInput switch
            {
                false => new ForwardTableBasedCrc64(polynomial, initialValue, finalXorValue, reflectOutput),
                _ => new ReflectedTableBasedCrc64(polynomial, initialValue, finalXorValue, reflectOutput),
            };

            return set;
        }

        internal void WriteCrcToSpan(ulong crc, Span<byte> destination)
        {
            if (ReflectOutput)
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

            if (ReflectOutput != ReflectInput)
            {
                crc = ReverseBits(crc);
            }

            return crc ^ FinalXorValue;
        }

        private static ulong ReverseBits(ulong value)
        {
            value = ((value & 0xAAAAAAAAAAAAAAAA) >> 1) | ((value & 0x5555555555555555) << 1);
            value = ((value & 0xCCCCCCCCCCCCCCCC) >> 2) | ((value & 0x3333333333333333) << 2);
            value = ((value & 0xF0F0F0F0F0F0F0F0) >> 4) | ((value & 0x0F0F0F0F0F0F0F0F) << 4);
            value = ((value & 0xFF00FF00FF00FF00) >> 8) | ((value & 0x00FF00FF00FF00FF) << 8);
            value = ((value & 0xFFFF0000FFFF0000) >> 16) | ((value & 0x0000FFFF0000FFFF) << 16);
            return (value >> 32) | (value << 32);
        }
    }
}
