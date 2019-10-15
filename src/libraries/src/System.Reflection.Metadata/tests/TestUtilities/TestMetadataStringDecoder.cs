// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Reflection.Metadata.Tests
{
    public unsafe delegate string GetString(byte* bytes, int count);

    public sealed class TestMetadataStringDecoder : MetadataStringDecoder
    {
        private readonly GetString _getString;

        public TestMetadataStringDecoder(Encoding encoding, GetString getString)
            : base(encoding)
        {
            _getString = getString;
        }

        public override unsafe string GetString(byte* bytes, int byteCount)
        {
            return _getString(bytes, byteCount);
        }
    }
}
