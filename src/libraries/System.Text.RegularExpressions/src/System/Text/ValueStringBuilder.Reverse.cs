// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        public void AppendReversed(ReadOnlySpan<char> value)
        {
            Span<char> span = AppendSpan(value.Length);
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = value[value.Length - i - 1];
            }
        }

        public void Reverse()
        {
            _chars.Slice(0, _pos).Reverse();
        }
    }
}
