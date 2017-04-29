// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Data.SqlTypes
{
    internal abstract class SqlStreamChars : INullable, IDisposable
    {
        public abstract bool IsNull { get; }

        public abstract long Length { get; }

        public abstract long Position { get; set; }

        // --------------------------------------------------------------
        //	  Public methods
        // --------------------------------------------------------------
        public abstract int Read(char[] buffer, int offset, int count);

        public abstract void Write(char[] buffer, int offset, int count);

        public abstract long Seek(long offset, SeekOrigin origin);

        public abstract void SetLength(long value);

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    } // class SqlStreamChars
} // namespace System.Data.SqlTypes 
