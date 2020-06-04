// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public class WriteOnlyStream : MemoryStream
    {
        internal WriteOnlyStream(byte[] data) : base(data, 0, data.Length, true) { }
        public override bool CanRead { get { return false; } }
        public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException("This is a write-only stream"); }
        public override int ReadByte() { throw new NotSupportedException("This is a write-only stream"); }
        public override byte[] ToArray() { throw new NotSupportedException("This is a write-only stream"); }
        public override void WriteTo(Stream stream) { throw new NotSupportedException("This is a write-only stream"); }
    }
}
