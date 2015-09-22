// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.Sockets
{
    internal struct BufferList
    {
        private IList _buffers; // This is either an IList<ArraySegment<byte>> or a BufferOffsetSize[]

        public bool IsInitialized
        {
            get
            {
                return _buffers != null;
            }
        }

        public int Count
        {
            get
            {
                return _buffers.Count;
            }
        }

        public ArraySegment<byte> this[int i]
        {
            get
            {
                var bufferOffsetSizes = _buffers as BufferOffsetSize[];
                if (bufferOffsetSizes != null)
                {
                    BufferOffsetSize element = bufferOffsetSizes[i];
                    return new ArraySegment<byte>(element.Buffer, element.Offset, element.Size);
                }

                return ((IList<ArraySegment<byte>>)_buffers)[i];
            }
        }

        public BufferList(IList<ArraySegment<byte>> buffers)
        {
            _buffers = (IList)buffers;
        }

        public BufferList(BufferOffsetSize[] buffers)
        {
            _buffers = buffers;
        }
    }
}
