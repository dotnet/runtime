// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System.IO
{
    // This abstract base class represents a reader that can read a sequential
    // stream of characters.  This is not intended for reading bytes -
    // there are methods on the Stream class to read bytes.
    // A subclass must minimally implement the Peek() and Read() methods.
    //
    // This class is intended for character input, not bytes.  
    // There are methods on the Stream class for reading bytes. 
    public abstract partial class TextReader : IDisposable
    {
        public static TextReader Synchronized(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return reader is SyncTextReader ? reader : new SyncTextReader(reader);
        }

        [Serializable]
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
            public override string ReadLine() => _in.ReadLine();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override string ReadToEnd() => _in.ReadToEnd();

            //
            // On SyncTextReader all APIs should run synchronously, even the async ones.
            //

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<string> ReadLineAsync() => Task.FromResult(ReadLine());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                return Task.FromResult(ReadBlock(buffer, index, count));
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                return Task.FromResult(Read(buffer, index, count));
            }
        }
    }
}
