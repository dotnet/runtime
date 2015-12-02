// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    //
    // Preserve the original request buffer & sizes for user IO requests.
    // This is returned as an IAsyncResult to the application.
    //
    internal class BufferAsyncResult : LazyAsyncResult
    {
        public byte[] Buffer;
        public int Offset;
        public int Count;
        public bool IsWrite;

        public BufferAsyncResult(object asyncObject, byte[] buffer, int offset, int count, object asyncState, AsyncCallback asyncCallback)
            : this(asyncObject, buffer, offset, count, false, asyncState, asyncCallback)
        {
        }

        public BufferAsyncResult(object asyncObject, byte[] buffer, int offset, int count, bool isWrite, object asyncState, AsyncCallback asyncCallback)
            : base(asyncObject, asyncState, asyncCallback)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
            IsWrite = isWrite;
        }
    }
}
