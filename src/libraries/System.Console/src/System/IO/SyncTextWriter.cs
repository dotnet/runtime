// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    /* SyncTextWriter intentionally locks on itself rather than a private lock object.
     * This is done to synchronize different console writers.
     * For example - colored console writers can be synchronized with non-colored
     * writers by locking on Console.On (Issue#2855).
     */
    internal sealed class SyncTextWriter : TextWriter, IDisposable
    {
        internal readonly TextWriter _out;

        internal static SyncTextWriter GetSynchronizedTextWriter(TextWriter writer)
        {
            Debug.Assert(writer != null);
            return writer as SyncTextWriter ??
                new SyncTextWriter(writer);
        }

        internal SyncTextWriter(TextWriter t)
            : base(t.FormatProvider)
        {
            _out = t;
        }

        public override Encoding Encoding
        {
            get { return _out.Encoding; }
        }

        public override IFormatProvider FormatProvider
        {
            get { return _out.FormatProvider; }
        }

        public override String NewLine
        {
            get { lock (this) { return _out.NewLine; } }
            set { lock (this) { _out.NewLine = value; } }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    _out.Dispose();
                }
            }
        }

        public override void Flush()
        {
            lock (this)
            {
                _out.Flush();
            }
        }

        public override void Write(char value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(char[] buffer)
        {
            lock (this)
            {
                _out.Write(buffer);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            lock (this)
            {
                _out.Write(buffer, index, count);
            }
        }

        public override void Write(bool value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(int value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(uint value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(long value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(ulong value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(float value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(double value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(Decimal value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(String value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(Object value)
        {
            lock (this)
            {
                _out.Write(value);
            }
        }

        public override void Write(String format, Object[] arg)
        {
            lock (this)
            {
                _out.Write(format, arg);
            }
        }

        public override void WriteLine()
        {
            lock (this)
            {
                _out.WriteLine();
            }
        }

        public override void WriteLine(char value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(decimal value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(char[] buffer)
        {
            lock (this)
            {
                _out.WriteLine(buffer);
            }
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            lock (this)
            {
                _out.WriteLine(buffer, index, count);
            }
        }

        public override void WriteLine(bool value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(int value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(uint value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(long value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(ulong value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(float value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(double value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(String value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(Object value)
        {
            lock (this)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(String format, Object[] arg)
        {
            lock (this)
            {
                _out.WriteLine(format, arg);
            }
        }

        //
        // On SyncTextWriter all APIs should run synchronously, even the async ones.
        //

        public override Task WriteAsync(char value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(String value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(String value)
        {
            WriteLine(value);
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            WriteLine(buffer, index, count);
            return Task.CompletedTask;
        }

        public override Task FlushAsync()
        {
            Flush();
            return Task.CompletedTask;
        }
    }
}
