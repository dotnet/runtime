// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace SuperPmi;

// Builds byte strings. Recorded names are kept as their raw bytes, so that their hashes don't depend on decoding them.
public sealed class ByteBuilder
{
    private byte[] _buffer = new byte[256];

    public int Length { get; private set; }

    public ReadOnlySpan<byte> AsSpan() => _buffer.AsSpan(0, Length);

    public byte[] ToArray() => AsSpan().ToArray();

    public override string ToString() => Encoding.UTF8.GetString(AsSpan());

    public ByteBuilder Append(ReadOnlySpan<byte> bytes)
    {
        EnsureCapacity(Length + bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(Length));
        Length += bytes.Length;
        return this;
    }

    public ByteBuilder Append(string text)
    {
        int max = Encoding.UTF8.GetMaxByteCount(text.Length);
        EnsureCapacity(Length + max);
        Length += Encoding.UTF8.GetBytes(text, _buffer.AsSpan(Length));
        return this;
    }

    public void Truncate(int length) => Length = Math.Min(Length, length);

    private void EnsureCapacity(int capacity)
    {
        if (capacity > _buffer.Length)
            Array.Resize(ref _buffer, Math.Max(capacity, _buffer.Length * 2));
    }
}
