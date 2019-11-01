// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Simple wrapper to create a screen HDC within a using statement.
    /// </summary>
    internal struct ScreenDC : IDisposable
    {
        private IntPtr _handle;

        public static ScreenDC Create()
        {
            return new ScreenDC
            {
                _handle = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef)
            };
        }

        public static implicit operator IntPtr(ScreenDC screenDC) => screenDC._handle;

        public void Dispose()
        {
            UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, _handle));
        }
    }
}
