// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    internal sealed class SyncTextWriter : TextWriter, IDisposable
    {
        private readonly object _methodLock = new object();
        private TextWriter _out;

        internal static TextWriter GetSynchronizedTextWriter(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            Contract.Ensures(Contract.Result<TextWriter>() != null);
            Contract.EndContractBlock();

            return writer is SyncTextWriter ?
                writer :
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
            get { lock (_methodLock) { return _out.NewLine; } }
            set { lock (_methodLock) { _out.NewLine = value; } }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_methodLock)
                {
                    // Explicitly pick up a potentially methodimpl'ed Dispose
                    ((IDisposable)_out).Dispose();
                }
            }
        }

        public override void Flush()
        {
            lock (_methodLock)
            {
                _out.Flush();
            }
        }

        public override void Write(char value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(char[] buffer)
        {
            lock (_methodLock)
            {
                _out.Write(buffer);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            lock (_methodLock)
            {
                _out.Write(buffer, index, count);
            }
        }

        public override void Write(bool value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(int value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(uint value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(long value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(ulong value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(float value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(double value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(Decimal value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(String value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(Object value)
        {
            lock (_methodLock)
            {
                _out.Write(value);
            }
        }

        public override void Write(String format, Object[] arg)
        {
            lock (_methodLock)
            {
                _out.Write(format, arg);
            }
        }

        public override void WriteLine()
        {
            lock (_methodLock)
            {
                _out.WriteLine();
            }
        }

        public override void WriteLine(char value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(decimal value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(char[] buffer)
        {
            lock (_methodLock)
            {
                _out.WriteLine(buffer);
            }
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            lock (_methodLock)
            {
                _out.WriteLine(buffer, index, count);
            }
        }

        public override void WriteLine(bool value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(int value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(uint value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(long value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(ulong value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(float value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(double value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(String value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(Object value)
        {
            lock (_methodLock)
            {
                _out.WriteLine(value);
            }
        }

        public override void WriteLine(String format, Object[] arg)
        {
            lock (_methodLock)
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