// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Reflection.Metadata.Tests
{
    public class TestStream : TestStreamBase
    {
        private readonly bool _canRead, _canWrite, _canSeek;

        public TestStream(bool canRead = false, bool canWrite = false, bool canSeek = false)
        {
            _canRead = canRead;
            _canWrite = canWrite;
            _canSeek = canSeek;
        }

        public override bool CanRead => _canRead;
        public override bool CanWrite => _canWrite;
        public override bool CanSeek => _canSeek;
    }
}
