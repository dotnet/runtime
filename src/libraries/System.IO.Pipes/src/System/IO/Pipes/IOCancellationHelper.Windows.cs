// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.Pipes
{
    internal unsafe class IOCancellationHelper
    {
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationRegistration;
        private SafeHandle _handle;
        private NativeOverlapped* _overlapped;

        public IOCancellationHelper(CancellationToken cancellationToken)
        {
            this._cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Marking that from this moment on
        /// user can cancel operation using cancellationToken
        /// </summary>
        public void AllowCancellation(SafeHandle handle, NativeOverlapped* overlapped)
        {
            Debug.Assert(handle != null, "Handle cannot be null");
            Debug.Assert(!handle.IsInvalid, "Handle cannot be invalid");
            Debug.Assert(overlapped != null, "Overlapped cannot be null");
            Debug.Assert(this._handle == null && this._overlapped == null, "Cancellation is already allowed.");

            if (!_cancellationToken.CanBeCanceled)
            {
                return;
            }

            this._handle = handle;
            this._overlapped = overlapped;
            if (this._cancellationToken.IsCancellationRequested)
            {
                this.Cancel();
            }
            else
            {
                this._cancellationRegistration = this._cancellationToken.Register(Cancel);
            }
        }

        /// <summary>
        /// Marking that operation is completed and
        /// from this moment cancellation is no longer possible.
        /// This MUST happen before Overlapped is freed and Handle is disposed.
        /// </summary>
        public void SetOperationCompleted()
        {
            if (this._overlapped != null)
            {
                this._cancellationRegistration.Dispose();
                this._handle = null;
                this._overlapped = null;
            }
        }

        public void ThrowIOOperationAborted()
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            // If we didn't throw that means that this is unexpected abortion
            throw __Error.GetOperationAborted();
        }

        /// <summary>
        /// Cancellation is not guaranteed to succeed.
        /// We ignore all errors here because operation could
        /// succeed just before it was called or someone already
        /// cancelled this operation without using token which should
        /// be manually detected - when operation finishes we should
        /// compare error code to ERROR_OPERATION_ABORTED and if cancellation
        /// token was not used to cancel we will throw.
        /// </summary>
        private void Cancel()
        {
            // Storing to locals to avoid data races
            SafeHandle handle = this._handle;
            NativeOverlapped* overlapped = this._overlapped;
            if (handle != null && !handle.IsInvalid && overlapped != null)
            {
                if (!Interop.mincore.CancelIoEx(handle, overlapped))
                {
                    // This case should not have any consequences although
                    // it will be easier to debug if there exists any special case
                    // we are not aware of.
                    int errorCode = Marshal.GetLastWin32Error();
                    Debug.WriteLine("CancelIoEx finished with error code {0}.", errorCode);
                }
                SetOperationCompleted();
            }
        }
    }
}
