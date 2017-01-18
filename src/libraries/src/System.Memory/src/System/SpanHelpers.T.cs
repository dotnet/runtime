// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class SpanHelpers
    {
        public static int IndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength)
            where T : struct, IEquatable<T>
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            T valueHead = value;
            ref T valueTail = ref Unsafe.Add(ref value, 1);
            int valueTailLength = valueLength - 1;

            int index = 0;
            for (;;)
            {
                Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    return -1;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = IndexOf(ref Unsafe.Add(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
                if (relativeIndex == -1)
                    return -1;
                index += relativeIndex;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(ref Unsafe.Add(ref searchSpace, index + 1), ref valueTail, valueTailLength))
                    return index;  // The tail matched. Return a successful find.

                index++;
            }
        }

        public static int IndexOf<T>(ref T searchSpace, T value, int length)
            where T : struct, IEquatable<T>
        {
            Debug.Assert(length >= 0);

            int index = -1;
            int remainingLength = length;
            while (remainingLength >= 8)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;

                remainingLength -= 8;
            }

            while (remainingLength >= 4)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;

                remainingLength -= 4;
            }

            while (remainingLength > 0)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, ++index)))
                    return index;

                remainingLength--;
            }
            return -1;
        }

        public static bool SequenceEqual<T>(ref T first, ref T second, int length)
            where T : struct, IEquatable<T>
        {
            Debug.Assert(length >= 0);

            if (Unsafe.AreSame(ref first, ref second))
                return true;

            int index = 0;
            while (length >= 8)
            {
                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                length -= 8;
            }

            while (length >= 4)
            {
                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;

                length -= 4;
            }

            while (length > 0)
            {
                if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index)))
                    return false;
                index++;
                length--;
            }

            return true;
        }
    }
}
