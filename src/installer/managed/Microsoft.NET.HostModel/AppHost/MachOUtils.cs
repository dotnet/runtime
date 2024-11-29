// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.NET.HostModel.MachO;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class MachOUtils
    {
        // The MachO Headers are copied from
        // https://opensource.apple.com/source/cctools/cctools-870/include/mach-o/loader.h
        //
        // The data fields and enumerations match the structure definitions in the above file,
        // and hence do not conform to C# CoreFx naming style.

        [StructLayout(LayoutKind.Sequential)]
        private struct SymtabCommand
        {
            public uint cmd;
            public uint cmdsize;
            public uint symoff;
            public uint nsyms;
            public uint stroff;
            public uint strsize;
        };

        private static void Verify(bool condition, MachOFormatError error)
        {
            if (!condition)
            {
                throw new AppHostMachOFormatException(error);
            }
        }

        /// <summary>
        /// This Method is a utility to adjust the apphost MachO-header
        /// to include the bytes added by the single-file bundler at the end of the file.
        ///
        /// The tool assumes the following layout of the executable
        ///
        /// * MachoHeader (64-bit, executable, not swapped integers)
        /// * LoadCommands
        ///     LC_SEGMENT_64 (__PAGEZERO)
        ///     LC_SEGMENT_64 (__TEXT)
        ///     LC_SEGMENT_64 (__DATA)
        ///     LC_SEGMENT_64 (__LINKEDIT)
        ///     ...
        ///     LC_SYMTAB
        ///
        ///  * ... Different Segments
        ///
        ///  * The __LINKEDIT Segment (last)
        ///      * ... Different sections ...
        ///      * SYMTAB (last)
        ///
        /// The MAC codesign tool places several restrictions on the layout
        ///   * The __LINKEDIT segment must be the last one
        ///   * The __LINKEDIT segment must cover the end of the file
        ///   * All bytes in the __LINKEDIT segment are used by other linkage commands
        ///     (ex: symbol/string table, dynamic load information etc)
        ///
        /// In order to circumvent these restrictions, we:
        ///    * Extend the __LINKEDIT segment to include the bundle-data
        ///    * Extend the string table to include all the bundle-data
        ///      (that is, the bundle-data appear as strings to the loader/codesign tool).
        ///
        ///  This method has certain limitations:
        ///    * The bytes for the bundler may be unnecessarily loaded at startup
        ///    * Tools that process the string table may be confused (?)
        ///    * The string table size is limited to 4GB. Bundles larger than that size
        ///      cannot be accommodated by this utility.
        ///
        /// </summary>
        /// <param name="filePath">Path to the AppHost</param>
        /// <returns>
        ///  True if
        ///    - The input is a MachO binary, and
        ///    - The additional bytes were successfully accommodated within the MachO segments.
        ///   False otherwise
        /// </returns>
        /// <exception cref="AppHostMachOFormatException">
        /// The input is a MachO file, but doesn't match the expect format of the AppHost.
        /// </exception>
        public static unsafe bool AdjustHeadersForBundle(string filePath)
        {
            ulong fileLength = (ulong)new FileInfo(filePath).Length;
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    byte* file = null;
                    try
                    {
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref file);
                        Verify(file != null, MachOFormatError.MemoryMapAccessFault);

                        MachHeader* header = (MachHeader*)file;

                        if (!MachObjectFile.IsMachOImage(accessor))
                        {
                            // Not a MachO file.
                            return false;
                        }

                        Verify(header->Is64Bit, MachOFormatError.Not64BitExe);

                        file += sizeof(MachHeader);
                        Segment64LoadCommand* linkEdit = null;
                        SymtabCommand* symtab = null;
                        LinkEditCommand* signature = null;

                        for (uint i = 0; i < header->NumberOfCommands; i++)
                        {
                            LoadCommand* command = (LoadCommand*)file;
                            if (command->GetCommandType(*header) == MachLoadCommandType.Segment64)
                            {
                                Segment64LoadCommand* segment = (Segment64LoadCommand*)file;
                                if (segment->Name.Equals(NameBuffer.__LINKEDIT))
                                {
                                    Verify(linkEdit == null, MachOFormatError.DuplicateLinkEdit);
                                    linkEdit = segment;
                                }
                            }
                            else if (command->GetCommandType(*header) == MachLoadCommandType.SymbolTable)
                            {
                                Verify(symtab == null, MachOFormatError.DuplicateSymtab);
                                symtab = (SymtabCommand*)command;
                            }

                            file += command->GetCommandSize(*header);
                        }

                        Verify(linkEdit != null, MachOFormatError.MissingLinkEdit);
                        Verify(symtab != null, MachOFormatError.MissingSymtab);

                        // Update the string table to include bundle-data
                        ulong newStringTableSize = fileLength - symtab->stroff;
                        if (newStringTableSize > uint.MaxValue)
                        {
                            // Too big, too bad;
                            return false;
                        }
                        symtab->strsize = (uint)newStringTableSize;

                        // Update the __LINKEDIT segment to include bundle-data
                        linkEdit->SetFileSize(fileLength - linkEdit->GetFileOffset(*header), *header);
                        linkEdit->SetVMSize(linkEdit->GetFileSize(*header), *header);
                    }
                    finally
                    {
                        if (file != null)
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }

            return true;
        }
    }
}
