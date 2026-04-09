// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Principal;

namespace System.Net
{
    internal sealed partial class ContextAwareResult
    {
        private WindowsIdentity? _windowsIdentity;

        private void SafeCaptureIdentity()
        {
            Debug.Assert(OperatingSystem.IsWindows());
            _windowsIdentity = WindowsIdentity.GetCurrent();
        }

        private void CleanupInternal()
        {
            if (_windowsIdentity != null)
            {
                Debug.Assert(OperatingSystem.IsWindows());
                _windowsIdentity.Dispose();
                _windowsIdentity = null;
            }
        }
    }
}
