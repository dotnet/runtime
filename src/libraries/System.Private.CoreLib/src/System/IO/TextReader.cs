// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // This abstract base class represents a reader that can read a sequential
    // stream of characters.  This is not intended for reading bytes -
    // there are methods on the Stream class to read bytes.
    // A subclass must minimally implement the Peek() and Read() methods.
    //
    // This class is intended for character input, not bytes.
    // There are methods on the Stream class for reading bytes.
    public abstract partial class TextReader : MarshalByRefObject, IDisposable
    {
        // Create our own instance to avoid static field initialization order problems on Mono.
        public static readonly TextReader Null = new StreamReader.NullStreamReader();

        protected TextReader() { }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        // Returns the next available character without actually reading it from
        // the input stream. The current position of the TextReader is not changed by
        // this operation. The returned value is -1 if no further characters are
        // available.
        //
        // This default method simply returns -1.
        //
        public virtual int Peek()
        {
            return -1;
        }

        // Reads the next character from the input stream. The returned value is
        // -1 if no further characters are available.
        //
        // This default method simply returns -1.
        //
        public virtual int Read()
        {
            return -1;
        }

        // Reads a block of characters. This method will read up to
        // count characters from this TextReader into the
        // buffer character array starting at position
        // index. Returns the actual number of characters read.
        //
        public virtual int Read(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            int n;
            for (n = 0; n < count; n++)
            {
                int ch = Read();
                if (ch == -1) break;
                buffer[index + n] = (char)ch;
            }

            return n;
        }

        // Reads a span of characters. This method will read up to
        // count characters from this TextReader into the
        // span of characters Returns the actual number of characters read.
        //
        public virtual int Read(Span<char> buffer)
        {
            char[] array = ArrayPool<char>.Shared.Rent(buffer.Length);

            try
            {
                int numRead = Read(array, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException(SR.IO_InvalidReadLength);
                }
                new Span<char>(array, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        // Reads all characters from the current position to the end of the
        // TextReader, and returns them as one string.
        public virtual string ReadToEnd()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while ((len = Read(chars, 0, chars.Length)) != 0)
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
        }

        // Blocking version of read.  Returns only when count
        // characters have been read or the end of the file was reached.
        //
        public virtual int ReadBlock(char[] buffer, int index, int count)
        {
            int i, n = 0;
            do
            {
                n += (i = Read(buffer, index + n, count - n));
            } while (i > 0 && n < count);
            return n;
        }

        // Blocking version of read for span of characters.  Returns only when count
        // characters have been read or the end of the file was reached.
        //
        public virtual int ReadBlock(Span<char> buffer)
        {
            char[] array = ArrayPool<char>.Shared.Rent(buffer.Length);

            try
            {
                int numRead = ReadBlock(array, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException(SR.IO_InvalidReadLength);
                }
                new Span<char>(array, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        public virtual string? ReadLine()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int ch = Read();
                if (ch == -1) break;
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && Peek() == '\n')
                    {
                        Read();
                    }

                    return sb.ToString();
                }
                sb.Append((char)ch);
            }
            if (sb.Length > 0)
            {
                return sb.ToString();
            }

            return null;
        }

        #region Task based Async APIs
        public virtual Task<string?> ReadLineAsync() => ReadLineCoreAsync(default);

        /// <summary>
        /// Reads a line of characters asynchronously and returns the data as a string.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A value task that represents the asynchronous read operation. The value of the <c>TResult</c>
        /// parameter contains the next line from the text reader, or is <see langword="null" /> if all of the characters have been read.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The text reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        /// <remarks>
        /// <para>The <see cref="TextReader"/> class is an abstract class. Therefore, you do not instantiate it in
        /// your code. For an example of using the <see cref="ReadLineAsync(CancellationToken)"/> method, see the
        /// <see cref="StreamReader.ReadLineAsync(CancellationToken)"/> method.</para>
        /// <para>If the current <see cref="TextReader"/> represents the standard input stream returned by
        /// the <c>Console.In</c> property, the <see cref="ReadLineAsync(CancellationToken)"/> method
        /// executes synchronously rather than asynchronously.</para>
        /// </remarks>
        public virtual ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
            new ValueTask<string?>(ReadLineCoreAsync(cancellationToken));

        private Task<string?> ReadLineCoreAsync(CancellationToken cancellationToken) =>
            Task<string?>.Factory.StartNew(static state => ((TextReader)state!).ReadLine(), this,
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        public virtual Task<string> ReadToEndAsync() => ReadToEndAsync(default);

        /// <summary>
        /// Reads all characters from the current position to the end of the text reader asynchronously and returns them as one string.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains
        /// a string with the characters from the current position to the end of the text reader.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters is larger than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="ObjectDisposedException">The text reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        /// <remarks>
        /// <para>The <see cref="TextReader"/> class is an abstract class. Therefore, you do not instantiate it in
        /// your code. For an example of using the <see cref="ReadToEndAsync(CancellationToken)"/> method, see the
        /// <see cref="StreamReader.ReadToEndAsync(CancellationToken)"/> method.</para>
        /// </remarks>
        public virtual async Task<string> ReadToEndAsync(CancellationToken cancellationToken)
        {
            var sb = new StringBuilder(4096);
            char[] chars = ArrayPool<char>.Shared.Rent(4096);
            try
            {
                int len;
                while ((len = await ReadAsyncInternal(chars, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    sb.Append(chars, 0, len);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
            }
            return sb.ToString();
        }

        public virtual Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return ReadAsyncInternal(new Memory<char>(buffer, index, count), default).AsTask();
        }

        public virtual ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
            new ValueTask<int>(MemoryMarshal.TryGetArray(buffer, out ArraySegment<char> array) ?
                ReadAsync(array.Array!, array.Offset, array.Count) :
                Task<int>.Factory.StartNew(static state =>
                {
                    var t = (TupleSlim<TextReader, Memory<char>>)state!;
                    return t.Item1.Read(t.Item2.Span);
                }, new TupleSlim<TextReader, Memory<char>>(this, buffer), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));

        internal virtual ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken) =>
            new ValueTask<int>(Task<int>.Factory.StartNew(static state =>
            {
                var t = (TupleSlim<TextReader, Memory<char>>)state!;
                return t.Item1.Read(t.Item2.Span);
            }, new TupleSlim<TextReader, Memory<char>>(this, buffer), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));

        public virtual Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return ReadBlockAsyncInternal(new Memory<char>(buffer, index, count), default).AsTask();
        }

        public virtual ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
            new ValueTask<int>(MemoryMarshal.TryGetArray(buffer, out ArraySegment<char> array) ?
                ReadBlockAsync(array.Array!, array.Offset, array.Count) :
                Task<int>.Factory.StartNew(static state =>
                {
                    var t = (TupleSlim<TextReader, Memory<char>>)state!;
                    return t.Item1.ReadBlock(t.Item2.Span);
                }, new TupleSlim<TextReader, Memory<char>>(this, buffer), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));

        internal async ValueTask<int> ReadBlockAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken)
        {
            int n = 0, i;
            do
            {
                i = await ReadAsyncInternal(buffer.Slice(n), cancellationToken).ConfigureAwait(false);
                n += i;
            } while (i > 0 && n < buffer.Length);

            return n;
        }
        #endregion

        public static TextReader Synchronized(TextReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader is SyncTextReader ? reader : new SyncTextReader(reader);
        }

        internal sealed class SyncTextReader : TextReader
        {
            internal readonly TextReader _in;

            internal SyncTextReader(TextReader t)
            {
                _in = t;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Close() => _in.Close();

            [MethodImpl(MethodImplOptions.Synchronized)]
            protected override void Dispose(bool disposing)
            {
                // Explicitly pick up a potentially methodimpl'ed Dispose
                if (disposing)
                    ((IDisposable)_in).Dispose();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override int Peek() => _in.Peek();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override int Read() => _in.Read();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override int Read(char[] buffer, int index, int count) => _in.Read(buffer, index, count);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override int ReadBlock(char[] buffer, int index, int count) => _in.ReadBlock(buffer, index, count);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override string? ReadLine() => _in.ReadLine();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override string ReadToEnd() => _in.ReadToEnd();

            //
            // On SyncTextReader all APIs should run synchronously, even the async ones.
            //

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<string?> ReadLineAsync() => Task.FromResult(ReadLine());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
                => cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<string?>(cancellationToken) : new ValueTask<string?>(ReadLine());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
                => cancellationToken.IsCancellationRequested ? Task.FromCanceled<string>(cancellationToken) : Task.FromResult(ReadToEnd());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
            {
                ArgumentNullException.ThrowIfNull(buffer);

                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                return Task.FromResult(ReadBlock(buffer, index, count));
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                ArgumentNullException.ThrowIfNull(buffer);

                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                return Task.FromResult(Read(buffer, index, count));
            }
        }
    }
}
