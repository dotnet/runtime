// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace System.IO.Hashing
{
    /// <summary>
    ///   Provides an implementation of the Adler-32 checksum algorithm, as specified in
    ///   <see href="https://www.rfc-editor.org/rfc/rfc1950">RFC 1950</see>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm produces a 32-bit checksum and is commonly used in
    ///     data compression formats such as zlib. It is not suitable for cryptographic purposes.
    ///   </para>
    /// </remarks>
    public sealed partial class Adler32 : NonCryptographicHashAlgorithm
    {
        private const uint InitialState = 1u;
        private const int Size = sizeof(uint);
        private uint _adler = InitialState;

        /// <summary>Largest prime smaller than 65536.</summary>
        private const uint ModBase = 65521;
        /// <summary>NMax is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) &lt;= 2^32-1</summary>
        private const int NMax = 5552;

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32"/> class.
        /// </summary>
        public Adler32()
            : base(Size)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32"/> class using the state from another instance.
        /// </summary>
        private Adler32(uint adler)
            : base(Size)
            => _adler = adler;

        /// <summary>
        /// Returns a clone of the current instance, with a copy of the current instance's internal state.
        /// </summary>
        /// <returns>
        /// A new instance that will produce the same sequence of values as the current instance.
        /// </returns>
        public Adler32 Clone()
            => new(_adler);

        /// <summary>
        /// Appends the contents of <paramref name="source"/> to the data already
        /// processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
            => _adler = Update(_adler, source);

        /// <summary>
        /// Resets the hash computation to the initial state.
        /// </summary>
        public override void Reset()
            => _adler = InitialState;

        /// <summary>
        /// Writes the computed hash value to <paramref name="destination"/>
        /// without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        protected override void GetCurrentHashCore(Span<byte> destination)
            => BinaryPrimitives.WriteUInt32BigEndian(destination, _adler);

        /// <summary>
        /// Writes the computed hash value to <paramref name="destination"/>
        /// then clears the accumulated state.
        /// </summary>
        protected override void GetHashAndResetCore(Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, _adler);
            _adler = InitialState;
        }

        /// <summary>
        /// Gets the current computed hash value without modifying accumulated state.
        /// </summary>
        /// <returns>
        /// The hash value for the data already provided.
        /// </returns>
        [CLSCompliant(false)]
        public uint GetCurrentHashAsUInt32()
            => _adler;

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The Adler-32 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return Hash(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The Adler-32 hash of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source)
        {
            byte[] ret = new byte[Size];
            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(ret, hash);
            return ret;
        }

        /// <summary>
        /// Attempts to compute the Adler-32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        /// On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        /// the computed hash value (4 bytes); otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < Size)
            {
                bytesWritten = 0;
                return false;
            }

            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(destination, hash);
            bytesWritten = Size;
            return true;
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < Size)
            {
                ThrowDestinationTooShort();
            }

            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(destination, hash);
            return Size;
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>
        /// The computed Adler-32 hash.
        /// </returns>
        [CLSCompliant(false)]
        public static uint HashToUInt32(ReadOnlySpan<byte> source)
            => Update(InitialState, source);

        private static uint Update(uint adler, ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
            {
                return adler;
            }

#if NET
            if (BitConverter.IsLittleEndian &&
                Vector128.IsHardwareAccelerated &&
                source.Length >= Vector128<byte>.Count * 2)
            {
                if (AdvSimd.IsSupported)
                {
                    return UpdateArm128(adler, source);
                }

                if (Avx512BW.IsSupported && source.Length >= Vector512<byte>.Count)
                {
                    return UpdateVector512(adler, source);
                }

                if (Avx2.IsSupported && source.Length >= Vector256<byte>.Count)
                {
                    return UpdateVector256(adler, source);
                }

                return UpdateVector128(adler, source);
            }
#endif

            return UpdateScalar(adler, source);
        }

        private static uint UpdateScalar(uint adler, ReadOnlySpan<byte> source)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;
            Debug.Assert(!source.IsEmpty);

            do
            {
                int k = source.Length < NMax ? source.Length : NMax;
                foreach (byte b in source.Slice(0, k))
                {
                    s1 += b;
                    s2 += s1;
                }

                s1 %= ModBase;
                s2 %= ModBase;
                source = source.Slice(k);
            }
            while (source.Length > 0);

            return (s2 << 16) | s1;
        }

#if NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateVector128(uint adler, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length >= Vector128<byte>.Count * 2);

            const int BlockSize = 32; // two Vector128<byte> loads

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector128<sbyte> tap1 = Vector128.Create(32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17);
            Vector128<sbyte> tap2 = Vector128.Create(16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            do
            {
                int n = Math.Min(length, NMax);
                int blocks = n / BlockSize;
                n = blocks * BlockSize;
                length -= n;

                Vector128<uint> vs1 = Vector128<uint>.Zero;
                Vector128<uint> vs2 = Vector128.CreateScalar(s2);
                Vector128<uint> vps = Vector128.CreateScalar(s1 * (uint)blocks);

                do
                {
                    Vector128<byte> bytes1 = Vector128.LoadUnsafe(ref sourceRef);
                    Vector128<byte> bytes2 = Vector128.LoadUnsafe(ref sourceRef, 16);
                    sourceRef = ref Unsafe.Add(ref sourceRef, BlockSize);

                    vps += vs1;

                    if (Ssse3.IsSupported)
                    {
                        vs1 += Sse2.SumAbsoluteDifferences(bytes1, Vector128<byte>.Zero).AsUInt32();
                        vs1 += Sse2.SumAbsoluteDifferences(bytes2, Vector128<byte>.Zero).AsUInt32();

                        vs2 += Sse2.MultiplyAddAdjacent(Ssse3.MultiplyAddAdjacent(bytes1, tap1), Vector128<short>.One).AsUInt32();
                        vs2 += Sse2.MultiplyAddAdjacent(Ssse3.MultiplyAddAdjacent(bytes2, tap2), Vector128<short>.One).AsUInt32();
                    }
                    else
                    {
                        (Vector128<ushort> lo1, Vector128<ushort> hi1) = Vector128.Widen(bytes1);
                        (Vector128<ushort> lo2, Vector128<ushort> hi2) = Vector128.Widen(bytes2);
                        (Vector128<uint> sumLo, Vector128<uint> sumHi) = Vector128.Widen(lo1 + hi1 + lo2 + hi2);
                        vs1 += sumLo + sumHi;
                        vs2 += WeightedSumWidening128(bytes1, tap1) + WeightedSumWidening128(bytes2, tap2);

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static Vector128<uint> WeightedSumWidening128(Vector128<byte> data, Vector128<sbyte> weights)
                        {
                            (Vector128<ushort> dLo, Vector128<ushort> dHi) = Vector128.Widen(data);
                            (Vector128<short> wLo, Vector128<short> wHi) = Vector128.Widen(weights);

                            (Vector128<int> pLo1, Vector128<int> pHi1) = Vector128.Widen(dLo.AsInt16() * wLo);
                            (Vector128<int> pLo2, Vector128<int> pHi2) = Vector128.Widen(dHi.AsInt16() * wHi);

                            return (pLo1 + pHi1 + pLo2 + pHi2).AsUInt32();
                        }
                    }
                }
                while (--blocks > 0);

                vs2 += vps << 5;

                s1 += Vector128.Sum(vs1);
                s2 = Vector128.Sum(vs2);

                s1 %= ModBase;
                s2 %= ModBase;
            }
            while (length >= BlockSize);

            if (length > 0)
            {
                UpdateScalarTail(ref sourceRef, length, ref s1, ref s2);
            }

            return (s2 << 16) | s1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateVector256(uint adler, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length >= Vector256<byte>.Count);

            const int BlockSize = 32;

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector256<sbyte> weights = Vector256.Create(32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            do
            {
                int n = Math.Min(length, NMax);
                int blocks = n / BlockSize;
                n = blocks * BlockSize;
                length -= n;

                Vector256<uint> vs1 = Vector256.CreateScalar(s1);
                Vector256<uint> vs2 = Vector256.CreateScalar(s2);
                Vector256<uint> vs3 = Vector256<uint>.Zero;

                do
                {
                    Vector256<byte> data = Vector256.LoadUnsafe(ref sourceRef);
                    sourceRef = ref Unsafe.Add(ref sourceRef, BlockSize);

                    Vector256<uint> vs1_0 = vs1;
                    vs1 += Avx2.SumAbsoluteDifferences(data, Vector256<byte>.Zero).AsUInt32();
                    vs3 += vs1_0;

                    Vector256<short> mad = Avx2.MultiplyAddAdjacent(data, weights);
                    vs2 += Avx2.MultiplyAddAdjacent(mad, Vector256<short>.One).AsUInt32();
                }
                while (--blocks > 0);

                vs3 <<= 5;
                vs2 += vs3;

                s1 = (uint)Vector256.Sum(vs1.AsUInt64()); // SumAbsoluteDifferences stores the results in the even lanes
                s2 = Vector256.Sum(vs2);

                s1 %= ModBase;
                s2 %= ModBase;
            }
            while (length >= BlockSize);

            if (length > 0)
            {
                UpdateScalarTail(ref sourceRef, length, ref s1, ref s2);
            }

            return (s2 << 16) | s1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateVector512(uint adler, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length >= Vector512<byte>.Count);

            const int BlockSize = 64;

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            Vector512<sbyte> weights = Vector512.Create(
                32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1,
                32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);

            do
            {
                int n = Math.Min(length, NMax);
                int blocks = n / BlockSize;
                n = blocks * BlockSize;
                length -= n;

                Vector512<uint> vs1 = Vector512.CreateScalar(s1);
                Vector512<uint> vs2 = Vector512.CreateScalar(s2);
                Vector512<uint> vs3 = Vector512<uint>.Zero;

                do
                {
                    Vector512<byte> data = Vector512.LoadUnsafe(ref sourceRef);
                    sourceRef = ref Unsafe.Add(ref sourceRef, BlockSize);

                    Vector512<uint> vs1_0 = vs1;
                    vs1 += Avx512BW.SumAbsoluteDifferences(data, Vector512<byte>.Zero).AsUInt32();
                    vs3 += vs1_0;
                    vs2 += Avx512BW.MultiplyAddAdjacent(Avx512BW.MultiplyAddAdjacent(data, weights), Vector512<short>.One).AsUInt32();

                    Vector256<uint> sumLo = Avx2.SumAbsoluteDifferences(data.GetLower(), Vector256<byte>.Zero).AsUInt32();
                    vs2 += Vector512.Create(sumLo << 5, Vector256<uint>.Zero);
                }
                while (--blocks > 0);

                vs3 <<= 6;
                vs2 += vs3;

                s1 = (uint)Vector512.Sum(vs1.AsUInt64());
                s2 = Vector512.Sum(vs2);

                s1 %= ModBase;
                s2 %= ModBase;
            }
            while (length >= BlockSize);

            if (length >= Vector256<byte>.Count)
            {
                return UpdateVector256((s2 << 16) | s1, MemoryMarshal.CreateReadOnlySpan(ref sourceRef, length));
            }

            if (length > 0)
            {
                UpdateScalarTail(ref sourceRef, length, ref s1, ref s2);
            }

            return (s2 << 16) | s1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint UpdateArm128(uint adler, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length >= Vector128<byte>.Count * 2);

            const int BlockSize = 32; // two Vector128<byte> loads

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            int length = source.Length;

            do
            {
                int n = Math.Min(length, NMax);
                int blocks = n / BlockSize;
                n = blocks * BlockSize;
                length -= n;

                Vector128<uint> vs1 = Vector128<uint>.Zero;
                Vector128<uint> vps = Vector128.CreateScalar(s1 * (uint)blocks);

                Vector128<ushort> vColumnSum1 = Vector128<ushort>.Zero;
                Vector128<ushort> vColumnSum2 = Vector128<ushort>.Zero;
                Vector128<ushort> vColumnSum3 = Vector128<ushort>.Zero;
                Vector128<ushort> vColumnSum4 = Vector128<ushort>.Zero;

                do
                {
                    Vector128<byte> bytes1 = Vector128.LoadUnsafe(ref sourceRef);
                    Vector128<byte> bytes2 = Vector128.LoadUnsafe(ref sourceRef, 16);
                    sourceRef = ref Unsafe.Add(ref sourceRef, BlockSize);

                    vps += vs1;

                    vs1 = AdvSimd.AddPairwiseWideningAndAdd(
                        vs1,
                        AdvSimd.AddPairwiseWideningAndAdd(
                            AdvSimd.AddPairwiseWidening(bytes1),
                            bytes2));

                    vColumnSum1 = AdvSimd.AddWideningLower(vColumnSum1, bytes1.GetLower());
                    vColumnSum2 = AdvSimd.AddWideningLower(vColumnSum2, bytes1.GetUpper());
                    vColumnSum3 = AdvSimd.AddWideningLower(vColumnSum3, bytes2.GetLower());
                    vColumnSum4 = AdvSimd.AddWideningLower(vColumnSum4, bytes2.GetUpper());
                }
                while (--blocks > 0);

                Vector128<uint> vs2 = vps << 5;
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum1.GetLower(), Vector64.Create((ushort)32, 31, 30, 29));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum1.GetUpper(), Vector64.Create((ushort)28, 27, 26, 25));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum2.GetLower(), Vector64.Create((ushort)24, 23, 22, 21));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum2.GetUpper(), Vector64.Create((ushort)20, 19, 18, 17));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum3.GetLower(), Vector64.Create((ushort)16, 15, 14, 13));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum3.GetUpper(), Vector64.Create((ushort)12, 11, 10, 9));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum4.GetLower(), Vector64.Create((ushort)8, 7, 6, 5));
                vs2 = AdvSimd.MultiplyWideningLowerAndAdd(vs2, vColumnSum4.GetUpper(), Vector64.Create((ushort)4, 3, 2, 1));

                s1 += Vector128.Sum(vs1);
                s2 += Vector128.Sum(vs2);

                s1 %= ModBase;
                s2 %= ModBase;
            }
            while (length >= BlockSize);

            if (length > 0)
            {
                UpdateScalarTail(ref sourceRef, length, ref s1, ref s2);
            }

            return (s2 << 16) | s1;
        }

        private static void UpdateScalarTail(ref byte sourceRef, int length, ref uint s1, ref uint s2)
        {
            Debug.Assert(length is > 0 and < NMax);

            foreach (byte b in MemoryMarshal.CreateReadOnlySpan(ref sourceRef, length))
            {
                s1 += b;
                s2 += s1;
            }

            s1 %= ModBase;
            s2 %= ModBase;
        }
#endif
    }
}
