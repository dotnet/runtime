// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // This class implements a text reader that reads from a string.
    public class StringReader : TextReader
    {
        private string? _s;
        private int _pos;

        public StringReader(string s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            _s = s;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            _s = null;
            _pos = 0;
            base.Dispose(disposing);
        }

        // Returns the next available character without actually reading it from
        // the underlying string. The current position of the StringReader is not
        // changed by this operation. The returned value is -1 if no further
        // characters are available.
        //
        public override int Peek()
        {
            string? s = _s;
            if (s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int pos = _pos;
            if ((uint)pos < (uint)s.Length)
            {
                return s[pos];
            }

            return -1;
        }

        // Reads the next character from the underlying string. The returned value
        // is -1 if no further characters are available.
        //
        public override int Read()
        {
            string? s = _s;
            if (s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int pos = _pos;
            if ((uint)pos < (uint)s.Length)
            {
                _pos++;
                return s[pos];
            }

            return -1;
        }

        // Reads a block of characters. This method will read up to count
        // characters from this StringReader into the buffer character
        // array starting at position index. Returns the actual number of
        // characters read, or zero if the end of the string is reached.
        //
        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (_s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int n = _s.Length - _pos;
            if (n > 0)
            {
                if (n > count)
                {
                    n = count;
                }

                _s.CopyTo(_pos, buffer, index, n);
                _pos += n;
            }
            return n;
        }

        public override int Read(Span<char> buffer)
        {
            if (GetType() != typeof(StringReader))
            {
                // This overload was added after the Read(char[], ...) overload, and so in case
                // a derived type may have overridden it, we need to delegate to it, which the base does.
                return base.Read(buffer);
            }

            string? s = _s;
            if (s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int n = s.Length - _pos;
            if (n > 0)
            {
                if (n > buffer.Length)
                {
                    n = buffer.Length;
                }

                s.AsSpan(_pos, n).CopyTo(buffer);
                _pos += n;
            }

            return n;
        }

        public override int ReadBlock(Span<char> buffer) => Read(buffer);

        public override string ReadToEnd()
        {
            string? s = _s;
            if (s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int pos = _pos;
            _pos = s.Length;

            if (pos != 0)
            {
                s = s.Substring(pos);
            }

            return s;
        }

        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the underlying string has been reached.
        //
        public override string? ReadLine()
        {
            string? s = _s;
            if (s == null)
            {
                ThrowObjectDisposedException_ReaderClosed();
            }

            int pos = _pos;
            if ((uint)pos >= (uint)s.Length)
            {
                return null;
            }

            ReadOnlySpan<char> remaining = s.AsSpan(pos);
            int foundLineLength = remaining.IndexOfAny('\r', '\n');
            if (foundLineLength >= 0)
            {
                string result = s.Substring(pos, foundLineLength);

                char ch = remaining[foundLineLength];
                pos += foundLineLength + 1;
                if (ch == '\r')
                {
                    if ((uint)pos < (uint)s.Length && s[pos] == '\n')
                    {
                        pos++;
                    }
                }
                _pos = pos;

                return result;
            }
            else
            {
                string result = s.Substring(pos);
                _pos = s.Length;
                return result;
            }
        }

        #region Task based Async APIs
        public override Task<string?> ReadLineAsync()
        {
            return Task.FromResult(ReadLine());
        }

        public override Task<string> ReadToEndAsync()
        {
            return Task.FromResult(ReadToEnd());
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return Task.FromResult(ReadBlock(buffer, index, count));
        }

        public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
            cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) :
            new ValueTask<int>(ReadBlock(buffer.Span));

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return Task.FromResult(Read(buffer, index, count));
        }

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
            cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) :
            new ValueTask<int>(Read(buffer.Span));
        #endregion

        [DoesNotReturn]
        private static void ThrowObjectDisposedException_ReaderClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
        }
    }
}
