// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Determines whether the provided value contains only ASCII bytes.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII bytes or is
        /// empty; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(ReadOnlySpan<byte> value) =>
            IsValidCore(ref MemoryMarshal.GetReference(value), value.Length);

        /// <summary>
        /// Determines whether the provided value contains only ASCII chars.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII chars or is
        /// empty; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(ReadOnlySpan<char> value) =>
            IsValidCore(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(value)), value.Length);

        /// <summary>
        /// Determines whether the provided value is ASCII byte.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> is ASCII, False otherwise.</returns>
        public static bool IsValid(byte value) => value <= 127;

        /// <summary>
        /// Determines whether the provided value is ASCII char.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> is ASCII, False otherwise.</returns>
        public static bool IsValid(char value) => value <= 127;

        private static unsafe bool IsValidCore<T>(ref T searchSpace, int length) where T : unmanaged
        {
            Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                uint elementsPerUlong = (uint)(sizeof(ulong) / sizeof(T));

                if (length < elementsPerUlong)
                {
                    if (typeof(T) == typeof(byte) && length >= sizeof(uint))
                    {
                        // Process byte inputs with lengths [4, 7]
                        return AllBytesInUInt32AreAscii(
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.As<T, byte>(ref searchSpace)) |
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref searchSpace, length - sizeof(uint)))));
                    }

                    // Process inputs with lengths [0, 3]
                    for (nuint j = 0; j < (uint)length; j++)
                    {
                        if (typeof(T) == typeof(byte)
                            ? (Unsafe.BitCast<T, byte>(Unsafe.Add(ref searchSpace, j)) > 127)
                            : (Unsafe.BitCast<T, char>(Unsafe.Add(ref searchSpace, j)) > 127))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                nuint i = 0;

                // If vectorization isn't supported, process 16 bytes at a time.
                if (!Vector128.IsHardwareAccelerated && length > 2 * elementsPerUlong)
                {
                    nuint finalStart = (nuint)length - 2 * elementsPerUlong;

                    for (; i < finalStart; i += 2 * elementsPerUlong)
                    {
                        if (!AllCharsInUInt64AreAscii<T>(
                            Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref searchSpace, i))) |
                            Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref searchSpace, i + elementsPerUlong)))))
                        {
                            return false;
                        }
                    }

                    i = finalStart;
                }

                // Process the last [8, 16] bytes.
                return AllCharsInUInt64AreAscii<T>(
                    Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref searchSpace, i))) |
                    Unsafe.ReadUnaligned<ulong>(ref Unsafe.Subtract(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref searchSpace, length)), sizeof(ulong))));
            }

            ref T searchSpaceEnd = ref Unsafe.Add(ref searchSpace, length);

            // Process inputs with lengths [16, 32] bytes.
            if (length <= 2 * Vector128<T>.Count)
            {
                return AllCharsInVectorAreAscii(
                    Vector128.LoadUnsafe(ref searchSpace) |
                    Vector128.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, Vector128<T>.Count)));
            }

            if (Avx.IsSupported)
            {
                // Process inputs with lengths [33, 64] bytes.
                if (length <= 2 * Vector256<T>.Count)
                {
                    return AllCharsInVectorAreAscii(
                        Vector256.LoadUnsafe(ref searchSpace) |
                        Vector256.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, Vector256<T>.Count)));
                }

                // Process long inputs 128 bytes at a time.
                if (length > 4 * Vector256<T>.Count)
                {
                    // Process the first 128 bytes.
                    if (!AllCharsInVectorAreAscii(
                        Vector256.LoadUnsafe(ref searchSpace) |
                        Vector256.LoadUnsafe(ref searchSpace, (nuint)Vector256<T>.Count) |
                        Vector256.LoadUnsafe(ref searchSpace, 2 * (nuint)Vector256<T>.Count) |
                        Vector256.LoadUnsafe(ref searchSpace, 3 * (nuint)Vector256<T>.Count)))
                    {
                        return false;
                    }

                    nuint i = 4 * (nuint)Vector256<T>.Count;

                    // Try to opportunistically align the reads below. The input isn't pinned, so the GC
                    // is free to move the references. We're therefore assuming that reads may still be unaligned.
                    // They may also be unaligned if the input chars aren't 2-byte aligned.
                    nuint misalignedElements = Unsafe.OpportunisticMisalignment(ref searchSpace, Vector256<byte>.Count) / (nuint)sizeof(T);
                    i -= misalignedElements;
                    Debug.Assert((int)i > 3 * Vector256<T>.Count);

                    nuint finalStart = (nuint)length - 4 * (nuint)Vector256<T>.Count;

                    for (; i < finalStart; i += 4 * (nuint)Vector256<T>.Count)
                    {
                        ref T current = ref Unsafe.Add(ref searchSpace, i);

                        if (!AllCharsInVectorAreAscii(
                            Vector256.LoadUnsafe(ref current) |
                            Vector256.LoadUnsafe(ref current, (nuint)Vector256<T>.Count) |
                            Vector256.LoadUnsafe(ref current, 2 * (nuint)Vector256<T>.Count) |
                            Vector256.LoadUnsafe(ref current, 3 * (nuint)Vector256<T>.Count)))
                        {
                            return false;
                        }
                    }

                    searchSpace = ref Unsafe.Add(ref searchSpace, finalStart);
                }

                // Process the last [1, 128] bytes.
                // The search space has at least 2 * Vector256 bytes available to read.
                // We process the first 2 and last 2 vectors, which may overlap.
                return AllCharsInVectorAreAscii(
                    Vector256.LoadUnsafe(ref searchSpace) |
                    Vector256.LoadUnsafe(ref searchSpace, (nuint)Vector256<T>.Count) |
                    Vector256.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, 2 * Vector256<T>.Count)) |
                    Vector256.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, Vector256<T>.Count)));
            }
            else
            {
                // Process long inputs 64 bytes at a time.
                if (length > 4 * Vector128<T>.Count)
                {
                    // Process the first 64 bytes.
                    if (!AllCharsInVectorAreAscii(
                        Vector128.LoadUnsafe(ref searchSpace) |
                        Vector128.LoadUnsafe(ref searchSpace, (nuint)Vector128<T>.Count) |
                        Vector128.LoadUnsafe(ref searchSpace, 2 * (nuint)Vector128<T>.Count) |
                        Vector128.LoadUnsafe(ref searchSpace, 3 * (nuint)Vector128<T>.Count)))
                    {
                        return false;
                    }

                    nuint i = 4 * (nuint)Vector128<T>.Count;

                    // Try to opportunistically align the reads below. The input isn't pinned, so the GC
                    // is free to move the references. We're therefore assuming that reads may still be unaligned.
                    // They may also be unaligned if the input chars aren't 2-byte aligned.
                    nuint misalignedElements = Unsafe.OpportunisticMisalignment(ref searchSpace, Vector128<byte>.Count) / (nuint)sizeof(T);
                    i -= misalignedElements;
                    Debug.Assert((int)i > 3 * Vector128<T>.Count);

                    nuint finalStart = (nuint)length - 4 * (nuint)Vector128<T>.Count;

                    for (; i < finalStart; i += 4 * (nuint)Vector128<T>.Count)
                    {
                        ref T current = ref Unsafe.Add(ref searchSpace, i);

                        if (!AllCharsInVectorAreAscii(
                            Vector128.LoadUnsafe(ref current) |
                            Vector128.LoadUnsafe(ref current, (nuint)Vector128<T>.Count) |
                            Vector128.LoadUnsafe(ref current, 2 * (nuint)Vector128<T>.Count) |
                            Vector128.LoadUnsafe(ref current, 3 * (nuint)Vector128<T>.Count)))
                        {
                            return false;
                        }
                    }

                    searchSpace = ref Unsafe.Add(ref searchSpace, finalStart);
                }

                // Process the last [1, 64] bytes.
                // The search space has at least 2 * Vector128 bytes available to read.
                // We process the first 2 and last 2 vectors, which may overlap.
                return AllCharsInVectorAreAscii(
                    Vector128.LoadUnsafe(ref searchSpace) |
                    Vector128.LoadUnsafe(ref searchSpace, (nuint)Vector128<T>.Count) |
                    Vector128.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, 2 * Vector128<T>.Count)) |
                    Vector128.LoadUnsafe(ref Unsafe.Subtract(ref searchSpaceEnd, Vector128<T>.Count)));
            }
        }
    }
}
