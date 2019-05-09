// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class ThreadPoolBoundHandle
    {
        private static ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
        {
            Debug.Assert(handle != null);
            Debug.Assert(!handle.IsClosed);
            Debug.Assert(!handle.IsInvalid);

            try
            {
                // ThreadPool.BindHandle will always return true, otherwise, it throws. See the underlying FCall
                // implementation in ThreadPoolNative::CorBindIoCompletionCallback to see the implementation.
                bool succeeded = ThreadPool.BindHandle(handle);
                Debug.Assert(succeeded);
            }
            catch (Exception ex)
            {   // BindHandle throws ApplicationException on full CLR and Exception on CoreCLR.
                // We do not let either of these leak and convert them to ArgumentException to 
                // indicate that the specified handles are invalid.

                if (ex.HResult == HResults.E_HANDLE)         // Bad handle
                    throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

                if (ex.HResult == HResults.E_INVALIDARG)     // Handle already bound or sync handle
                    throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, nameof(handle));

                throw;
            }

            return new ThreadPoolBoundHandle(handle);
        }
    }
}
