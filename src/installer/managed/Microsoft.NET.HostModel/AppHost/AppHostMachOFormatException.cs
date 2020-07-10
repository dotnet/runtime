// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Additional details about the failure with caused an AppHostMachOFormatException
    /// </summary>
    public enum MachOFormatError
    {
        Not64BitExe,            // Apphost is expected to be a 64-bit MachO executable
        DuplicateLinkEdit,      // Only one __LINKEDIT segment is expected in the apphost
        DuplicateSymtab,        // Only one SYMTAB is expected in the apphost
        SignNeedsLinkEdit,      // CODE_SIGNATURE command must follow a Segment64 command named __LINKEDIT
        SignNeedsSymtab,        // CODE_SIGNATURE command must follow the SYMTAB command
        LinkEditNotLast,        // __LINKEDIT must be the last segment in the binary layout
        SymtabNotInLinkEdit,    // SYMTAB must within the __LINKEDIT segment!
        SignNotInLinkEdit,      // Signature blob must be within the __LINKEDIT segment!
        SignCommandNotLast,     // CODE_SIGNATURE command must be the last command
        SignBlobNotLast,        // Signature blob must be at the very end of the file
        SignDoesntFollowSymtab, // Signature blob must immediately follow the Symtab
        MemoryMapAccessFault,   // Error reading the memory-mapped apphost
        InvalidUTF8             // UTF8 decoding failed 
    }

    /// <summary>
    /// The MachO application host executable cannot be customized because 
    /// it was not in the expected format
    /// </summary>
    public class AppHostMachOFormatException : AppHostUpdateException
    {
        public readonly MachOFormatError Error;

        public AppHostMachOFormatException(MachOFormatError error)
        {
            Error = error;
        }
    }
}

