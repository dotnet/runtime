// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Security.Principal;

namespace System.Net
{
    internal partial class ContextAwareResult
    {
        private WindowsIdentity? _windowsIdentity;

        // Security: We need an assert for a call into WindowsIdentity.GetCurrent.
        private void SafeCaptureIdentity()
        {
            _windowsIdentity = WindowsIdentity.GetCurrent();
        }

        private void CleanupInternal()
        {
            if (_windowsIdentity != null)
            {
                _windowsIdentity.Dispose();
                _windowsIdentity = null;
            }
        }
    }
}
