// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Interop;

/// <summary>Writes indented source text without constructing a syntax tree.</summary>
public sealed class IndentedTextWriter
{
    private const string NewLine = "\r\n";
    private const int IndentationSize = 4;

    private readonly StringBuilder _builder = new();
    private int _indent;
    private bool _pendingCarriageReturn;

    /// <summary>Gets the number of characters written.</summary>
    public int Length => _builder.Length;

    /// <summary>Gets or sets the indentation level.</summary>
    public int Indent
    {
        get => _indent;
        set
        {
            Debug.Assert(value >= 0);
            _indent = value;
        }
    }

    /// <summary>Clears the text after all indented blocks have been closed.</summary>
    public void Clear()
    {
        Debug.Assert(Indent == 0);
        _builder.Clear();
        _pendingCarriageReturn = false;
    }

    /// <summary>Writes text, indenting nonempty lines and normalizing line endings.</summary>
    /// <param name="content">The text to write.</param>
    public void Write(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        int start = _pendingCarriageReturn && content[0] == '\n' ? 1 : 0;
        _pendingCarriageReturn = false;
        while (start < content.Length)
        {
            int end = start;
            while (end < content.Length && content[end] is not ('\r' or '\n'))
            {
                end++;
            }

            if (end != start)
            {
                WriteIndentation();
                _builder.Append(content, start, end - start);
            }

            if (end == content.Length)
            {
                break;
            }

            _builder.Append(NewLine);
            _pendingCarriageReturn = content[end] == '\r';
            start = end + 1;
            if (start < content.Length)
            {
                if (_pendingCarriageReturn && content[start] == '\n')
                {
                    start++;
                }
                _pendingCarriageReturn = false;
            }
        }
    }

    /// <summary>Writes a character.</summary>
    /// <param name="value">The character to write.</param>
    public void Write(char value)
    {
        if (value == '\n' && _pendingCarriageReturn)
        {
            _pendingCarriageReturn = false;
            return;
        }

        _pendingCarriageReturn = value == '\r';
        if (value is '\r' or '\n')
        {
            _builder.Append(NewLine);
        }
        else
        {
            WriteIndentation();
            _builder.Append(value);
        }
    }

    internal void WriteVerbatim(string content)
    {
        // Input fragments can contain raw string literals whose whitespace and line endings
        // are part of their value, rather than formatting owned by the generator.
        _builder.Append(content);
        _pendingCarriageReturn = false;
    }

    /// <summary>Writes interpolated text directly through its handler.</summary>
    /// <param name="handler">The handler that writes the interpolation.</param>
    public void Write([InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler handler)
    {
    }

    /// <summary>Writes a line ending without adding trailing indentation.</summary>
    public void WriteLine()
    {
        _builder.Append(NewLine);
        _pendingCarriageReturn = false;
    }

    /// <summary>Writes text followed by a line ending.</summary>
    /// <param name="content">The text to write.</param>
    public void WriteLine(string? content)
    {
        Write(content);
        WriteLine();
    }

    /// <summary>Writes a character followed by a line ending.</summary>
    /// <param name="value">The character to write.</param>
    public void WriteLine(char value)
    {
        Write(value);
        WriteLine();
    }

    /// <summary>Writes interpolated text followed by a line ending.</summary>
    /// <param name="handler">The handler that writes the interpolation.</param>
    public void WriteLine([InterpolatedStringHandlerArgument("")] ref WriteInterpolatedStringHandler handler)
    {
        WriteLine();
    }

    /// <summary>Opens an indented block that is closed when the returned scope is disposed.</summary>
    /// <returns>The scope that closes the block.</returns>
    public Block WriteBlock()
    {
        WriteLine('{');
        Indent++;
        return new Block(this);
    }

    /// <inheritdoc/>
    public override string ToString() => _builder.ToString();

    private void WriteIndentation()
    {
        if (_builder.Length == 0 || _builder[_builder.Length - 1] == '\n')
        {
            _builder.Append(' ', checked(Indent * IndentationSize));
        }
    }

    /// <summary>Represents an open indented block.</summary>
    public struct Block : IDisposable
    {
        private IndentedTextWriter? _writer;

        internal Block(IndentedTextWriter writer)
        {
            _writer = writer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            IndentedTextWriter? writer = _writer;
            _writer = null;
            if (writer is not null)
            {
                writer.Indent--;
                writer.WriteLine('}');
            }
        }
    }

    /// <summary>Writes interpolated values using invariant formatting.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly IndentedTextWriter _writer;

        /// <summary>Initializes a handler for compiler-generated interpolation calls.</summary>
        /// <param name="literalLength">The number of literal characters.</param>
        /// <param name="formattedCount">The number of formatted values.</param>
        /// <param name="writer">The destination writer.</param>
        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, IndentedTextWriter writer)
        {
            _writer = writer;
        }

        /// <summary>Writes a literal part of the interpolation.</summary>
        /// <param name="value">The text to write.</param>
        public void AppendLiteral(string value) => _writer.Write(value);

        /// <summary>Writes a string value.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value) => _writer.Write(value);

        /// <summary>Writes a value using invariant formatting.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted<T>(T value) => AppendFormatted(value, format: null);

        /// <summary>Writes a value using the specified invariant format.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format to apply.</param>
        public void AppendFormatted<T>(T value, string? format)
        {
            _writer.Write(value is IFormattable formattable
                ? formattable.ToString(format, CultureInfo.InvariantCulture)
                : value?.ToString());
        }
    }
}
