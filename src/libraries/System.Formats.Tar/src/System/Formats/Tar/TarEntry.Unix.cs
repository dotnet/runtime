// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    // Unix specific methods for the TarEntry class.
    public abstract partial class TarEntry
    {
        // Unix specific implementation of the method that extracts the current entry as a block device.
        private void ExtractAsBlockDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.BlockDevice);
            Interop.CheckIo(Interop.Sys.CreateBlockDevice(destinationFileName, (uint)Mode, (uint)_header._devMajor, (uint)_header._devMinor), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a character device.
        private void ExtractAsCharacterDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.CharacterDevice);
            Interop.CheckIo(Interop.Sys.CreateCharacterDevice(destinationFileName, (uint)Mode, (uint)_header._devMajor, (uint)_header._devMinor), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a fifo file.
        private void ExtractAsFifo(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.Fifo);
            Interop.CheckIo(Interop.Sys.MkFifo(destinationFileName, (uint)Mode), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a hard link.
        private void ExtractAsHardLink(string targetFilePath, string hardLinkFilePath)
        {
            Debug.Assert(EntryType is TarEntryType.HardLink);
            Debug.Assert(!string.IsNullOrEmpty(targetFilePath));
            Debug.Assert(!string.IsNullOrEmpty(hardLinkFilePath));
            Interop.CheckIo(Interop.Sys.Link(targetFilePath, hardLinkFilePath), hardLinkFilePath);
        }
    }
}
