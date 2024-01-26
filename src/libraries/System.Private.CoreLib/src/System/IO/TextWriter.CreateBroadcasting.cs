// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract partial class TextWriter
    {
        /// <summary>
        /// Creates an instance of <see cref="TextWriter"/> that writes supplied inputs to each of the writers in <paramref name="writers"/>.
        /// </summary>
        /// <param name="writers">The <see cref="TextWriter"/> instances to which all operations should be broadcast (multiplexed).</param>
        /// <returns>
        /// An instance of <see cref="TextWriter"/> that writes supplied inputs to each of the writers in <paramref name="writers"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="writers"/> is <see langword="null"/> or it contains a <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The resulting instance will delegate each operation to each of the writers in <paramref name="writers"/>.
        /// For example, calling <see cref="Write(char)"/> will write the specified char to each writer, one after the
        /// other. The writers will be written to in the same order as they are specified in <paramref name="writers"/>.
        /// An exception from the operation on one writer will prevent the operation from being performed on subsequent writers.
        /// </para>
        /// <para>
        /// <see cref="Encoding"/> and <see cref="FormatProvider"/> will return the corresponding object from first writer
        /// in <paramref name="writers"/>.
        /// </para>
        /// </remarks>
        public static TextWriter CreateBroadcasting(params TextWriter[] writers)
        {
            ArgumentNullException.ThrowIfNull(writers);

            return writers.Length != 0 ?
                new BroadcastingTextWriter([.. writers]) :
                Null;
        }

        private sealed class BroadcastingTextWriter : TextWriter
        {
            private readonly TextWriter[] _writers;

            public BroadcastingTextWriter(TextWriter[] writers)
            {
                Debug.Assert(writers is { Length: > 0 });
                foreach (TextWriter writer in writers)
                {
                    ArgumentNullException.ThrowIfNull(writer, nameof(writers));
                }

                _writers = writers;
            }

            public override Encoding Encoding => _writers[0].Encoding;

            public override IFormatProvider FormatProvider => _writers[0].FormatProvider;

            [AllowNull]
            public override string NewLine
            {
                get => base.NewLine;
                set
                {
                    base.NewLine = value;
                    foreach (TextWriter writer in _writers)
                    {
                        writer.NewLine = value;
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    foreach (TextWriter writer in _writers)
                    {
                        writer.Dispose();
                    }
                }
            }

            public override async ValueTask DisposeAsync()
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.DisposeAsync().ConfigureAwait(false);
                }
            }

            public override void Flush()
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Flush();
                }
            }

            public override async Task FlushAsync()
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            public override void Write(bool value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(char value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(buffer, index, count);
                }
            }

            public override void Write(char[]? buffer)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(buffer);
                }
            }

            public override void Write(decimal value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(double value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(int value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(long value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(ReadOnlySpan<char> buffer)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(buffer);
                }
            }

            public override void Write(uint value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(ulong value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(float value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(string? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(object? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write(StringBuilder? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(value);
                }
            }

            public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(format, arg0);
                }
            }

            public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(format, arg0, arg1);
                }
            }

            public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(format, arg0, arg1, arg2);
                }
            }

            public override void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.Write(format, arg);
                }
            }

            public override void WriteLine()
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine();
                }
            }

            public override void WriteLine(char value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(char[]? buffer)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(buffer);
                }
            }

            public override void WriteLine(char[] buffer, int index, int count)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(buffer, index, count);
                }
            }

            public override void WriteLine(ReadOnlySpan<char> buffer)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(buffer);
                }
            }

            public override void WriteLine(bool value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(int value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(uint value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(long value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(ulong value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(float value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(double value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(decimal value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(string? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(StringBuilder? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine(object? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(value);
                }
            }

            public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(format, arg0);
                }
            }

            public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(format, arg0, arg1);
                }
            }

            public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(format, arg0, arg1, arg2);
                }
            }

            public override void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arg)
            {
                foreach (TextWriter writer in _writers)
                {
                    writer.WriteLine(format, arg);
                }
            }

            public override async Task WriteAsync(char value)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteAsync(value).ConfigureAwait(false);
                }
            }

            public override async Task WriteAsync(string? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteAsync(value).ConfigureAwait(false);
                }
            }

            public override async Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }

            public override async Task WriteAsync(char[] buffer, int index, int count)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteAsync(buffer, index, count).ConfigureAwait(false);
                }
            }

            public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync(char value)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync(value).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync(string? value)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync(value).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync(char[] buffer, int index, int count)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync(buffer, index, count).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
            }

            public override async Task WriteLineAsync()
            {
                foreach (TextWriter writer in _writers)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
