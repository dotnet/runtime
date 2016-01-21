// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    partial class ContextAwareResult
    {
        private void SafeCaptureIdentity()
        {
            // WindowsIdentity is not supported on NETCore50
        }

        private void CleanupInternal()
        {
            // Nothing to cleanup
        }
    }
}
