// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public static class WaitHandleExtensions
    {
        /// <summary>
        /// Gets the native operating system handle.
        /// </summary>
        /// <param name="waitHandle">The <see cref="System.Threading.WaitHandle"/> to operate on.</param>
        /// <returns>A <see cref="System.Runtime.InteropServices.SafeHandle"/> representing the native operating system handle.</returns>
        public static SafeWaitHandle? GetSafeWaitHandle(this WaitHandle waitHandle)
        {
            if (waitHandle == null)
            {
                throw new ArgumentNullException(nameof(waitHandle));
            }

            return waitHandle.SafeWaitHandle;
        }

        /// <summary>
        /// Sets the native operating system handle
        /// </summary>
        /// <param name="waitHandle">The <see cref="System.Threading.WaitHandle"/> to operate on.</param>
        /// <param name="value">A <see cref="System.Runtime.InteropServices.SafeHandle"/> representing the native operating system handle.</param>
        public static void SetSafeWaitHandle(this WaitHandle waitHandle, SafeWaitHandle? value)
        {
            if (waitHandle == null)
            {
                throw new ArgumentNullException(nameof(waitHandle));
            }

            waitHandle.SafeWaitHandle = value;
        }
    }
}
