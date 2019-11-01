// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // This class implements a text reader that reads from a string.
    public class StringReader : TextReader
    {
        private string? _s;
        private int _pos;
        private int _length;

        public StringReader(string s)
        {
            _s = s ?? throw new ArgumentNullException(nameof(s));
            _length = s.Length;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            _s = null;
            _pos = 0;
            _length = 0;
            base.Dispose(disposing);
        }

        // Returns the next available character without actually reading it from
        // the underlying string. The current position of the StringReader is not
        // changed by this operation. The returned value is -1 if no further
        // characters are available.
        //
        public override int Peek()
        {
            if (_s == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }
            if (_pos == _length)
            {
                return -1;
            }

            return _s[_pos];
        }

        // Reads the next character from the underlying string. The returned value
        // is -1 if no further characters are available.
        //
        public override int Read()
        {
            if (_s == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }
            if (_pos == _length)
            {
                return -1;
            }

            return _s[_pos++];
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
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            int n = _length - _pos;
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

            if (_s == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            int n = _length - _pos;
            if (n > 0)
            {
                if (n > buffer.Length)
                {
                    n = buffer.Length;
                }

                _s.AsSpan(_pos, n).CopyTo(buffer);
                _pos += n;
            }

            return n;
        }

        public override int ReadBlock(Span<char> buffer) => Read(buffer);

        public override string ReadToEnd()
        {
            if (_s == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            string s;
            if (_pos == 0)
            {
                s = _s;
            }
            else
            {
                s = _s.Substring(_pos, _length - _pos);
            }

            _pos = _length;
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
            if (_s == null)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
            }

            int i = _pos;
            while (i < _length)
            {
                char ch = _s[i];
                if (ch == '\r' || ch == '\n')
                {
                    string result = _s.Substring(_pos, i - _pos);
                    _pos = i + 1;
                    if (ch == '\r' && _pos < _length && _s[_pos] == '\n')
                    {
                        _pos++;
                    }

                    return result;
                }

                i++;
            }

            if (i > _pos)
            {
                string result = _s.Substring(_pos, i - _pos);
                _pos = i;
                return result;
            }

            return null;
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
            cancellationToken.IsCancellationRequested ? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken)) :
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
            cancellationToken.IsCancellationRequested ? new ValueTask<int>(Task.FromCanceled<int>(cancellationToken)) :
            new ValueTask<int>(Read(buffer.Span));
        #endregion
    }
}
