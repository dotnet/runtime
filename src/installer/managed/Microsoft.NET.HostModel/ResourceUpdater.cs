// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.NET.HostModel.Win32Resources;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Provides methods for modifying the embedded native resources in a PE image.
    /// </summary>
    public class ResourceUpdater : IDisposable
    {
        private readonly FileStream stream;
        private readonly PEReader _reader;
        private ResourceData _resourceData;
        private readonly bool leaveOpen;

        /// <summary>
        /// Create a resource updater for the given PE file.
        /// Resources can be added to this updater, which will queue them for update.
        /// The target PE file will not be modified until Update() is called, after
        /// which the ResourceUpdater can not be used for further updates.
        /// </summary>
        public ResourceUpdater(string peFile)
            : this(new FileStream(peFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }

        /// <summary>
        /// Create a resource updater for the given PE file. This
        /// Resources can be added to this updater, which will queue them for update.
        /// The target PE file will not be modified until Update() is called, after
        /// which the ResourceUpdater can not be used for further updates.
        /// </summary>
        public ResourceUpdater(FileStream stream, bool leaveOpen = false)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
            try
            {
                this.stream.Seek(0, SeekOrigin.Begin);
                _reader = new PEReader(this.stream, PEStreamOptions.LeaveOpen);
                _resourceData = new ResourceData(_reader);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    this.stream?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Add all resources from a source PE file. It is assumed
        /// that the input is a valid PE file. If it is not, an
        /// exception will be thrown. This will not modify the target
        /// until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResourcesFromPEImage(string peFile)
        {
            if (_resourceData == null)
                ThrowExceptionForInvalidUpdate();

            using var module = new PEReader(File.Open(peFile, FileMode.Open, FileAccess.Read, FileShare.Read));
            var moduleResources = new ResourceData(module);
            _resourceData.CopyResourcesFrom(moduleResources);
            return this;
        }

        internal static bool IsIntResource(IntPtr lpType)
        {
            return ((uint)lpType >> 16) == 0;
        }

        private const int LangID_LangNeutral_SublangNeutral = 0;

        /// <summary>
        /// Add a language-neutral integer resource from a byte[] with
        /// a particular type and name. This will not modify the
        /// target until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResource(byte[] data, IntPtr lpType, IntPtr lpName)
        {
            if (!IsIntResource(lpType) || !IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource types");
            }
            if (_resourceData == null)
                ThrowExceptionForInvalidUpdate();

            _resourceData.AddResource((ushort)lpName, (ushort)lpType, LangID_LangNeutral_SublangNeutral, data);

            return this;
        }

        /// <summary>
        /// Add a language-neutral integer resource from a byte[] with
        /// a particular type and name. This will not modify the
        /// target until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResource(byte[] data, string lpType, IntPtr lpName)
        {
            if (!IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource names");
            }
            if (_resourceData == null)
                ThrowExceptionForInvalidUpdate();

            _resourceData.AddResource((ushort)lpName, lpType, LangID_LangNeutral_SublangNeutral, data);

            return this;
        }

        /// <summary>
        /// Write the pending resource updates to the target PE
        /// file. After this, the ResourceUpdater no longer maintains
        /// an update handle, and can not be used for further updates.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public void Update()
        {
            if (_resourceData == null)
                ThrowExceptionForInvalidUpdate();

            int resourceSectionIndex = _reader.PEHeaders.SectionHeaders.Length;
            for (int i = 0; i < _reader.PEHeaders.SectionHeaders.Length; i++)
            {
                if (_reader.PEHeaders.SectionHeaders[i].Name == ".rsrc")
                {
                    resourceSectionIndex = i;
                    break;
                }
            }

            int fileAlignment = _reader.PEHeaders.PEHeader!.FileAlignment;
            int sectionAlignment = _reader.PEHeaders.PEHeader!.SectionAlignment;

            bool needsAddSection = resourceSectionIndex == _reader.PEHeaders.SectionHeaders.Length;
            bool isRsrcIsLastSection;
            int rsrcPointerToRawData;
            int rsrcVirtualAddress;
            int rsrcOriginalRawDataSize;
            int rsrcOriginalVirtualSize;
            if (needsAddSection)
            {
                isRsrcIsLastSection = true;

                SectionHeader lastSection = _reader.PEHeaders.SectionHeaders.Last();
                rsrcPointerToRawData =
                    GetAligned(lastSection.PointerToRawData + lastSection.SizeOfRawData, fileAlignment);
                rsrcVirtualAddress = GetAligned(lastSection.VirtualAddress + lastSection.VirtualSize, sectionAlignment);
                rsrcOriginalRawDataSize = 0;
                rsrcOriginalVirtualSize = 0;
            }
            else
            {
                isRsrcIsLastSection = _reader.PEHeaders.SectionHeaders.Length - 1 == resourceSectionIndex;

                SectionHeader resourceSection = _reader.PEHeaders.SectionHeaders[resourceSectionIndex];
                rsrcPointerToRawData = resourceSection.PointerToRawData;
                rsrcVirtualAddress = resourceSection.VirtualAddress;
                rsrcOriginalRawDataSize = resourceSection.SizeOfRawData;
                rsrcOriginalVirtualSize = resourceSection.VirtualSize;
            }

            var objectDataBuilder = new ObjectDataBuilder();
            _resourceData.WriteResources(rsrcVirtualAddress, ref objectDataBuilder);
            var rsrcSectionData = objectDataBuilder.ToData();

            int rsrcSectionDataSize = rsrcSectionData.Length;
            int newSectionSize = GetAligned(rsrcSectionDataSize, fileAlignment);
            int newSectionVirtualSize = GetAligned(rsrcSectionDataSize, sectionAlignment);

            int delta = newSectionSize - GetAligned(rsrcOriginalRawDataSize, fileAlignment);
            int virtualDelta = newSectionVirtualSize - GetAligned(rsrcOriginalVirtualSize, sectionAlignment);

            int trailingSectionVirtualStart = rsrcVirtualAddress + rsrcOriginalVirtualSize;
            int trailingSectionStart = rsrcPointerToRawData + rsrcOriginalRawDataSize;
            int trailingSectionLength = (int)(stream.Length - trailingSectionStart);

            bool needsMoveTrailingSections = !isRsrcIsLastSection && delta > 0;
            long finalImageSize = trailingSectionStart + Math.Max(delta, 0) + trailingSectionLength;

            using (var mmap = MemoryMappedFile.CreateFromFile(stream, null, finalImageSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            using (MemoryMappedViewAccessor accessor = mmap.CreateViewAccessor(0, finalImageSize, MemoryMappedFileAccess.ReadWrite))
            {
                int peSignatureOffset = ReadI32(accessor, PEOffsets.DosStub.PESignatureOffset);
                int sectionBase = peSignatureOffset + PEOffsets.PEHeaderSize +
                                  (ushort)_reader.PEHeaders.CoffHeader.SizeOfOptionalHeader;

                if (needsAddSection)
                {
                    int resourceSectionBase = sectionBase + PEOffsets.OneSectionHeaderSize * resourceSectionIndex;
                    // ensure we have space for new section header
                    if (resourceSectionBase + PEOffsets.OneSectionHeaderSize >
                        _reader.PEHeaders.SectionHeaders[0].PointerToRawData)
                        throw new InvalidOperationException("Cannot add section header");

                    WriteI32(accessor, peSignatureOffset + PEOffsets.PEHeader.NumberOfSections, resourceSectionIndex + 1);

                    // section name ".rsrc\0\0\0" = 2E 72 73 72 63 00 00 00
                    accessor.Write(resourceSectionBase + 0, (byte)0x2E);
                    accessor.Write(resourceSectionBase + 1, (byte)0x72);
                    accessor.Write(resourceSectionBase + 2, (byte)0x73);
                    accessor.Write(resourceSectionBase + 3, (byte)0x72);
                    accessor.Write(resourceSectionBase + 4, (byte)0x63);
                    accessor.Write(resourceSectionBase + 5, (byte)0x00);
                    accessor.Write(resourceSectionBase + 6, (byte)0x00);
                    accessor.Write(resourceSectionBase + 7, (byte)0x00);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.VirtualSize, rsrcSectionDataSize);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.VirtualAddress, rsrcVirtualAddress);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.RawSize, newSectionSize);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.RawPointer, rsrcPointerToRawData);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.RelocationsPointer, 0);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.LineNumbersPointer, 0);
                    WriteI16(accessor, resourceSectionBase + PEOffsets.SectionHeader.NumberOfRelocations, 0);
                    WriteI16(accessor, resourceSectionBase + PEOffsets.SectionHeader.NumberOfLineNumbers, 0);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.SectionCharacteristics,
                        (int)(SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead));
                }

                if (needsMoveTrailingSections)
                {
                    byte[] moveTrailingSectionBuffer = new byte[trailingSectionLength];
                    accessor.ReadArray(trailingSectionStart, moveTrailingSectionBuffer, 0, trailingSectionLength);
                    accessor.WriteArray(trailingSectionStart + delta, moveTrailingSectionBuffer, 0, trailingSectionLength);

                    for (int i = resourceSectionIndex + 1; i < _reader.PEHeaders.SectionHeaders.Length; i++)
                    {
                        int currentSectionBase = sectionBase + PEOffsets.OneSectionHeaderSize * i;

                        ModifyI32(accessor, currentSectionBase + PEOffsets.SectionHeader.VirtualAddress,
                            pointer => pointer + virtualDelta);
                        ModifyI32(accessor, currentSectionBase + PEOffsets.SectionHeader.RawPointer,
                            pointer => pointer + delta);
                    }
                }

                if (rsrcSectionDataSize != rsrcOriginalVirtualSize)
                {
                    // update size of .rsrc section
                    int resourceSectionBase = sectionBase + PEOffsets.OneSectionHeaderSize * resourceSectionIndex;
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.VirtualSize, rsrcSectionDataSize);
                    WriteI32(accessor, resourceSectionBase + PEOffsets.SectionHeader.RawSize, newSectionSize);

                    void PatchRVA(int offset)
                    {
                        ModifyI32(accessor, offset,
                            pointer => pointer >= trailingSectionVirtualStart ? pointer + virtualDelta : pointer);
                    }

                    int dataDirectoriesOffset = _reader.PEHeaders.PEHeader.Magic == PEMagic.PE32Plus
                        ? peSignatureOffset + PEOffsets.PEHeader.PE64DataDirectories
                        : peSignatureOffset + PEOffsets.PEHeader.PE32DataDirectories;

                    // fix header
                    ModifyI32(accessor, peSignatureOffset + PEOffsets.PEHeader.InitializedDataSize,
                        size => size + delta);
                    ModifyI32(accessor, peSignatureOffset + PEOffsets.PEHeader.SizeOfImage,
                        size => size + virtualDelta);

                    if (needsMoveTrailingSections)
                    {
                        // fix RVA in DataDirectory
                        for (int i = 0; i < _reader.PEHeaders.PEHeader.NumberOfRvaAndSizes; i++)
                            PatchRVA(dataDirectoriesOffset + i * PEOffsets.DataDirectoryEntrySize +
                                     PEOffsets.DataDirectoryEntry.VirtualAddressOffset);
                    }

                    // update the ResourceTable in DataDirectories
                    int resourceTableOffset = dataDirectoriesOffset + PEOffsets.ResourceTableDataDirectoryIndex * PEOffsets.DataDirectoryEntrySize;
                    WriteI32(accessor, resourceTableOffset + PEOffsets.DataDirectoryEntry.VirtualAddressOffset, rsrcVirtualAddress);
                    WriteI32(accessor, resourceTableOffset + PEOffsets.DataDirectoryEntry.SizeOffset, rsrcSectionDataSize);
                }

                accessor.WriteArray(rsrcPointerToRawData, rsrcSectionData, 0, rsrcSectionDataSize);

                // clear rest
                //Array.Fill is standard 2.1
                for (int i = rsrcSectionDataSize; i < newSectionSize; i++)
                    accessor.Write(rsrcPointerToRawData + i, (byte)0);

                _resourceData = null;
            }
        }

        private static int ReadI32(MemoryMappedViewAccessor buffer, int position)
        {
            return buffer.ReadByte(position + 0)
                        | (buffer.ReadByte(position + 1) << 8)
                        | (buffer.ReadByte(position + 2) << 16)
                        | (buffer.ReadByte(position + 3) << 24);
        }

        private static void WriteI32(MemoryMappedViewAccessor buffer, int position, int data)
        {
            buffer.Write(position + 0, (byte)(data & 0xFF));
            buffer.Write(position + 1, (byte)(data >> 8 & 0xFF));
            buffer.Write(position + 2, (byte)(data >> 16 & 0xFF));
            buffer.Write(position + 3, (byte)(data >> 24 & 0xFF));
        }
        private static void WriteI16(MemoryMappedViewAccessor buffer, int position, short data)
        {
            buffer.Write(position + 0, (byte)(data & 0xFF));
            buffer.Write(position + 1, (byte)(data >> 8 & 0xFF));
        }

        private static void ModifyI32(MemoryMappedViewAccessor buffer, int position, Func<int, int> modifier) =>
            WriteI32(buffer, position, modifier(ReadI32(buffer, position)));

        public static int GetAligned(int integer, int alignWith) => (integer + alignWith - 1) & ~(alignWith - 1);

        private static void ThrowExceptionForInvalidUpdate()
        {
            throw new InvalidOperationException(
                "Update handle is invalid. This instance may not be used for further updates");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing && !leaveOpen)
            {
                _reader.Dispose();
                stream.Dispose();
            }
        }
    }
}
