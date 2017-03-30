// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Data.SqlTypes
{
    internal abstract class SqlStreamChars : INullable, IDisposable
    {
        public abstract bool IsNull { get; }

        public abstract bool CanRead { get; }

        public abstract bool CanSeek { get; }

        public abstract bool CanWrite { get; }

        public abstract long Length { get; }

        public abstract long Position { get; set; }

        // --------------------------------------------------------------
        //	  Public methods
        // --------------------------------------------------------------
        public abstract int Read(char[] buffer, int offset, int count);

        public abstract void Write(char[] buffer, int offset, int count);

        public abstract long Seek(long offset, SeekOrigin origin);

        public abstract void SetLength(long value);

        public abstract void Flush();

        public virtual void Close()
        {
            Dispose(true);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public virtual int ReadChar()
        {
            // Reads one char from the stream by calling Read(char[], int, int). 
            // Will return an char cast to an int or -1 on end of stream.
            // The performance of the default implementation on Stream is bad,
            // and any subclass with an internal buffer should override this method.
            char[] oneCharArray = new char[1];
            int r = Read(oneCharArray, 0, 1);
            if (r == 0)
                return -1;
            return oneCharArray[0];
        }

        public virtual void WriteChar(char value)
        {
            // Writes one char from the stream by calling Write(char[], int, int).  
            // The performance of the default implementation on Stream is bad,
            // and any subclass with an internal buffer should override this method.
            char[] oneCharArray = new char[1];
            oneCharArray[0] = value;
            Write(oneCharArray, 0, 1);
        }


        // Private class: the Null SqlStreamChars
        private class NullSqlStreamChars : SqlStreamChars
        {
            // --------------------------------------------------------------
            //	  Constructor(s)
            // --------------------------------------------------------------

            internal NullSqlStreamChars()
            {
            }


            // --------------------------------------------------------------
            //	  Public properties
            // --------------------------------------------------------------

            public override bool IsNull
            {
                get
                {
                    return true;
                }
            }

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    throw new SqlNullValueException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new SqlNullValueException();
                }
                set
                {
                    throw new SqlNullValueException();
                }
            }

            // --------------------------------------------------------------
            //	  Public methods
            // --------------------------------------------------------------
            public override int Read(char[] buffer, int offset, int count)
            {
                throw new SqlNullValueException();
            }

            public override void Write(char[] buffer, int offset, int count)
            {
                throw new SqlNullValueException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new SqlNullValueException();
            }

            public override void SetLength(long value)
            {
                throw new SqlNullValueException();
            }

            public override void Flush()
            {
                throw new SqlNullValueException();
            }

            public override void Close()
            {
            }
        } // class NullSqlStreamChars


        // The Null instance
        public static SqlStreamChars Null
        {
            get
            {
                return new NullSqlStreamChars();
            }
        }
    } // class SqlStreamChars
} // namespace System.Data.SqlTypes 
