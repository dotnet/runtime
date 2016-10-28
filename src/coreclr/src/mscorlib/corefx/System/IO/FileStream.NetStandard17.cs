// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback callback, object state)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(numBytes), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < numBytes)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            if (IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);

            if (!IsAsync)
                return base.BeginRead(array, offset, numBytes, callback, state);
            else
                return TaskToApm.Begin(ReadAsyncInternal(array, offset, numBytes, CancellationToken.None), callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback callback, object state)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(numBytes), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - offset < numBytes)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            if (IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

            if (!IsAsync)
                return base.BeginWrite(array, offset, numBytes, callback, state);
            else
                return TaskToApm.Begin(WriteAsyncInternal(array, offset, numBytes, CancellationToken.None), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!IsAsync)
                return base.EndRead(asyncResult);
            else
                return TaskToApm.End<int>(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!IsAsync)
                base.EndWrite(asyncResult);
            else
                TaskToApm.End(asyncResult);
        }
    }
}
