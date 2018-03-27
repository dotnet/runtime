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

            uint uValue = value; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
            {
                const int elementsPerByte = sizeof(ushort) / sizeof(byte);
                int unaligned = ((int)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1)) / elementsPerByte;
                nLength = (IntPtr)((Vector<ushort>.Count - unaligned) & (Vector<ushort>.Count - 1));
            }
        SequentialScan:
#endif
            while ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;

                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;
                if (uValue == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (uValue == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (uValue == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;

                index += 4;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;

                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;

                index += 1;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index < length))
            {
                nLength = (IntPtr)((length - (int)(byte*)index) & ~(Vector<ushort>.Count - 1));

                // Get comparison Vector
                Vector<ushort> vComparison = new Vector<ushort>(value);

                while ((byte*)nLength > (byte*)index)
                {
                    var vMatches = Vector.Equals(vComparison, Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, index))));
                    if (Vector<ushort>.Zero.Equals(vMatches))
                    {
                        index += Vector<ushort>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)index + LocateFirstFoundChar(vMatches);
                }

                if ((int)(byte*)index < length)
                {
                    nLength = (IntPtr)(length - (int)(byte*)index);
                    goto SequentialScan;
                }
            }
#endif
            return -1;
        Found: // Workaround for https://github.com/dotnet/coreclr/issues/13549
            return (int)(byte*)index;
        Found1:
            return (int)(byte*)(index + 1);
        Found2:
            return (int)(byte*)(index + 2);
        Found3:
            return (int)(byte*)(index + 3);
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
