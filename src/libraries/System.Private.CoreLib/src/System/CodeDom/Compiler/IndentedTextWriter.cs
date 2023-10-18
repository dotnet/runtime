// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.CodeDom.Compiler
{
    public class IndentedTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        private readonly string _tabString;
        private int _indentLevel;
        private bool _tabsPending;

        public const string DefaultTabString = "    ";

        public IndentedTextWriter(TextWriter writer) : this(writer, DefaultTabString) { }

        public IndentedTextWriter(TextWriter writer, string tabString) : base(CultureInfo.InvariantCulture)
        {
            ArgumentNullException.ThrowIfNull(writer);

            _writer = writer;
            _tabString = tabString;
            _tabsPending = true;
        }

        public override Encoding Encoding => _writer.Encoding;

        [AllowNull]
        public override string NewLine
        {
            get { return _writer.NewLine; }
            set { _writer.NewLine = value; }
        }

        public int Indent
        {
            get { return _indentLevel; }
            set { _indentLevel = Math.Max(value, 0); }
        }

        public TextWriter InnerWriter => _writer;

        public override void Close() => _writer.Close();

        /// <inheritdoc/>
        public override ValueTask DisposeAsync() => _writer.DisposeAsync();

        public override void Flush() => _writer.Flush();

        /// <inheritdoc/>
        public override Task FlushAsync() => _writer.FlushAsync();

        /// <summary>
        /// Clears all buffers for this <see cref="IndentedTextWriter"/> asynchronously and causes any buffered data to be
        /// written to the underlying device.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous flush operation.</returns>
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
            GetType() != typeof(IndentedTextWriter) ? FlushAsync() :
            _writer.FlushAsync(cancellationToken);

        protected virtual void OutputTabs()
        {
            if (_tabsPending)
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    _writer.Write(_tabString);
                }
                _tabsPending = false;
            }
        }

        /// <summary>
        /// Asynchronously outputs tabs to the underlying <see cref="TextWriter"/> based on the current <see cref="Indent"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual async Task OutputTabsAsync()
        {
            if (_tabsPending)
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    await _writer.WriteAsync(_tabString).ConfigureAwait(false);
                }
                _tabsPending = false;
            }
        }

        public override void Write(string? s)
        {
            OutputTabs();
            _writer.Write(s);
        }

        public override void Write(bool value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(char value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(char[]? buffer)
        {
            OutputTabs();
            _writer.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.Write(buffer, index, count);
        }

        public override void Write(double value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(float value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(int value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(long value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write(object? value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        {
            OutputTabs();
            _writer.Write(format, arg0);
        }

        public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        {
            OutputTabs();
            _writer.Write(format, arg0, arg1);
        }

        public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
        {
            OutputTabs();
            _writer.Write(format, arg);
        }

        /// <summary>
        /// Asynchronously writes the specified <see cref="char"/> to the underlying <see cref="TextWriter"/>, inserting
        /// tabs at the start of every line.
        /// </summary>
        /// <param name="value">The <see cref="char"/> to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteAsync(char value)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteAsync(value).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes the specified number of <see cref="char"/>s from the specified buffer
        /// to the underlying <see cref="TextWriter"/>, starting at the specified index, and outputting tabs at the
        /// start of every new line.
        /// </summary>
        /// <param name="buffer">The array to write from.</param>
        /// <param name="index">Index in the array to stort writing at.</param>
        /// <param name="count">The number of characters to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteAsync(buffer, index, count).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes the specified string to the underlying <see cref="TextWriter"/>, inserting tabs at the
        /// start of every line.
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteAsync(string? value)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteAsync(value).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes the specified characters to the underlying <see cref="TextWriter"/>, inserting tabs at the
        /// start of every line.
        /// </summary>
        /// <param name="buffer">The characters to write.</param>
        /// <param name="cancellationToken">Token for canceling the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously writes the contents of the specified <see cref="StringBuilder"/> to the underlying <see cref="TextWriter"/>, inserting tabs at the
        /// start of every line.
        /// </summary>
        /// <param name="value">The text to write.</param>
        /// <param name="cancellationToken">Token for canceling the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
        }

        public void WriteLineNoTabs(string? s)
        {
            _writer.WriteLine(s);
        }

        /// <summary>
        /// Asynchronously writes the specified string to the underlying <see cref="TextWriter"/> without inserting tabs.
        /// </summary>
        /// <param name="s">The string to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task WriteLineNoTabsAsync(string? s)
        {
            return _writer.WriteLineAsync(s);
        }

        public override void WriteLine(string? s)
        {
            OutputTabs();
            _writer.WriteLine(s);
            _tabsPending = true;
        }

        public override void WriteLine()
        {
            OutputTabs();
            _writer.WriteLine();
            _tabsPending = true;
        }

        public override void WriteLine(bool value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(char value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(char[]? buffer)
        {
            OutputTabs();
            _writer.WriteLine(buffer);
            _tabsPending = true;
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.WriteLine(buffer, index, count);
            _tabsPending = true;
        }

        public override void WriteLine(double value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(float value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(int value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(long value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine(object? value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0);
            _tabsPending = true;
        }

        public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0, arg1);
            _tabsPending = true;
        }

        public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
        {
            OutputTabs();
            _writer.WriteLine(format, arg);
            _tabsPending = true;
        }

        [CLSCompliant(false)]
        public override void WriteLine(uint value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <inheritdoc/>
        public override async Task WriteLineAsync()
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync().ConfigureAwait(false);
            _tabsPending = true;
        }

        /// <summary>
        /// Asynchronously writes the specified <see cref="char"/> to the underlying <see cref="TextWriter"/> followed by a line terminator, inserting tabs
        /// at the start of every line.
        /// </summary>
        /// <param name="value">The character to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteLineAsync(char value)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(value).ConfigureAwait(false);
            _tabsPending = true;
        }

        /// <summary>
        /// Asynchronously writes the specified number of characters from the specified buffer followed by a line terminator,
        /// to the underlying <see cref="TextWriter"/>, starting at the specified index within the buffer, inserting tabs at the start of every line.
        /// </summary>
        /// <param name="buffer">The buffer containing characters to write.</param>
        /// <param name="index">The index within the buffer to start writing at.</param>
        /// <param name="count">The number of characters to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteLineAsync(char[] buffer, int index, int count)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(buffer, index, count).ConfigureAwait(false);
            _tabsPending = true;
        }

        /// <summary>
        /// Asynchronously writes the specified string followed by a line terminator to the underlying <see cref="TextWriter"/>, inserting
        /// tabs at the start of every line.
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteLineAsync(string? value)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(value).ConfigureAwait(false);
            _tabsPending = true;
        }

        /// <summary>
        /// Asynchronously writes the specified characters followed by a line terminator to the underlying <see cref="TextWriter"/>, inserting
        /// tabs at the start of every line.
        /// </summary>
        /// <param name="buffer">The characters to write.</param>
        /// <param name="cancellationToken">Token for canceling the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(buffer, cancellationToken).ConfigureAwait(false);
            _tabsPending = true;
        }

        /// <summary>
        /// Asynchronously writes the contents of the specified <see cref="StringBuilder"/> followed by a line terminator to the
        /// underlying <see cref="TextWriter"/>, inserting tabs at the start of every line.
        /// </summary>
        /// <param name="value">The text to write.</param>
        /// <param name="cancellationToken">Token for canceling the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
        {
            await OutputTabsAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(value, cancellationToken).ConfigureAwait(false);
            _tabsPending = true;
        }
    }
}
