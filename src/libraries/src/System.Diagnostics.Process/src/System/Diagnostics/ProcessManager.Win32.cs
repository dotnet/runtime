// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        public static IntPtr GetMainWindowHandle(int processId) 
        {
            MainWindowFinder finder = new MainWindowFinder();
            return finder.FindMainWindow(processId);
        }
    }

    internal sealed class MainWindowFinder 
    {
        private const int GW_OWNER = 4;
        private IntPtr _bestHandle;
        private int _processId;
 
        public IntPtr FindMainWindow(int processId)
        {
            _bestHandle = (IntPtr)0;
            _processId = processId;
            
            Interop.User32.EnumThreadWindowsCallback callback = new Interop.User32.EnumThreadWindowsCallback(EnumWindowsCallback);
            Interop.User32.EnumWindows(callback, IntPtr.Zero);
 
            GC.KeepAlive(callback);
            return _bestHandle;
        }
 
        private bool IsMainWindow(IntPtr handle) 
        {           
            if (Interop.User32.GetWindow(handle, GW_OWNER) != (IntPtr)0 || !Interop.User32.IsWindowVisible(handle))
                return false;
            
            return true;
        }
 
        private bool EnumWindowsCallback(IntPtr handle, IntPtr extraParameter) {
            int processId;
            Interop.User32.GetWindowThreadProcessId(handle, out processId);

            if (processId == _processId) {
                if (IsMainWindow(handle)) {
                    _bestHandle = handle;
                    return false;
                }
            }
            return true;
        }
    }
}
