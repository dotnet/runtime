// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace System.Data.SqlTypes
{
    public sealed partial class SqlFileStream : System.IO.Stream
    {
        public SqlFileStream(string path, byte[] transactionContext, FileAccess access)
        {
            throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported);
        }

        public SqlFileStream(string path, byte[] transactionContext, FileAccess access, FileOptions options, Int64 allocationSize)
        {
            throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported);
        }

        public string Name { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public byte[] TransactionContext { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override bool CanRead { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override bool CanSeek { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override bool CanWrite { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override long Length { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override long Position { get { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } set { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); } }
        public override void Flush() { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); }
        public override int Read(byte[] buffer, int offset, int count) { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); }
        public override void SetLength(long value) { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); }
        public override void Write(byte[] buffer, int offset, int count) { throw new PlatformNotSupportedException(SR.SqlFileStream_NotSupported); }
    }
}


