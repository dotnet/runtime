// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    internal ref partial struct ValueStringBuilder : IStringBuilderInternal
    {
        Span<char> IStringBuilderInternal.RemainingCurrentChunk => _chars.Slice(_pos);
        void IStringBuilderInternal.UnsafeGrow(int size) => _pos += size;

        internal void AppendFormat(IFormatProvider? provider, string format, ReadOnlySpan<object?> args)
            => StringBuilderInternal.AppendFormatHelper(ref this, provider, format, args);
    }
}
