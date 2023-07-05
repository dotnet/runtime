// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Build.Tasks
{
    /// <summary>
    /// Dumps native Win32 resources in the given assembly into a specified *.res file.
    /// </summary>
    public class DumpNativeResources : Task
    {
        /// <summary>
        /// File name of the assembly with Win32 resources to be dumped.
        /// </summary>
        [Required]
        public string MainAssembly
        {
            get;
            set;
        }

        /// <summary>
        /// File name into which to dump the Win32 resources.
        /// </summary>
        [Required]
        public string ResourceFile
        {
            get;
            set;
        }

        public override bool Execute()
        {
            using (FileStream fs = File.OpenRead(MainAssembly))
            using (PEReader peFile = new PEReader(fs))
            {
                DirectoryEntry resourceDirectory = peFile.PEHeaders.PEHeader.ResourceTableDirectory;
                if (resourceDirectory.Size != 0
                    && peFile.PEHeaders.TryGetDirectoryOffset(resourceDirectory, out int rsrcOffset))
                {
                    using (var bw = new BinaryWriter(File.OpenWrite(ResourceFile)))
                    {
                        ResWriter.WriteResources(peFile, rsrcOffset, resourceDirectory.Size, bw);
                    }
                }
                else
                {
                    if (File.Exists(ResourceFile))
                    {
                        try
                        {
                            File.Delete(ResourceFile);
                        }
                        catch { }
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Helper class that converts from a Win32 PE resource directory format to
    /// a set of RESOURCEHEADER structures (https://docs.microsoft.com/en-us/windows/desktop/menurc/resourceheader)
    /// that form the basis of Win32 RES files.
    /// </summary>
    internal sealed class ResWriter
    {
        private readonly PEMemoryBlock _memoryBlock;
        private readonly PEReader _peReader;
        private readonly int _rsrcOffset;
        private readonly int _rsrcSize;
        private readonly BinaryWriter _bw;

        private object _typeIdOrName;
        private object _resourceIdOrName;
        private int _languageId;

        private ResWriter(PEMemoryBlock memoryBlock, PEReader peReader, int rsrcOffset, int rsrcSize, BinaryWriter bw)
        {
            _memoryBlock = memoryBlock;
            _peReader = peReader;
            _rsrcOffset = rsrcOffset;
            _rsrcSize = rsrcSize;
            _bw = bw;
        }

        public static void WriteResources(PEReader reader, int rsrcOffset, int rsrcSize, BinaryWriter bw)
        {
            var rw = new ResWriter(reader.GetEntireImage(), reader, rsrcOffset, rsrcSize, bw);

            // First entry is a null resource entry

            bw.Write(new byte[] {
                            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00,
                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        });

            rw.DumpDirectory(reader.GetEntireImage().GetReader(rsrcOffset, rsrcSize), 0);
        }

        private void DumpDirectory(BlobReader br, int level)
        {
            // Skip characteristics
            br.ReadInt32();

            // Skip time/date stamp
            br.ReadInt32();

            // Skip time/date stamp
            br.ReadInt32();

            ushort numNamed = br.ReadUInt16();
            ushort numId = br.ReadUInt16();

            for (int i = 0; i < numNamed + numId; i++)
            {
                int nameOffsetOrId = br.ReadInt32();
                uint entryTableOrSubdirectoryOffset = br.ReadUInt32();

                if (i < numNamed)
                {
                    nameOffsetOrId = nameOffsetOrId &= 0x7FFFFFFF;
                    BlobReader nameReader = _memoryBlock.GetReader(_rsrcOffset + nameOffsetOrId, _rsrcSize - nameOffsetOrId);
                    ushort nameLength = nameReader.ReadUInt16();
                    StringBuilder sb = new StringBuilder(nameLength);
                    for (int charIndex = 0; charIndex < nameLength; charIndex++)
                        sb.Append((char)nameReader.ReadUInt16());
                    string name = sb.ToString();

                    if (level == 0)
                    {
                        _typeIdOrName = name;
                    }
                    else if (level == 1)
                    {
                        _resourceIdOrName = name;
                    }
                    else
                        throw new BadImageFormatException();
                }
                else
                {
                    if (level == 0)
                    {
                        _typeIdOrName = nameOffsetOrId;
                    }
                    else if (level == 1)
                    {
                        _resourceIdOrName = nameOffsetOrId;
                    }
                    else if (level == 2)
                    {
                        _languageId = nameOffsetOrId;
                    }
                    else
                        throw new BadImageFormatException();
                }

                if (level < 2)
                {
                    if ((entryTableOrSubdirectoryOffset & 0x80000000) == 0)
                        throw new BadImageFormatException();
                    entryTableOrSubdirectoryOffset &= 0x7FFFFFFF;
                    DumpDirectory(_memoryBlock.GetReader(_rsrcOffset + (int)entryTableOrSubdirectoryOffset, _rsrcSize - (int)entryTableOrSubdirectoryOffset), level + 1);
                }
                else
                {
                    DumpEntry(_memoryBlock.GetReader(_rsrcOffset + (int)entryTableOrSubdirectoryOffset, _rsrcSize - (int)entryTableOrSubdirectoryOffset));
                }
            }
        }

        private void DumpEntry(BlobReader br)
        {
            int dataRva = br.ReadInt32();
            int size = br.ReadInt32();

            // Skip codepage
            br.ReadInt32();

            // Skip reserved
            br.ReadInt32();

            var ms = new MemoryStream();
            var hdr = new BinaryWriter(ms, System.Text.Encoding.Unicode);

            hdr.Write(size); // DataSize
            hdr.Write(0); // HeaderSize
            hdr.WriteNameOrId(_typeIdOrName); // TYPE
            hdr.WriteNameOrId(_resourceIdOrName); // NAME

            // round up to "DWORD" offset
            long curHeaderSize = hdr.BaseStream.Position;
            while (curHeaderSize % 4 != 0)
            {
                hdr.Write((byte)0);
                curHeaderSize++;
            }

            hdr.Write(0); // DataVersion
            hdr.Write((short)0); // MemoryFlags
            hdr.Write((ushort)_languageId); // LanguageId
            hdr.Write(0); // Version
            hdr.Write(0); // Characteristics

            // Patch up HeaderSize
            var headerSize = hdr.Seek(0, SeekOrigin.Current);
            hdr.Seek(4, SeekOrigin.Begin);
            hdr.Write((int)headerSize);

            var hdrData = ms.ToArray();

            _bw.Write(hdrData);
            _bw.Write(_peReader.GetSectionData(dataRva).GetReader().ReadBytes(size));

            // Make sure we are DWORD aligned
            var totalSize = hdrData.Length + size;
            while (totalSize % 4 != 0)
            {
                _bw.Write((byte)0);
                totalSize++;
            }

        }
    }

    internal static class ResourceHelper
    {
        public static void WriteNameOrId(this BinaryWriter bw, object nameOrId)
        {
            if (nameOrId is string s)
            {
                // String
                bw.Write(s.ToCharArray());
                bw.Write((short)0);
            }
            else
            {
                // Integer
                bw.Write((short)-1);
                bw.Write((short)(int)nameOrId);
            }
        }
    }
}
