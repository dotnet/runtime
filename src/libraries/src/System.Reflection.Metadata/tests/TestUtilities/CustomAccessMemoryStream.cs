// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Reflection.Metadata.Tests
{
    public class CustomAccessMemoryStream : MemoryStream
    {
        private readonly bool _canRead, _canSeek, _canWrite;

        public CustomAccessMemoryStream(bool canRead, bool canSeek, bool canWrite, byte[] buffer = null)
            : base(buffer ?? Array.Empty<byte>())
        {
            _canRead = canRead;
            _canSeek = canSeek;
            _canWrite = canWrite;
        }

        public override bool CanRead => _canRead;
        public override bool CanSeek => _canSeek;
        public override bool CanWrite => _canWrite;
    }
}
