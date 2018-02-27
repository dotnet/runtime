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
        public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            byte valueHead = value;
            ref byte valueTail = ref Unsafe.Add(ref value, 1);
            int valueTailLength = valueLength - 1;

            int index = 0;
            for (; ; )
            {
                Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = IndexOf(ref Unsafe.Add(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
                if (relativeIndex == -1)
                    break;
                index += relativeIndex;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(ref Unsafe.Add(ref searchSpace, index + 1), ref valueTail, valueTailLength))
                    return index;  // The tail matched. Return a successful find.

                index++;
            }
            return -1;
        }

        public static int IndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            int index = -1;
            for (int i = 0; i < valueLength; i++)
            {
                var tempIndex = IndexOf(ref searchSpace, Unsafe.Add(ref value, i), searchSpaceLength);
                if ((uint)tempIndex < (uint)index)
                {
                    index = tempIndex;
                    // Reduce space for search, cause we don't care if we find the search value after the index of a previously found value
                    searchSpaceLength = tempIndex;

                    if (index == 0) break;
                }
            }
            return index;
        }

        public static int LastIndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            int index = -1;
            for (int i = 0; i < valueLength; i++)
            {
                var tempIndex = LastIndexOf(ref searchSpace, Unsafe.Add(ref value, i), searchSpaceLength);
                if (tempIndex > index) index = tempIndex;
            }
            return index;
        }

        public static unsafe int IndexOf(ref byte searchSpace, byte value, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue = value; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)0; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(uint)((Vector<byte>.Count - unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;

                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;
                if (uValue == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (uValue == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (uValue == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;
                if (uValue == Unsafe.Add(ref searchSpace, index + 4))
                    goto Found4;
                if (uValue == Unsafe.Add(ref searchSpace, index + 5))
                    goto Found5;
                if (uValue == Unsafe.Add(ref searchSpace, index + 6))
                    goto Found6;
                if (uValue == Unsafe.Add(ref searchSpace, index + 7))
                    goto Found7;

                index += 8;
            }

            if ((byte*)nLength >= (byte*)4)
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
                nLength = (IntPtr)(uint)((length - (uint)index) & ~(Vector<byte>.Count - 1));
                // Get comparison Vector
                Vector<byte> vComparison = GetVector(value);
                while ((byte*)nLength > (byte*)index)
                {
                    var vMatches = Vector.Equals(vComparison, Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index)));
                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index += Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)index + LocateFirstFoundByte(vMatches);
                }

                if ((int)(byte*)index < length)
                {
                    unchecked
                    {
                        nLength = (IntPtr)(length - (int)(byte*)index);
                    }
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            byte valueHead = value;
            ref byte valueTail = ref Unsafe.Add(ref value, 1);
            int valueTailLength = valueLength - 1;

            int index = 0;
            for (; ; )
            {
                Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = LastIndexOf(ref searchSpace, valueHead, remainingSearchSpaceLength);
                if (relativeIndex == -1)
                    break;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(ref Unsafe.Add(ref searchSpace, relativeIndex + 1), ref valueTail, valueTailLength))
                    return relativeIndex;  // The tail matched. Return a successful find.

                index += remainingSearchSpaceLength - relativeIndex;
            }
            return -1;
        }

        public static unsafe int LastIndexOf(ref byte searchSpace, byte value, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue = value; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)(uint)length; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(((length & (Vector<byte>.Count - 1)) + unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;
                index -= 8;

                if (uValue == Unsafe.Add(ref searchSpace, index + 7))
                    goto Found7;
                if (uValue == Unsafe.Add(ref searchSpace, index + 6))
                    goto Found6;
                if (uValue == Unsafe.Add(ref searchSpace, index + 5))
                    goto Found5;
                if (uValue == Unsafe.Add(ref searchSpace, index + 4))
                    goto Found4;
                if (uValue == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;
                if (uValue == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (uValue == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;
                index -= 4;

                if (uValue == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;
                if (uValue == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (uValue == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;
                index -= 1;

                if (uValue == Unsafe.Add(ref searchSpace, index))
                    goto Found;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index > 0))
            {
                nLength = (IntPtr)(uint)((uint)index & ~(Vector<byte>.Count - 1));

                // Get comparison Vector
                Vector<byte> vComparison = GetVector(value);
                while ((byte*)nLength > (byte*)(Vector<byte>.Count - 1))
                {
                    var vMatches = Vector.Equals(vComparison, Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index - Vector<byte>.Count)));
                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index -= Vector<byte>.Count;
                        nLength -= Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)(index) - Vector<byte>.Count + LocateLastFoundByte(vMatches);
                }
                if ((int)(byte*)index > 0)
                {
                    nLength = index;
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static unsafe int IndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)0; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(uint)((Vector<byte>.Count - unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            uint lookUp;
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 4);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found4;
                lookUp = Unsafe.Add(ref searchSpace, index + 5);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found5;
                lookUp = Unsafe.Add(ref searchSpace, index + 6);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found6;
                lookUp = Unsafe.Add(ref searchSpace, index + 7);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found7;

                index += 8;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;

                index += 4;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;

                index += 1;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index < length))
            {
                nLength = (IntPtr)(uint)((length - (uint)index) & ~(Vector<byte>.Count - 1));
                // Get comparison Vector
                Vector<byte> values0 = GetVector(value0);
                Vector<byte> values1 = GetVector(value1);

                while ((byte*)nLength > (byte*)index)
                {
                    Vector<byte> vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index));
                    var vMatches = Vector.BitwiseOr(
                                    Vector.Equals(vData, values0),
                                    Vector.Equals(vData, values1));
                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index += Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)index + LocateFirstFoundByte(vMatches);
                }

                if ((int)(byte*)index < length)
                {
                    unchecked
                    {
                        nLength = (IntPtr)(length - (int)(byte*)index);
                    }
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static unsafe int IndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue2 = value2; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)0; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(uint)((Vector<byte>.Count - unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            uint lookUp;
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found4;
                lookUp = Unsafe.Add(ref searchSpace, index + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found5;
                lookUp = Unsafe.Add(ref searchSpace, index + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found6;
                lookUp = Unsafe.Add(ref searchSpace, index + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found7;

                index += 8;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;

                index += 4;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;

                index += 1;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index < length))
            {
                nLength = (IntPtr)(uint)((length - (uint)index) & ~(Vector<byte>.Count - 1));
                // Get comparison Vector
                Vector<byte> values0 = GetVector(value0);
                Vector<byte> values1 = GetVector(value1);
                Vector<byte> values2 = GetVector(value2);
                while ((byte*)nLength > (byte*)index)
                {
                    Vector<byte> vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index));

                    var vMatches = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(vData, values0),
                                        Vector.Equals(vData, values1)),
                                    Vector.Equals(vData, values2));

                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index += Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)index + LocateFirstFoundByte(vMatches);
                }

                if ((int)(byte*)index < length)
                {
                    unchecked
                    {
                        nLength = (IntPtr)(length - (int)(byte*)index);
                    }
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static unsafe int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)(uint)length; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(((length & (Vector<byte>.Count - 1)) + unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            uint lookUp;
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;
                index -= 8;

                lookUp = Unsafe.Add(ref searchSpace, index + 7);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found7;
                lookUp = Unsafe.Add(ref searchSpace, index + 6);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found6;
                lookUp = Unsafe.Add(ref searchSpace, index + 5);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found5;
                lookUp = Unsafe.Add(ref searchSpace, index + 4);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found4;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;
                index -= 4;

                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;
                index -= 1;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index > 0))
            {
                nLength = (IntPtr)(uint)((uint)index & ~(Vector<byte>.Count - 1));
                // Get comparison Vector
                Vector<byte> values0 = GetVector(value0);
                Vector<byte> values1 = GetVector(value1);

                while ((byte*)nLength > (byte*)(Vector<byte>.Count - 1))
                {
                    Vector<byte> vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index - Vector<byte>.Count));
                    var vMatches = Vector.BitwiseOr(
                                    Vector.Equals(vData, values0),
                                    Vector.Equals(vData, values1));
                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index -= Vector<byte>.Count;
                        nLength -= Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)(index) - Vector<byte>.Count + LocateLastFoundByte(vMatches);
                }

                if ((int)(byte*)index > 0)
                {
                    nLength = index;
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static unsafe int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue2 = value2; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            IntPtr index = (IntPtr)(uint)length; // Use UIntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)(uint)length;
#if !netstandard11
            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                unchecked
                {
                    int unaligned = (int)(byte*)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                    nLength = (IntPtr)(((length & (Vector<byte>.Count - 1)) + unaligned) & (Vector<byte>.Count - 1));
                }
            }
        SequentialScan:
#endif
            uint lookUp;
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;
                index -= 8;

                lookUp = Unsafe.Add(ref searchSpace, index + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found7;
                lookUp = Unsafe.Add(ref searchSpace, index + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found6;
                lookUp = Unsafe.Add(ref searchSpace, index + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found5;
                lookUp = Unsafe.Add(ref searchSpace, index + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found4;
                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;
                index -= 4;

                lookUp = Unsafe.Add(ref searchSpace, index + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.Add(ref searchSpace, index + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref searchSpace, index + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;
                index -= 1;

                lookUp = Unsafe.Add(ref searchSpace, index);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }
#if !netstandard11
            if (Vector.IsHardwareAccelerated && ((int)(byte*)index > 0))
            {
                nLength = (IntPtr)(uint)((uint)index & ~(Vector<byte>.Count - 1));
                // Get comparison Vector
                Vector<byte> values0 = GetVector(value0);
                Vector<byte> values1 = GetVector(value1);
                Vector<byte> values2 = GetVector(value2);
                while ((byte*)nLength > (byte*)(Vector<byte>.Count - 1))
                {
                    Vector<byte> vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index - Vector<byte>.Count));

                    var vMatches = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(vData, values0),
                                        Vector.Equals(vData, values1)),
                                    Vector.Equals(vData, values2));

                    if (Vector<byte>.Zero.Equals(vMatches))
                    {
                        index -= Vector<byte>.Count;
                        nLength -= Vector<byte>.Count;
                        continue;
                    }
                    // Find offset of first match
                    return (int)(byte*)(index) - Vector<byte>.Count + LocateLastFoundByte(vMatches);
                }

                if ((int)(byte*)index > 0)
                {
                    nLength = index;
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
        Found4:
            return (int)(byte*)(index + 4);
        Found5:
            return (int)(byte*)(index + 5);
        Found6:
            return (int)(byte*)(index + 6);
        Found7:
            return (int)(byte*)(index + 7);
        }

        public static unsafe bool SequenceEqual(ref byte first, ref byte second, int length)
        {
            Debug.Assert(length >= 0);

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            IntPtr i = (IntPtr)0; // Use IntPtr and byte* for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr n = (IntPtr)length;

#if !netstandard11
            if (Vector.IsHardwareAccelerated && (byte*)n >= (byte*)Vector<byte>.Count)
            {
                n -= Vector<byte>.Count;
                while ((byte*)n > (byte*)i)
                {
                    if (Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref first, i)) !=
                        Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref second, i)))
                    {
                        goto NotEqual;
                    }
                    i += Vector<byte>.Count;
                }
                return Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref first, n)) ==
                       Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref second, n));
            }
#endif

            if ((byte*)n >= (byte*)sizeof(UIntPtr))
            {
                n -= sizeof(UIntPtr);
                while ((byte*)n > (byte*)i)
                {
                    if (Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref first, i)) !=
                        Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref second, i)))
                    {
                        goto NotEqual;
                    }
                    i += sizeof(UIntPtr);
                }
                return Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref first, n)) ==
                       Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref second, n));
            }

            while ((byte*)n > (byte*)i)
            {
                if (Unsafe.AddByteOffset(ref first, i) != Unsafe.AddByteOffset(ref second, i))
                    goto NotEqual;
                i += 1;
            }

        Equal:
            return true;

        NotEqual: // Workaround for https://github.com/dotnet/coreclr/issues/13549
            return false;
        }

#if !netstandard11
        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(Vector<byte> match)
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
            return i * 8 + LocateFirstFoundByte(candidate);
        }
#endif

        public static unsafe int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
        {
            Debug.Assert(firstLength >= 0);
            Debug.Assert(secondLength >= 0);

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            var minLength = firstLength;
            if (minLength > secondLength) minLength = secondLength;

            IntPtr i = (IntPtr)0; // Use IntPtr and byte* for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr n = (IntPtr)minLength;

#if !netstandard11
            if (Vector.IsHardwareAccelerated && (byte*)n > (byte*)Vector<byte>.Count)
            {
                n -= Vector<byte>.Count;
                while ((byte*)n > (byte*)i)
                {
                    if (Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref first, i)) !=
                        Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref second, i)))
                    {
                        goto NotEqual;
                    }
                    i += Vector<byte>.Count;
                }
                goto NotEqual;
            }
#endif

            if ((byte*)n > (byte*)sizeof(UIntPtr))
            {
                n -= sizeof(UIntPtr);
                while ((byte*)n > (byte*)i)
                {
                    if (Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref first, i)) !=
                        Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref second, i)))
                    {
                        goto NotEqual;
                    }
                    i += sizeof(UIntPtr);
                }
            }

        NotEqual:  // Workaround for https://github.com/dotnet/coreclr/issues/13549
            while((byte*)minLength > (byte*)i)
            {
                int result = Unsafe.AddByteOffset(ref first, i).CompareTo(Unsafe.AddByteOffset(ref second, i));
                if (result != 0) return result;
                i += 1;
            }

        Equal:
            return firstLength - secondLength;
        }

#if !netstandard11
        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundByte(Vector<byte> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = Vector<ulong>.Count - 1;
            // Pattern unrolled by jit https://github.com/dotnet/coreclr/pull/8001
            for (; i >= 0; i--)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 8 + LocateLastFoundByte(candidate);
        }
#endif

#if !netstandard11
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(ulong match)
        {
            unchecked
            {
                // Flag least significant power of two bit
                var powerOfTwoFlag = match ^ (match - 1);
                // Shift all powers of two into the high byte and extract
                return (int)((powerOfTwoFlag * XorPowerOfTwoToHighByte) >> 57);
            }
        }
#endif

#if !netstandard11
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundByte(ulong match)
        {
            // Find the most significant byte that has its highest bit set
            int index = 7;
            while ((long)match > 0)
            {
                match = match << 8;
                index--;
            }
            return index;
        }
#endif

#if !netstandard11
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<byte> GetVector(byte vectorByte)
        {
#if !netcoreapp
            // Vector<byte> .ctor doesn't become an intrinsic due to detection issue
            // However this does cause it to become an intrinsic (with additional multiply and reg->reg copy)
            // https://github.com/dotnet/coreclr/issues/7459#issuecomment-253965670
            return Vector.AsVectorByte(new Vector<uint>(vectorByte * 0x01010101u));
#else
            return new Vector<byte>(vectorByte);
#endif
        }
#endif

#if !netstandard11
        private const ulong XorPowerOfTwoToHighByte = (0x07ul |
                                                       0x06ul << 8 |
                                                       0x05ul << 16 |
                                                       0x04ul << 24 |
                                                       0x03ul << 32 |
                                                       0x02ul << 40 |
                                                       0x01ul << 48) + 1;
#endif
    }
}
