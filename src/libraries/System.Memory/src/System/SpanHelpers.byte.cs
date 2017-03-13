// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if !netstandard10
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
            for (;;)
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

        public static unsafe int IndexOf(ref byte searchSpace, byte value, int length)
        {
            Debug.Assert(length >= 0);

            IntPtr index = (IntPtr)0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
            while (length >= 8)
            {
                length -= 8;

                if (value == Unsafe.Add(ref searchSpace, index))
                    goto Found;
                if (value == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (value == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (value == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;
                if (value == Unsafe.Add(ref searchSpace, index + 4))
                    goto Found4;
                if (value == Unsafe.Add(ref searchSpace, index + 5))
                    goto Found5;
                if (value == Unsafe.Add(ref searchSpace, index + 6))
                    goto Found6;
                if (value == Unsafe.Add(ref searchSpace, index + 7))
                    goto Found7;

                index += 8;
            }

            if (length >= 4)
            {
                length -= 4;

                if (value == Unsafe.Add(ref searchSpace, index))
                    goto Found;
                if (value == Unsafe.Add(ref searchSpace, index + 1))
                    goto Found1;
                if (value == Unsafe.Add(ref searchSpace, index + 2))
                    goto Found2;
                if (value == Unsafe.Add(ref searchSpace, index + 3))
                    goto Found3;

                index += 4;
            }

            while (length > 0)
            {
                if (value == Unsafe.Add(ref searchSpace, index))
                    goto Found;

                index += 1;
                length--;
            }
            return -1;

        Found: // Workaround for https://github.com/dotnet/coreclr/issues/9692
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

#if !netstandard10
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

        NotEqual: // Workaround for https://github.com/dotnet/coreclr/issues/9692
            return false;
        }
    }
}
