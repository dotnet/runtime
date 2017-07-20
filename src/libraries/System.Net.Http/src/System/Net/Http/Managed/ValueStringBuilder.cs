// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    internal struct ValueStringBuilder
    {
        public char[] Chars;
        public int Length;

        public ValueStringBuilder(int initialCapacity)
        {
            Chars = new char[initialCapacity];
            Length = 0;
        }

        public void Append(char c)
        {
            if (Length == Chars.Length)
            {
                Grow();
            }
            Chars[Length++] = c;
        }

        private void Grow() => Array.Resize(ref Chars, Chars.Length * 2);

        public void Clear() => Length = 0;

        public override string ToString() => new string(Chars, 0, Length);
    }
}
