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
    public static ConcatenatedString Join(this IEnumerable<string> list, string separator)
    => ConcatenatedString.Join(list, separator);
    public static ConcatenatedString Join<T>(this IEnumerable<T> list, string separator, Func<T, string> formatter)
    => ConcatenatedString.Join(list, separator, formatter);
    public static ConcatenatedString Join<T>(this IEnumerable<T> list, string separator, Func<T, int, string> formatter)
    => ConcatenatedString.Join(list, separator, formatter);

    public static JoinedList<T> Join<T>(this IList<T> list, string separator)
    => new JoinedList<T>(list, separator, (item, _) => $"{item}");
    public static JoinedList<T> Join<T>(this IList<T> list, string separator, Func<T, string> formatter)
    => new JoinedList<T>(list, separator, (str, _) => formatter(str));
    public static JoinedList<T> Join<T>(this IList<T> list, string separator, Func<T, int, string> formatter)
    => new JoinedList<T>(list, separator, formatter);
}

internal sealed record JoinedList<T>(IList<T> items, string separator, Func<T, int, string> formatter) : IStringSegments
{
    public IEnumerator<string> GetEnumerator()
    {
        bool hasSeparator = !string.IsNullOrEmpty(separator);

        for (int i = 0; i < items.Count; i++)
        {
            if (hasSeparator && i != 0)
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

internal interface IStringSegments {
    public IEnumerator<string> GetEnumerator();
}

internal sealed class ConcatenatedString : IStringSegments, IEnumerable<string>
{
    private readonly IEnumerable<string> _values;

    public ConcatenatedString(IEnumerable<string> values)
    {
        _values = values;
    }

    public static ConcatenatedString Join(IEnumerable<string> values, string separator)
        => new ConcatenatedString(JoinInternal(values, separator, (x, _) => x));

    public static ConcatenatedString Join<T>(IEnumerable<T> values, string separator, Func<T, string> format)
        => new ConcatenatedString(JoinInternal(values, separator, (x, _) => format(x)));

    public static ConcatenatedString Join<T>(IEnumerable<T> values, string separator, Func<T, int, string> format)
        => new ConcatenatedString(JoinInternal(values, separator, format));

    private static IEnumerable<string> JoinInternal<T>(IEnumerable<T> values, string separator, Func<T, int, string> format)
    {
        int index = 0;
        bool hasSeparator = !string.IsNullOrEmpty(separator);

        foreach (var value in values)
        {
            if (hasSeparator && index != 0)
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
    // since we are intentionally using multi-line string writes,
    // we want to capture the compile-time new line
    private string CompileTimeNewLine = @"
";

    public JoinedStringStreamWriter(Stream stream) : base(stream)
    {
        NewLine = CompileTimeNewLine;
    }

    public JoinedStringStreamWriter(string path, bool append) : base(path, append)
    {
        NewLine = CompileTimeNewLine;
    }

#if NET
#pragma warning disable  CA1822 // Mark members as static
#pragma warning disable  IDE0060 // Remove unused parameter
    public void Write([InterpolatedStringHandlerArgument("")] StringSegmentStreamWriterHandler builder)
    {
        // The builder writes directly to the writer
    }
#pragma warning restore  IDE0060
#pragma warning restore  CA1822
#endif

    public void Write(IStringSegments list)
    {
        foreach (var item in list)
        {
            Write(item);
        }
    }

    public void WriteLine(IStringSegments list)
    {
        foreach (var item in list)
        {
            Write(item);
        }
        WriteLine();
    }
}

#if NET
[InterpolatedStringHandler]
internal ref struct StringSegmentStreamWriterHandler
{
    private JoinedStringStreamWriter _writer;

#pragma warning disable IDE0060
    public StringSegmentStreamWriterHandler(int literalLength, int formattedCount, JoinedStringStreamWriter writer)
    {
        _writer = writer;
    }
#pragma warning restore IDE0060

    public void AppendLiteral(string s) => _writer.Write(s);
    public void AppendFormatted<T>(T value) => _writer.Write(value);
    public void AppendFormatted(Span<char> value) => _writer.Write(value);
    public void AppendFormatted(char[] buffer, int index, int count) => _writer.Write(buffer, index, count);
    public void AppendFormatted(string format, object? arg0, object? arg1, object? arg2) => _writer.Write(format, arg0, arg1, arg2);

    public void AppendFormatted(IStringSegments list)
    {
        foreach (var item in list)
        {
            _writer.Write(item);
        }
    }

    public override string ToString()
    {;
        return "";
    }
}
#endif
