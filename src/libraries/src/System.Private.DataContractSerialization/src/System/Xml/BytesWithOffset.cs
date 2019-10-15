// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal readonly struct BytesWithOffset
    {
        private readonly byte[] _bytes;
        private readonly int _offset;

        public BytesWithOffset(byte[] bytes, int offset)
        {
            _bytes = bytes;
            _offset = offset;
        }

        public byte[] Bytes
        {
            get
            {
                return _bytes;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
        }
    }
}
