// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal struct BytesWithOffset
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
