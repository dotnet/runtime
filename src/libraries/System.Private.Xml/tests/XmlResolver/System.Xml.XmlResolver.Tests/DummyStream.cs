// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Xml.XmlResolver.Tests
{
    internal class DummyStream : MemoryStream
    {
        internal DummyStream(byte[] data) : base(data) { }        
        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin loc)
        {
            throw new NotSupportedException("Seek operation is not supported for Dummy Stream");
        }
    }
}
