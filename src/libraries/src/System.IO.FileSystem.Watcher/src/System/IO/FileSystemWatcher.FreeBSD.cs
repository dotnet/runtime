// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    // FreeBSD implementation of FileSystemWatcher's PAL.  The main partial definition of FileSystemWatcher expects:
    // - StartRaisingEvents
    // - StopRaisingEvents
    // - FinalizeDispose

    // We will need to implement these methods to support FreeBSD.
    // Until then, this is stubbed out with PlatformNotSupportedException.

    public partial class FileSystemWatcher
    {
        private void StartRaisingEvents()
        {
            throw new PlatformNotSupportedException();
        }

        private void StopRaisingEvents()
        {
            throw new PlatformNotSupportedException();
        }

        private void Close()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
