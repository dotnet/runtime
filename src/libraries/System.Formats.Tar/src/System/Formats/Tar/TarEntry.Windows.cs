// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    // Windows specific methods for the TarEntry class.
    public abstract partial class TarEntry
    {
        // Throws on Windows. Block devices are not supported on this platform.
        private void ExtractAsBlockDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice);
            throw new InvalidOperationException(SR.IO_DeviceFiles_NotSupported);
        }

        // Throws on Windows. Character devices are not supported on this platform.
        private void ExtractAsCharacterDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice);
            throw new InvalidOperationException(SR.IO_DeviceFiles_NotSupported);
        }

        // Throws on Windows. Fifo files are not supported on this platform.
        private void ExtractAsFifo(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.Fifo);
            throw new InvalidOperationException(SR.IO_FifoFiles_NotSupported);
        }

        // Windows specific implementation of the method that extracts the current entry as a hard link.
        private void ExtractAsHardLink(string targetFilePath, string hardLinkFilePath)
        {
            Debug.Assert(EntryType is TarEntryType.HardLink);
            Debug.Assert(!string.IsNullOrEmpty(targetFilePath));
            Debug.Assert(!string.IsNullOrEmpty(hardLinkFilePath));
            Interop.Kernel32.CreateHardLink(hardLinkFilePath, targetFilePath);
        }
    }
}
