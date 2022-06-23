// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    // Unix specific methods for the TarEntry class.
    public abstract partial class TarEntry
    {
        // Unix specific implementation of the method that extracts the current entry as a block device.
        partial void ExtractAsBlockDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.BlockDevice);
            Interop.CheckIo(Interop.Sys.CreateBlockDevice(destinationFileName, (uint)Mode, (uint)_header._devMajor, (uint)_header._devMinor), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a character device.
        partial void ExtractAsCharacterDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.CharacterDevice);
            Interop.CheckIo(Interop.Sys.CreateCharacterDevice(destinationFileName, (uint)Mode, (uint)_header._devMajor, (uint)_header._devMinor), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a fifo file.
        partial void ExtractAsFifo(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.Fifo);
            Interop.CheckIo(Interop.Sys.MkFifo(destinationFileName, (uint)Mode), destinationFileName);
        }

        // Unix specific implementation of the method that extracts the current entry as a hard link.
        partial void ExtractAsHardLink(string targetFilePath, string hardLinkFilePath)
        {
            Debug.Assert(EntryType is TarEntryType.HardLink);
            Debug.Assert(!string.IsNullOrEmpty(targetFilePath));
            Debug.Assert(!string.IsNullOrEmpty(hardLinkFilePath));
            Interop.CheckIo(Interop.Sys.Link(targetFilePath, hardLinkFilePath), hardLinkFilePath);
        }

        // Unix specific implementation of the method that specifies the file permissions of the extracted file.
        partial void SetModeOnFile(SafeFileHandle handle, string destinationFileName)
        {
            // Only extract USR, GRP, and OTH file permissions, and ignore
            // S_ISUID, S_ISGID, and S_ISVTX bits.
            // It is off by default because it's possible that a file in an archive could have
            // one of these bits set and, unknown to the person extracting, could allow others to
            // execute the file as the user or group.
            const int ExtractPermissionMask = 0x1FF;
            int permissions = (int)Mode & ExtractPermissionMask;

            // If the permissions weren't set at all, don't write the file's permissions.
            if (permissions != 0)
            {
                Interop.CheckIo(Interop.Sys.FChMod(handle, permissions), destinationFileName);
            }
        }
    }
}
