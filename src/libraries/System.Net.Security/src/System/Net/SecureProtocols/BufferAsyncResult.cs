// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
