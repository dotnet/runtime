// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JoinedString;

internal static class JoinedStringExtensions
{
    public static JoinedStringEnumerable Join(this IEnumerable<string> list, string separator)
    => JoinedStringEnumerable.Join(list, separator);
    public static JoinedStringEnumerable Join<T>(this IEnumerable<T> list, string separator, Func<T, string> formatter)
    => JoinedStringEnumerable.Join(list, separator, formatter);
    public static JoinedStringEnumerable Join<T>(this IEnumerable<T> list, string separator, Func<T, int, string> formatter)
    => JoinedStringEnumerable.Join(list, separator, formatter);

    public static JoinedList<T> Join<T>(this IList<T> list, string separator)
    => new JoinedList<T>(list, separator, (item, _) => $"{item}");
    public static JoinedList<T> Join<T>(this IList<T> list, string separator, Func<T, string> formatter)
    => new JoinedList<T>(list, separator, (str, _) => formatter(str));
    public static JoinedList<T> Join<T>(this IList<T> list, string separator, Func<T, int, string> formatter)
    => new JoinedList<T>(list, separator, formatter);
}


internal sealed record JoinedList<T>(IList<T> items, string separator, Func<T, int, string> formatter)
{
    public IEnumerator<string> GetEnumerator()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (i != 0)
                yield return separator;
            yield return formatter(items[i], i);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var item in this)
        {
            sb.Append(item);
        }
        return sb.ToString();
    }
}

internal sealed class JoinedStringEnumerable : IEnumerable<string>
{
    private readonly IEnumerable<string> _values;

    private JoinedStringEnumerable(IEnumerable<string> values)
    {
        _values = values;
    }

    public static JoinedStringEnumerable Join(IEnumerable<string> values, string separator)
        => new JoinedStringEnumerable(JoinInternal(values, separator, (x, _) => x));

    public static JoinedStringEnumerable Join<T>(IEnumerable<T> values, string separator, Func<T, string> format)
        => new JoinedStringEnumerable(JoinInternal(values, separator, (x, _) => format(x)));

    public static JoinedStringEnumerable Join<T>(IEnumerable<T> values, string separator, Func<T, int, string> format)
        => new JoinedStringEnumerable(JoinInternal(values, separator, format));

    private static IEnumerable<string> JoinInternal<T>(IEnumerable<T> values, string separator, Func<T, int, string> format)
    {
        int index = 0;
        foreach (var value in values)
        {
            if (index != 0)
                yield return separator;
            yield return format(value, index++);
        }
    }

    public IEnumerator<string> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var item in this)
        {
            sb.Append(item);
        }
        return sb.ToString();
    }
}

internal sealed class JoinedStringStreamWriter : StreamWriter
{
    private string CompilerNewLine = Environment.NewLine;

    public JoinedStringStreamWriter(Stream stream) : base(stream)
    {
        NewLine = CompilerNewLine;
    }

    public JoinedStringStreamWriter(string path, bool append) : base(path, append)
    {
        NewLine = CompilerNewLine;
    }

#if NET8_0_OR_GREATER
#pragma warning disable  CA1822 // Mark members as static
#pragma warning disable  IDE0060 // Remove unused parameter
    public void Write([InterpolatedStringHandlerArgument("")] JoinedStringWriterHandler builder)
    {
        // The builder writes directly to the writer
    }
#pragma warning restore  IDE0060
#pragma warning restore  CA1822
#endif
}

#if NET8_0_OR_GREATER
[InterpolatedStringHandler]
internal ref struct JoinedStringWriterHandler
{
    private JoinedStringStreamWriter _writer;

#pragma warning disable IDE0060
    public JoinedStringWriterHandler(int literalLength, int formattedCount, JoinedStringStreamWriter writer)
    {
        writer.Flush();
        _writer = writer;
    }
#pragma warning restore IDE0060

    public void AppendLiteral(string s) => _writer.Write(s);
    public void AppendFormatted<T>(T value) => _writer.Write(value);
    public void AppendFormatted(Span<char> value) => _writer.Write(value);
    public void AppendFormatted(char[] buffer, int index, int count) => _writer.Write(buffer, index, count);
    public void AppendFormatted(string format, object? arg0, object? arg1, object? arg2) => _writer.Write(format, arg0, arg1, arg2);

    public void AppendFormatted(JoinedStringEnumerable list)
    {
        foreach (var item in list)
        {
            _writer.Write(item);
        }
    }

    public void AppendFormatted<T>(JoinedList<T> list)
    {
        foreach (var item in list)
        {
            _writer.Write(item);
        }
    }

    public override string ToString()
    {
        _writer.Flush();
        return "";
    }
}
#endif
