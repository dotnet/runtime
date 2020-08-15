// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        // https://tools.ietf.org/html/rfc8259
        // Does the span contain '"', '\',  or any control characters (i.e. 0 to 31)
        // IndexOfAny(34, 92, < 32)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            if (Vector.IsHardwareAccelerated)
            {
                return IndexOfOrLessThan(
                        ref MemoryMarshal.GetReference(span),
                        JsonConstants.Quote,
                        JsonConstants.BackSlash,
                        lessThan: JsonConstants.Space,
                        span.Length);
            }
            else
            {
                return IndexOfOrLessThanNonVector(span);
            }
        }

        private static unsafe int IndexOfOrLessThan(ref byte searchSpace, byte value0, byte value1, byte lessThan, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uLessThan = lessThan; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, as it is used later
                nint vectorDiff = (nint)length - Vector<byte>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }

            uint lookUp;
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;

                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;
                lookUp = AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found1;
                lookUp = AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found2;
                lookUp = AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found3;
                lookUp = AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found4;
                lookUp = AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found5;
                lookUp = AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found6;
                lookUp = AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;
                lookUp = AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found1;
                lookUp = AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found2;
                lookUp = AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return (int)offset;
        Found1:
            return (int)(offset + 1);
        Found2:
            return (int)(offset + 2);
        Found3:
            return (int)(offset + 3);
        Found4:
            return (int)(offset + 4);
        Found5:
            return (int)(offset + 5);
        Found6:
            return (int)(offset + 6);
        Found7:
            return (int)(offset + 7);

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Vector.IsHardwareAccelerated)
            {
                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);
                Vector<byte> valuesLessThan = new Vector<byte>(lessThan);

                Vector<byte> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchSpace, offset);
                    search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1)),
                                Vector.LessThan(search, valuesLessThan));
                    if (Vector<byte>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<byte>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchSpace, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                            Vector.BitwiseOr(
                                Vector.Equals(search, values0),
                                Vector.Equals(search, values1)),
                            Vector.LessThan(search, valuesLessThan));
                if (Vector<byte>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)LocateFirstFoundByte(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }
    }
}
