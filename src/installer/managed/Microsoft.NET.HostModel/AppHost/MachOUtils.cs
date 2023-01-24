// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class MachOUtils
    {
        // The MachO Headers are copied from
        // https://opensource.apple.com/source/cctools/cctools-870/include/mach-o/loader.h
        //
        // The data fields and enumerations match the structure definitions in the above file,
        // and hence do not conform to C# CoreFx naming style.

        private enum Magic : uint
        {
            MH_MAGIC = 0xfeedface,
            MH_CIGAM = 0xcefaedfe,
            MH_MAGIC_64 = 0xfeedfacf,
            MH_CIGAM_64 = 0xcffaedfe
        }

        private enum FileType : uint
        {
            MH_EXECUTE = 0x2
        }

#pragma warning disable 0649
        private struct MachHeader
        {
            public Magic magic;
            public int cputype;
            public int cpusubtype;
            public FileType filetype;
            public uint ncmds;
            public uint sizeofcmds;
            public uint flags;
            public uint reserved;

            public bool Is64BitExecutable()
            {
                return magic == Magic.MH_MAGIC_64 && filetype == FileType.MH_EXECUTE;
            }

            public bool IsValid()
            {
                switch (magic)
                {
                    case Magic.MH_CIGAM:
                    case Magic.MH_CIGAM_64:
                    case Magic.MH_MAGIC:
                    case Magic.MH_MAGIC_64:
                        return true;

                    default:
                        return false;
                }
            }
        }

        private enum Command : uint
        {
            LC_SYMTAB = 0x2,
            LC_SEGMENT_64 = 0x19,
            LC_CODE_SIGNATURE = 0x1d,
        }

        private struct LoadCommand
        {
            public Command cmd;
            public uint cmdsize;
        }

        // The linkedit_data_command contains the offsets and sizes of a blob
        // of data in the __LINKEDIT segment (including LC_CODE_SIGNATURE).
        private struct LinkEditDataCommand
        {
            public Command cmd;
            public uint cmdsize;
            public uint dataoff;
            public uint datasize;
        }

        private struct SymtabCommand
        {
            public uint cmd;
            public uint cmdsize;
            public uint symoff;
            public uint nsyms;
            public uint stroff;
            public uint strsize;
        };

        private unsafe struct SegmentCommand64
        {
            public Command cmd;
            public uint cmdsize;
            public fixed byte segname[16];
            public ulong vmaddr;
            public ulong vmsize;
            public ulong fileoff;
            public ulong filesize;
            public int maxprot;
            public int initprot;
            public uint nsects;
            public uint flags;

            public string SegName
            {
                get
                {
                    fixed (byte* p = segname)
                    {
                        int len = 0;
                        while (*(p + len) != 0 && len++ < 16) ;

                        try
                        {
                            return Encoding.UTF8.GetString(p, len);
                        }
                        catch (ArgumentException)
                        {
                            throw new AppHostMachOFormatException(MachOFormatError.InvalidUTF8);
                        }
                    }
                }
            }
        }

#pragma warning restore 0649

        private static void Verify(bool condition, MachOFormatError error)
        {
            if (!condition)
            {
                throw new AppHostMachOFormatException(error);
            }
        }

        public static bool IsMachOImage(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                if (reader.BaseStream.Length < 256) // Header size
                {
                    return false;
                }

                uint magic = reader.ReadUInt32();
                return Enum.IsDefined(typeof(Magic), magic);
            }
        }

        /// <summary>
        /// This Method is a utility to remove the code-signature (if any)
        /// from a MachO AppHost binary.
        ///
        /// The tool assumes the following layout of the executable:
        ///
        /// * MachoHeader (64-bit, executable, not swapped integers)
        /// * LoadCommands
        ///     LC_SEGMENT_64 (__PAGEZERO)
        ///     LC_SEGMENT_64 (__TEXT)
        ///     LC_SEGMENT_64 (__DATA)
        ///     LC_SEGMENT_64 (__LINKEDIT)
        ///     ...
        ///     LC_SYMTAB
        ///     ...
        ///     LC_CODE_SIGNATURE (last)
        ///
        ///  * ... Different Segments ...
        ///
        ///  * The __LINKEDIT Segment (last)
        ///      * ... Different sections ...
        ///      * SYMTAB
        ///      * (Some alignment bytes)
        ///      * The Code-signature
        ///
        /// In order to remove the signature, the method:
        /// - Removes (zeros out) the LC_CODE_SIGNATURE command
        /// - Adjusts the size and count of the load commands in the header
        /// - Truncates the size of the __LINKEDIT segment to the end of SYMTAB
        /// - Truncates the apphost file to the end of the __LINKEDIT segment
        ///
        /// </summary>
        /// <param name="stream">Stream containing the AppHost</param>
        /// <returns>
        ///  True if
        ///    - The input is a MachO binary, and
        ///    - It is a signed binary, and
        ///    - The signature was successfully removed
        ///   False otherwise
        /// </returns>
        /// <exception cref="AppHostMachOFormatException">
        /// The input is a MachO file, but doesn't match the expect format of the AppHost.
        /// </exception>
        public static unsafe bool RemoveSignature(FileStream stream)
        {
            uint signatureSize = 0;
            using (var mappedFile = MemoryMappedFile.CreateFromFile(stream,
                                                                    mapName: null,
                                                                    capacity: 0,
                                                                    MemoryMappedFileAccess.ReadWrite,
                                                                    HandleInheritability.None,
                                                                    leaveOpen: true))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    byte* file = null;
                    try
                    {
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref file);
                        Verify(file != null, MachOFormatError.MemoryMapAccessFault);

                        MachHeader* header = (MachHeader*)file;

                        if (!header->IsValid())
                        {
                            // Not a MachO file.
                            return false;
                        }

                        Verify(header->Is64BitExecutable(), MachOFormatError.Not64BitExe);

                        file += sizeof(MachHeader);
                        SegmentCommand64* linkEdit = null;
                        SymtabCommand* symtab = null;
                        LinkEditDataCommand* signature = null;

                        for (uint i = 0; i < header->ncmds; i++)
                        {
                            LoadCommand* command = (LoadCommand*)file;
                            if (command->cmd == Command.LC_SEGMENT_64)
                            {
                                SegmentCommand64* segment = (SegmentCommand64*)file;
                                if (segment->SegName.Equals("__LINKEDIT"))
                                {
                                    Verify(linkEdit == null, MachOFormatError.DuplicateLinkEdit);
                                    linkEdit = segment;
                                }
                            }
                            else if (command->cmd == Command.LC_SYMTAB)
                            {
                                Verify(symtab == null, MachOFormatError.DuplicateSymtab);
                                symtab = (SymtabCommand*)command;
                            }
                            else if (command->cmd == Command.LC_CODE_SIGNATURE)
                            {
                                Verify(i == header->ncmds - 1, MachOFormatError.SignCommandNotLast);
                                signature = (LinkEditDataCommand*)command;
                                break;
                            }

                            file += command->cmdsize;
                        }

                        if (signature != null)
                        {
                            Verify(linkEdit != null, MachOFormatError.MissingLinkEdit);
                            Verify(symtab != null, MachOFormatError.MissingSymtab);

                            var symtabEnd = symtab->stroff + symtab->strsize;
                            var linkEditEnd = linkEdit->fileoff + linkEdit->filesize;
                            var signatureEnd = signature->dataoff + signature->datasize;
                            var fileEnd = (ulong)stream.Length;

                            Verify(linkEditEnd == fileEnd, MachOFormatError.LinkEditNotLast);
                            Verify(signatureEnd == fileEnd, MachOFormatError.SignBlobNotLast);

                            Verify(symtab->symoff > linkEdit->fileoff, MachOFormatError.SymtabNotInLinkEdit);
                            Verify(signature->dataoff > linkEdit->fileoff, MachOFormatError.SignNotInLinkEdit);

                            // The signature blob immediately follows the symtab blob,
                            // except for a few bytes of padding.
                            Verify(signature->dataoff >= symtabEnd && signature->dataoff - symtabEnd < 32, MachOFormatError.SignBlobNotLast);

                            // Remove the signature command
                            header->ncmds--;
                            header->sizeofcmds -= signature->cmdsize;
                            Unsafe.InitBlock(signature, 0, signature->cmdsize);

                            // Remove the signature blob (note for truncation)
                            signatureSize = (uint)(fileEnd - symtabEnd);

                            // Adjust the __LINKEDIT segment load command
                            linkEdit->filesize -= signatureSize;

                            // codesign --remove-signature doesn't reset the vmsize.
                            // Setting the vmsize here makes the output bin-equal with the original
                            // unsigned apphost (and not bin-equal with a signed-unsigned-apphost).
                            linkEdit->vmsize = linkEdit->filesize;
                        }
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

            if (signatureSize != 0)
            {
                // The signature was removed, update the file length
                stream.SetLength(stream.Length - signatureSize);
                return true;
            }

            return false;
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

                        if (!header->IsValid())
                        {
                            // Not a MachO file.
                            return false;
                        }

                        Verify(header->Is64BitExecutable(), MachOFormatError.Not64BitExe);

                        file += sizeof(MachHeader);
                        SegmentCommand64* linkEdit = null;
                        SymtabCommand* symtab = null;
                        LinkEditDataCommand* signature = null;

                        for (uint i = 0; i < header->ncmds; i++)
                        {
                            LoadCommand* command = (LoadCommand*)file;
                            if (command->cmd == Command.LC_SEGMENT_64)
                            {
                                SegmentCommand64* segment = (SegmentCommand64*)file;
                                if (segment->SegName.Equals("__LINKEDIT"))
                                {
                                    Verify(linkEdit == null, MachOFormatError.DuplicateLinkEdit);
                                    linkEdit = segment;
                                }
                            }
                            else if (command->cmd == Command.LC_SYMTAB)
                            {
                                Verify(symtab == null, MachOFormatError.DuplicateSymtab);
                                symtab = (SymtabCommand*)command;
                            }

                            file += command->cmdsize;
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
                        linkEdit->filesize = fileLength - linkEdit->fileoff;
                        linkEdit->vmsize = linkEdit->filesize;
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
