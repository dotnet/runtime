// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace System.IO
{
    // This abstract base class represents a writer that can write a sequential
    // stream of characters. A subclass must minimally implement the 
    // Write(char) method.
    //
    // This class is intended for character output, not bytes.  
    // There are methods on the Stream class for writing bytes. 
    public abstract partial class TextWriter : IDisposable
    {
        public static TextWriter Synchronized(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            return writer is SyncTextWriter ? writer : new SyncTextWriter(writer);
        }

        [Serializable]
        internal sealed class SyncTextWriter : TextWriter, IDisposable
        {
            private readonly TextWriter _out;

            internal SyncTextWriter(TextWriter t) : base(t.FormatProvider)
            {
                _out = t;
            }

            public override Encoding Encoding =>  _out.Encoding;

            public override IFormatProvider FormatProvider => _out.FormatProvider;

            public override string NewLine
            {
                [MethodImpl(MethodImplOptions.Synchronized)]
                get { return _out.NewLine; }
                [MethodImpl(MethodImplOptions.Synchronized)]
                set { _out.NewLine = value; }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Close() => _out.Close();

            [MethodImpl(MethodImplOptions.Synchronized)]
            protected override void Dispose(bool disposing)
            {
                // Explicitly pick up a potentially methodimpl'ed Dispose
                if (disposing)
                    ((IDisposable)_out).Dispose();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Flush() => _out.Flush();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(char value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(char[] buffer) => _out.Write(buffer);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(char[] buffer, int index, int count) => _out.Write(buffer, index, count);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(bool value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(int value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(uint value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(long value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(ulong value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(float value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(double value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(Decimal value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(string value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(object value) => _out.Write(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(string format, object arg0) => _out.Write(format, arg0);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(string format, object arg0, object arg1) => _out.Write(format, arg0, arg1);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(string format, object arg0, object arg1, object arg2) => _out.Write(format, arg0, arg1, arg2);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void Write(string format, object[] arg) => _out.Write(format, arg);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine() => _out.WriteLine();

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(char value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(decimal value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(char[] buffer) => _out.WriteLine(buffer);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(char[] buffer, int index, int count) => _out.WriteLine(buffer, index, count);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(bool value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(int value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(uint value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(long value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(ulong value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(float value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(double value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(string value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(object value) => _out.WriteLine(value);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(string format, object arg0) => _out.WriteLine(format, arg0);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(string format, object arg0, object arg1) => _out.WriteLine(format, arg0, arg1);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(string format, object arg0, object arg1, object arg2) => _out.WriteLine(format, arg0, arg1, arg2);

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void WriteLine(string format, object[] arg) => _out.WriteLine(format, arg);

            //
            // On SyncTextWriter all APIs should run synchronously, even the async ones.
            //

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteAsync(char value)
            {
                Write(value);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteAsync(string value)
            {
                Write(value);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteAsync(char[] buffer, int index, int count)
            {
                Write(buffer, index, count);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteLineAsync(char value)
            {
                WriteLine(value);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteLineAsync(string value)
            {
                WriteLine(value);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task WriteLineAsync(char[] buffer, int index, int count)
            {
                WriteLine(buffer, index, count);
                return Task.CompletedTask;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override Task FlushAsync()
            {
                Flush();
                return Task.CompletedTask;
            }
        }
    }
}
