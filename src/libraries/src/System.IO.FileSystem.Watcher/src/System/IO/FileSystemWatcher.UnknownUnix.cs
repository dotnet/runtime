// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        /// <summary>Called when FileSystemWatcher is finalized.</summary>
        private void FinalizeDispose()
        {
        }

        private void StartRaisingEvents()
        {
            throw new PlatformNotSupportedException();
        }

        private void StopRaisingEvents()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
