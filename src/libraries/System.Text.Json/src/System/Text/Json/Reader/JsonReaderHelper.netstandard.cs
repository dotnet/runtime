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
        /// <summary>IndexOfAny('"', '\', less than 32)</summary>
        /// <remarks>https://tools.ietf.org/html/rfc8259</remarks>
        public static unsafe int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            // Borrowed and modified from SpanHelpers.Byte:
            // https://github.com/dotnet/corefx/blob/fc169cddedb6820aaabbdb8b7bece2a3df0fd1a5/src/Common/src/CoreLib/System/SpanHelpers.Byte.cs#L473-L604

            ref byte searchSpace = ref MemoryMarshal.GetReference(span);
            int length = span.Length;
            Debug.Assert(length >= 0);

            const byte Value0 = JsonConstants.Quote;
            const byte Value1 = JsonConstants.BackSlash;
            const byte LessThan = JsonConstants.Space;

            const uint UValue0 = Value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            const uint UValue1 = Value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            const uint ULessThan = LessThan; // Use uint for comparisons to avoid unnecessary 8->32 extensions

            IntPtr index = (IntPtr)0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            IntPtr nLength = (IntPtr)length;

            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                int unaligned = (int)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
                nLength = (IntPtr)((Vector<byte>.Count - unaligned) & (Vector<byte>.Count - 1));
            }
        SequentialScan:
            uint lookUp;
            while ((byte*)nLength >= (byte*)8)
            {
                nLength -= 8;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 1);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 2);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 3);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 4);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found4;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 5);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found5;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 6);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found6;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 7);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found7;

                index += 8;
            }

            if ((byte*)nLength >= (byte*)4)
            {
                nLength -= 4;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 1);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 2);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, index + 3);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found3;

                index += 4;
            }

            while ((byte*)nLength > (byte*)0)
            {
                nLength -= 1;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, index);
                if (UValue0 == lookUp || UValue1 == lookUp || ULessThan > lookUp)
                    goto Found;

                index += 1;
            }

            if (Vector.IsHardwareAccelerated && ((int)(byte*)index < length))
            {
                nLength = (IntPtr)((length - (int)(byte*)index) & ~(Vector<byte>.Count - 1));

                // Get comparison Vector
                Vector<byte> values0 = new Vector<byte>(Value0);
                Vector<byte> values1 = new Vector<byte>(Value1);
                Vector<byte> valuesLessThan = new Vector<byte>(LessThan);

                while ((byte*)nLength > (byte*)index)
                {
                    Vector<byte> vData = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref searchSpace, index));

                    var vMatches = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(vData, values0),
                                        Vector.Equals(vData, values1)),
                                    Vector.LessThan(vData, valuesLessThan));

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
                    nLength = (IntPtr)(length - (int)(byte*)index);
                    goto SequentialScan;
                }
            }
            return -1;
        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateFirstFoundByte(ulong match)
        {
            // Flag least significant power of two bit
            var powerOfTwoFlag = match ^ (match - 1);
            // Shift all powers of two into the high byte and extract
            return (int)((powerOfTwoFlag * XorPowerOfTwoToHighByte) >> 57);
        }

        private const ulong XorPowerOfTwoToHighByte = (0x07ul |
                                               0x06ul << 8 |
                                               0x05ul << 16 |
                                               0x04ul << 24 |
                                               0x03ul << 32 |
                                               0x02ul << 40 |
                                               0x01ul << 48) + 1;
    }
}
