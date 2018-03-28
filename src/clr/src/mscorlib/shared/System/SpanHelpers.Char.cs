// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

#if !netstandard11
using System.Numerics;
#endif

namespace System
{
    internal static partial class SpanHelpers
    {
        public static unsafe int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
        {
            Debug.Assert(firstLength >= 0);
            Debug.Assert(secondLength >= 0);

            int lengthDelta = firstLength - secondLength;

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            IntPtr minLength = (IntPtr)((firstLength < secondLength) ? firstLength : secondLength);
            IntPtr i = (IntPtr)0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations

            if ((byte*)minLength >= (byte*)(sizeof(UIntPtr) / sizeof(char)))
            {
#if !netstandard11
                if (Vector.IsHardwareAccelerated && (byte*)minLength >= (byte*)Vector<ushort>.Count)
                {
                    IntPtr nLength = minLength - Vector<ushort>.Count;
                    do
                    {
                        if (Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, i))) !=
                            Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, i))))
                        {
                            break;
                        }
                        i += Vector<ushort>.Count;
                    }
                    while ((byte*)nLength >= (byte*)i);
                }
#endif

                while ((byte*)minLength >= (byte*)(i + sizeof(UIntPtr) / sizeof(char)))
                {
                    if (Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, i))) !=
                        Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, i))))
                    {
                        break;
                    }
                    i += sizeof(UIntPtr) / sizeof(char);
                }
            }

            if (sizeof(UIntPtr) > sizeof(int) && (byte*)minLength >= (byte*)(i + sizeof(int) / sizeof(char)))
            {
                if (Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, i))) ==
                    Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, i))))
                {
                    i += sizeof(int) / sizeof(char);
                }
            }

            while ((byte*)i < (byte*)minLength)
            {
                int result = Unsafe.Add(ref first, i).CompareTo(Unsafe.Add(ref second, i));
                if (result != 0) return result;
                i += 1;
            }

        Equal:
            return lengthDelta;
        }

        public static unsafe int IndexOf(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            fixed (char* pChars = &searchSpace)
            {
                char* pCh = pChars;
                char* pEndCh = pCh + length;

#if !netstandard11
                if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
                {
                    const int elementsPerByte = sizeof(ushort) / sizeof(byte);
                    int unaligned = ((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / elementsPerByte;
                    length = ((Vector<ushort>.Count - unaligned) & (Vector<ushort>.Count - 1));
                }
            SequentialScan:
#endif
                while (length >= 4)
                {
                    length -= 4;

                    if (*pCh == value)
                        goto Found;
                    if (*(pCh + 1) == value)
                        goto Found1;
                    if (*(pCh + 2) == value)
                        goto Found2;
                    if (*(pCh + 3) == value)
                        goto Found3;

                    pCh += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (*pCh == value)
                        goto Found;

                    pCh += 1;
                }
#if !netstandard11
                // We get past SequentialScan only if IsHardwareAccelerated is true. However, we still have the redundant check to allow
                // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
                if (Vector.IsHardwareAccelerated && pCh < pEndCh)
                {
                    length = (int)((pEndCh - pCh) & ~(Vector<ushort>.Count - 1));

                    // Get comparison Vector
                    Vector<ushort> vComparison = new Vector<ushort>(value);

                    while (length > 0)
                    {
                        // Using Unsafe.Read instead of ReadUnaligned since the search space is pinned and pCh is always vector aligned
                        Debug.Assert(((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) == 0);
                        Vector<ushort> vMatches = Vector.Equals(vComparison, Unsafe.Read<Vector<ushort>>(pCh));
                        if (Vector<ushort>.Zero.Equals(vMatches))
                        {
                            pCh += Vector<ushort>.Count;
                            length -= Vector<ushort>.Count;
                            continue;
                        }
                        // Find offset of first match
                        return (int)(pCh - pChars) + LocateFirstFoundChar(vMatches);
                    }

                    if (pCh < pEndCh)
                    {
                        length = (int)(pEndCh - pCh);
                        goto SequentialScan;
                    }
                }
#endif
                return -1;
            Found3:
                pCh++;
            Found2:
                pCh++;
            Found1:
                pCh++;
            Found:
                return (int)(pCh - pChars);
            }
        }

#if !netstandard11
        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundChar(Vector<ushort> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = 0;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i < Vector<ulong>.Count; i++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 4 + LocateFirstFoundChar(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundChar(ulong match)
        {
            unchecked
            {
                // Flag least significant power of two bit
                var powerOfTwoFlag = match ^ (match - 1);
                // Shift all powers of two into the high byte and extract
                return (int)((powerOfTwoFlag * XorPowerOfTwoToHighChar) >> 49);
            }
        }

        private const ulong XorPowerOfTwoToHighChar = (0x03ul |
                                                       0x02ul << 16 |
                                                       0x01ul << 32) + 1;
#endif
    }
}
