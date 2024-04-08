// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(Rune rune)
        {
            int pos = _pos;
            Span<char> chars = _chars;
            if ((uint)(pos + 1) < (uint)chars.Length && (uint)pos < (uint)chars.Length)
            {
                if (rune.Value <= 0xFFFF)
                {
                    chars[pos] = (char)rune.Value;
                    _pos = pos + 1;
                }
                else
                {
                    chars[pos] = (char)((rune.Value + ((0xD800u - 0x40u) << 10)) >> 10);
                    chars[pos + 1] = (char)((rune.Value & 0x3FFu) + 0xDC00u);
                    _pos = pos + 2;
                }
            }
            else
            {
                GrowAndAppend(rune);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(Rune rune)
        {
            if (rune.Value <= 0xFFFF)
            {
                Append((char)rune.Value);
            }
            else
            {
                Grow(2);
                Append(rune);
            }
        }
    }
}
