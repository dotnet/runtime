// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// This interface defines methods for obtaining a display/window device context handle (Win32 hdc).
    /// Note: Display and window dc handles are obtained and released using BeginPaint/EndPaint and
    /// GetDC/ReleaseDC; this interface is intended to be used with the last method only.  
    /// 
    /// Warning to implementors: Creating and releasing non-display dc handles using this interface needs
    /// special care, for instance using other Win32 functions like CreateDC or CreateCompatibleDC require 
    /// DeleteDC instead of ReleaseDC to properly free the dc handle.  
    /// 
    /// See the DeviceContext class for an implementation of this interface, it uses the Dispose method
    /// for freeing non-display dc handles.
    /// 
    /// This is a low-level API that is expected to be used with TextRenderer or PInvoke calls.
    /// </summary>
    public interface IDeviceContext : IDisposable
    {
        IntPtr GetHdc();

        void ReleaseHdc();
    }
}
