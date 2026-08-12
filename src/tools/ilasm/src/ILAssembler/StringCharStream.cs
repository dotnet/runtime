// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler;

internal sealed class StringCharStream : ICharStream
{
    private readonly string _text;
    private int _position;

    internal StringCharStream(string text, string? sourceName = null)
    {
        _text = text;
        SourceName = sourceName ?? string.Empty;
    }

    public int Index => _position;

    public int Size => _text.Length;

    public string SourceName { get; }

    public void Consume()
    {
        if (_position >= _text.Length)
        {
            throw new InvalidOperationException("Cannot consume past the end of the character stream.");
        }

        _position++;
    }

    public int LA(int i)
    {
        if (i == 0)
        {
            return 0;
        }

        int offset = i < 0 ? i : i - 1;
        int position = _position + offset;
        return (uint)position >= (uint)_text.Length ? TokenConstants.EOF : _text[position];
    }

    public int Mark() => -1;

    public void Release(int marker)
    {
    }

    public void Seek(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _text.Length);
        _position = index;
    }

    public string GetText(Interval interval)
    {
        int start = Math.Max(0, interval.a);
        int end = Math.Min(_text.Length - 1, interval.b);
        return start > end ? string.Empty : _text.Substring(start, end - start + 1);
    }
}
