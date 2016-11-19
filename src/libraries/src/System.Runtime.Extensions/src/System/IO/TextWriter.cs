// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;

namespace System.IO
{
    // This abstract base class represents a writer that can write a sequential
    // stream of characters. A subclass must minimally implement the 
    // Write(char) method.
    //
    // This class is intended for character output, not bytes.  
    // There are methods on the Stream class for writing bytes. 
    [Serializable]
    public abstract partial class TextWriter : MarshalByRefObject, IDisposable
    {
        public static readonly TextWriter Null = new NullTextWriter();

        protected char[] CoreNewLine = Environment.NewLine.ToCharArray();
        private string CoreNewLineStr = Environment.NewLine;

        // Can be null - if so, ask for the Thread's CurrentCulture every time.
        private IFormatProvider _internalFormatProvider;

        protected TextWriter()
        {
            _internalFormatProvider = null;  // Ask for CurrentCulture all the time.
        }

        protected TextWriter(IFormatProvider formatProvider)
        {
            _internalFormatProvider = formatProvider;
        }

        public virtual IFormatProvider FormatProvider
        {
            get
            {
                if (_internalFormatProvider == null)
                {
                    return CultureInfo.CurrentCulture;
                }
                else
                {
                    return _internalFormatProvider;
                }
            }
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Clears all buffers for this TextWriter and causes any buffered data to be
        // written to the underlying device. This default method is empty, but
        // descendant classes can override the method to provide the appropriate
        // functionality.
        public virtual void Flush()
        {
        }

        public abstract Encoding Encoding
        {
            get;
        }

        // Returns the line terminator string used by this TextWriter. The default line
        // terminator string is a carriage return followed by a line feed ("\r\n").
        //
        // Sets the line terminator string for this TextWriter. The line terminator
        // string is written to the text stream whenever one of the
        // WriteLine methods are called. In order for text written by
        // the TextWriter to be readable by a TextReader, only one of the following line
        // terminator strings should be used: "\r", "\n", or "\r\n".
        // 
        public virtual string NewLine
        {
            get { return CoreNewLineStr; }
            set
            {
                if (value == null)
                {
                    value = Environment.NewLine;
                }

                CoreNewLineStr = value;
                CoreNewLine = value.ToCharArray();
            }
        }


        // Writes a character to the text stream. This default method is empty,
        // but descendant classes can override the method to provide the
        // appropriate functionality.
        //
        public virtual void Write(char value)
        {
        }

        // Writes a character array to the text stream. This default method calls
        // Write(char) for each of the characters in the character array.
        // If the character array is null, nothing is written.
        //
        public virtual void Write(char[] buffer)
        {
            if (buffer != null)
            {
                Write(buffer, 0, buffer.Length);
            }
        }

        // Writes a range of a character array to the text stream. This method will
        // write count characters of data into this TextWriter from the
        // buffer character array starting at position index.
        //
        public virtual void Write(char[] buffer, int index, int count)
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

            for (int i = 0; i < count; i++) Write(buffer[index + i]);
        }

        // Writes the text representation of a boolean to the text stream. This
        // method outputs either Boolean.TrueString or Boolean.FalseString.
        //
        public virtual void Write(bool value)
        {
            Write(value ? "True" : "False");
        }

        // Writes the text representation of an integer to the text stream. The
        // text representation of the given value is produced by calling the
        // Int32.ToString() method.
        //
        public virtual void Write(int value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes the text representation of an integer to the text stream. The
        // text representation of the given value is produced by calling the
        // UInt32.ToString() method.
        //
        [CLSCompliant(false)]
        public virtual void Write(uint value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes the text representation of a long to the text stream. The
        // text representation of the given value is produced by calling the
        // Int64.ToString() method.
        //
        public virtual void Write(long value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes the text representation of an unsigned long to the text 
        // stream. The text representation of the given value is produced 
        // by calling the UInt64.ToString() method.
        //
        [CLSCompliant(false)]
        public virtual void Write(ulong value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes the text representation of a float to the text stream. The
        // text representation of the given value is produced by calling the
        // Float.toString(float) method.
        //
        public virtual void Write(float value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes the text representation of a double to the text stream. The
        // text representation of the given value is produced by calling the
        // Double.toString(double) method.
        //
        public virtual void Write(double value)
        {
            Write(value.ToString(FormatProvider));
        }

        public virtual void Write(decimal value)
        {
            Write(value.ToString(FormatProvider));
        }

        // Writes a string to the text stream. If the given string is null, nothing
        // is written to the text stream.
        //
        public virtual void Write(string value)
        {
            if (value != null)
            {
                Write(value.ToCharArray());
            }
        }

        // Writes the text representation of an object to the text stream. If the
        // given object is null, nothing is written to the text stream.
        // Otherwise, the object's ToString method is called to produce the
        // string representation, and the resulting string is then written to the
        // output stream.
        //
        public virtual void Write(object value)
        {
            if (value != null)
            {
                IFormattable f = value as IFormattable;
                if (f != null)
                {
                    Write(f.ToString(null, FormatProvider));
                }
                else
                    Write(value.ToString());
            }
        }

        // Writes out a formatted string.  Uses the same semantics as
        // String.Format.
        // 
        public virtual void Write(string format, object arg0)
        {
            Write(string.Format(FormatProvider, format, arg0));
        }

        // Writes out a formatted string.  Uses the same semantics as
        // String.Format.
        // 
        public virtual void Write(string format, object arg0, object arg1)
        {
            Write(string.Format(FormatProvider, format, arg0, arg1));
        }

        // Writes out a formatted string.  Uses the same semantics as
        // String.Format.
        // 
        public virtual void Write(string format, object arg0, object arg1, object arg2)
        {
            Write(string.Format(FormatProvider, format, arg0, arg1, arg2));
        }

        // Writes out a formatted string.  Uses the same semantics as
        // String.Format.
        // 
        public virtual void Write(string format, params object[] arg)
        {
            Write(string.Format(FormatProvider, format, arg));
        }


        // Writes a line terminator to the text stream. The default line terminator
        // is Environment.NewLine, but this value can be changed by setting the NewLine property.
        //
        public virtual void WriteLine()
        {
            Write(CoreNewLine);
        }

        // Writes a character followed by a line terminator to the text stream.
        //
        public virtual void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }

        // Writes an array of characters followed by a line terminator to the text
        // stream.
        //
        public virtual void WriteLine(char[] buffer)
        {
            Write(buffer);
            WriteLine();
        }

        // Writes a range of a character array followed by a line terminator to the
        // text stream.
        //
        public virtual void WriteLine(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            WriteLine();
        }

        // Writes the text representation of a boolean followed by a line
        // terminator to the text stream.
        //
        public virtual void WriteLine(bool value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of an integer followed by a line
        // terminator to the text stream.
        //
        public virtual void WriteLine(int value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of an unsigned integer followed by 
        // a line terminator to the text stream.
        //
        [CLSCompliant(false)]
        public virtual void WriteLine(uint value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of a long followed by a line terminator
        // to the text stream.
        //
        public virtual void WriteLine(long value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of an unsigned long followed by 
        // a line terminator to the text stream.
        //
        [CLSCompliant(false)]
        public virtual void WriteLine(ulong value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of a float followed by a line terminator
        // to the text stream.
        //
        public virtual void WriteLine(float value)
        {
            Write(value);
            WriteLine();
        }

        // Writes the text representation of a double followed by a line terminator
        // to the text stream.
        //
        public virtual void WriteLine(double value)
        {
            Write(value);
            WriteLine();
        }

        public virtual void WriteLine(decimal value)
        {
            Write(value);
            WriteLine();
        }

        // Writes a string followed by a line terminator to the text stream.
        //
        public virtual void WriteLine(string value)
        {
            if (value != null)
            {
                Write(value);
            }
            Write(CoreNewLineStr);
        }

        // Writes the text representation of an object followed by a line
        // terminator to the text stream.
        //
        public virtual void WriteLine(object value)
        {
            if (value == null)
            {
                WriteLine();
            }
            else
            {
                // Call WriteLine(value.ToString), not Write(Object), WriteLine().
                // This makes calls to WriteLine(Object) atomic.
                IFormattable f = value as IFormattable;
                if (f != null)
                {
                    WriteLine(f.ToString(null, FormatProvider));
                }
                else
                {
                    WriteLine(value.ToString());
                }
            }
        }

        // Writes out a formatted string and a new line.  Uses the same 
        // semantics as String.Format.
        // 
        public virtual void WriteLine(string format, object arg0)
        {
            WriteLine(string.Format(FormatProvider, format, arg0));
        }

        // Writes out a formatted string and a new line.  Uses the same 
        // semantics as String.Format.
        // 
        public virtual void WriteLine(string format, object arg0, object arg1)
        {
            WriteLine(string.Format(FormatProvider, format, arg0, arg1));
        }

        // Writes out a formatted string and a new line.  Uses the same 
        // semantics as String.Format.
        // 
        public virtual void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(string.Format(FormatProvider, format, arg0, arg1, arg2));
        }

        // Writes out a formatted string and a new line.  Uses the same 
        // semantics as String.Format.
        // 
        public virtual void WriteLine(string format, params object[] arg)
        {
            WriteLine(string.Format(FormatProvider, format, arg));
        }

        #region Task based Async APIs
        public virtual Task WriteAsync(char value)
        {
            var tuple = new Tuple<TextWriter, char>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char>)state;
                t.Item1.Write(t.Item2);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteAsync(string value)
        {
            var tuple = new Tuple<TextWriter, string>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, string>)state;
                t.Item1.Write(t.Item2);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task WriteAsync(char[] buffer)
        {
            if (buffer == null)
            {
                return Task.CompletedTask;
            }

            return WriteAsync(buffer, 0, buffer.Length);
        }

        public virtual Task WriteAsync(char[] buffer, int index, int count)
        {
            var tuple = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char[], int, int>)state;
                t.Item1.Write(t.Item2, t.Item3, t.Item4);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync(char value)
        {
            var tuple = new Tuple<TextWriter, char>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char>)state;
                t.Item1.WriteLine(t.Item2);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync(string value)
        {
            var tuple = new Tuple<TextWriter, string>(this, value);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, string>)state;
                t.Item1.WriteLine(t.Item2);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task WriteLineAsync(char[] buffer)
        {
            if (buffer == null)
            {
                return Task.CompletedTask;
            }

            return WriteLineAsync(buffer, 0, buffer.Length);
        }

        public virtual Task WriteLineAsync(char[] buffer, int index, int count)
        {
            var tuple = new Tuple<TextWriter, char[], int, int>(this, buffer, index, count);
            return Task.Factory.StartNew(state =>
            {
                var t = (Tuple<TextWriter, char[], int, int>)state;
                t.Item1.WriteLine(t.Item2, t.Item3, t.Item4);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task WriteLineAsync()
        {
            return WriteAsync(CoreNewLine);
        }

        public virtual Task FlushAsync()
        {
            return Task.Factory.StartNew(state =>
            {
                ((TextWriter)state).Flush();
            },
            this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        #endregion

        [Serializable]
        private sealed class NullTextWriter : TextWriter
        {
            internal NullTextWriter() : base(CultureInfo.InvariantCulture)
            {
            }

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.Unicode;
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
            }

            public override void Write(string value)
            {
            }

            // Not strictly necessary, but for perf reasons
            public override void WriteLine()
            {
            }

            // Not strictly necessary, but for perf reasons
            public override void WriteLine(string value)
            {
            }

            public override void WriteLine(object value)
            {
            }

            public override void Write(char value)
            {
            }
        }
    }
}
