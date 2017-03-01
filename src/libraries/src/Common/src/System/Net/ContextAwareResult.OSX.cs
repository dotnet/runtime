// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    partial class ContextAwareResult
    {
        private void SafeCaptureIdentity()
        {
            // WindowsIdentity is not supported on Unix
        }

        private void CleanupInternal()
        {
            // Nothing to cleanup
        }
    }
}
