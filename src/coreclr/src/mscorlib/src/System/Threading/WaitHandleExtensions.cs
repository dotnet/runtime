// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.Threading
{
    public static class WaitHandleExtensions
    {
        /// <summary>
        /// Gets the native operating system handle.
        /// </summary>
        /// <param name="waitHandle">The <see cref="System.Threading.WaitHandle"/> to operate on.</param>
        /// <returns>A <see cref="System.Runtime.InteropServices.SafeHandle"/> representing the native operating system handle.</returns>
        [SecurityCritical]
        public static SafeWaitHandle GetSafeWaitHandle(this WaitHandle waitHandle)
        {
            if (waitHandle == null)
            {
                throw new ArgumentNullException("waitHandle");
            }

            return waitHandle.SafeWaitHandle;
        }

        /// <summary>
        /// Sets the native operating system handle
        /// </summary>
        /// <param name="waitHandle">The <see cref="System.Threading.WaitHandle"/> to operate on.</param>
        /// <param name="value">A <see cref="System.Runtime.InteropServices.SafeHandle"/> representing the native operating system handle.</param>
        [SecurityCritical]
        public static void SetSafeWaitHandle(this WaitHandle waitHandle, SafeWaitHandle value)
        {
            if (waitHandle == null)
            {
                throw new ArgumentNullException("waitHandle");
            }

            waitHandle.SafeWaitHandle = value;
        }
    }
}