// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Simple wrapper to create a screen HDC within a using statement.
    /// </summary>
    internal struct ScreenDC : IDisposable
    {
        private IntPtr _handle;

        public static ScreenDC Create() => new ScreenDC
        {
            _handle = Interop.User32.GetDC(IntPtr.Zero)
        };

        public static implicit operator IntPtr(ScreenDC screenDC) => screenDC._handle;

        public void Dispose() => Interop.User32.ReleaseDC(IntPtr.Zero, _handle);
    }
}
