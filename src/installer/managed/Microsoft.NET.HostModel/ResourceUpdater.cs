// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil.Binary;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Provides methods for modifying the embedded native resources
    /// in a PE image. It currently only works on Windows, because it
    /// requires various kernel32 APIs.
    /// </summary>
    public class ResourceUpdater : IDisposable
    {
        private readonly FileStream stream;
        private readonly Image image;
        private readonly ResourceDirectoryTable resourceDirectory;

        ///<summary>
        /// Determines if the ResourceUpdater is supported by the current operating system.
        /// Some versions of Windows, such as Nano Server, do not support the needed APIs.
        /// </summary>
        public static bool IsSupportedOS()
        {
            return true;
        }

        /// <summary>
        /// Create a resource updater for the given PE file. This will
        /// acquire a native resource update handle for the file,
        /// preparing it for updates. Resources can be added to this
        /// updater, which will queue them for update. The target PE
        /// file will not be modified until Update() is called, after
        /// which the ResourceUpdater can not be used for further
        /// updates.
        /// </summary>
        public ResourceUpdater(string peFile)
        {
            stream = null;
            try
            {
                stream = new FileStream(peFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                image = ImageReader.Read(new BinaryReader(stream, Encoding.UTF8, true)).Image;
                resourceDirectory = image.ResourceDirectoryRoot;
            }
            catch (Exception)
            {
                stream?.Dispose();
                throw;
            }
        }

        private static ResourceDirectoryEntry FindOrCreateEntry(ResourceDirectoryTable table, int key)
        {
            foreach (ResourceDirectoryEntry tableEntry in table.Entries)
            {
                if (!tableEntry.IdentifiedByName && tableEntry.ID == key)
                    return tableEntry;
            }

            var newEntry = new ResourceDirectoryEntry(key);
            table.Entries.Add(newEntry);
            return newEntry;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, int key)
        {
            var entry = FindOrCreateEntry(table, key);
            if (entry.Child is ResourceDirectoryTable directory)
                return directory;
            if (entry.Child != null)
                throw new InvalidOperationException("Found entry is not Directory");
            directory = new ResourceDirectoryTable { MajorVersion = 4, MinorVersion = 0, };
            entry.Child = directory;
            return directory;
        }

        private static ResourceDirectoryEntry FindOrCreateEntry(ResourceDirectoryTable table, string key)
        {
            foreach (ResourceDirectoryEntry tableEntry in table.Entries)
            {
                if (tableEntry.IdentifiedByName && tableEntry.Name.String == key)
                    return tableEntry;
            }

            var newEntry = new ResourceDirectoryEntry(new ResourceDirectoryString(key));
            table.Entries.Add(newEntry);
            return newEntry;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, string key)
        {
            var entry = FindOrCreateEntry(table, key);
            if (entry.Child is ResourceDirectoryTable directory)
                return directory;
            if (entry.Child != null)
                throw new InvalidOperationException("Found entry is not Directory");
            directory = new ResourceDirectoryTable { MajorVersion = 4, MinorVersion = 0, };
            entry.Child = directory;
            return directory;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, ResourceDirectoryEntry keyHolder)
        {
            return keyHolder.IdentifiedByName
                ? FindOrCreateChildDirectory(table, keyHolder.Name.String)
                : FindOrCreateChildDirectory(table, keyHolder.ID);
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
            var module = ImageReader.Read(peFile).Image;

            foreach (ResourceDirectoryEntry lpType in module.ResourceDirectoryRoot.Entries)
            {
                var typeDirectory = FindOrCreateChildDirectory(resourceDirectory, lpType);
                foreach (ResourceDirectoryEntry lpName in ((ResourceDirectoryTable)lpType.Child).Entries)
                {
                    var nameDirectory = FindOrCreateChildDirectory(typeDirectory, lpName);
                    foreach (ResourceDirectoryEntry wLang in ((ResourceDirectoryTable)lpName.Child).Entries)
                    {
                        var hResource = (ResourceDataEntry)wLang.Child;
                        var entry = FindOrCreateEntry(nameDirectory, wLang.ID);
                        entry.Child = new ResourceDataEntry
                        {
                            Codepage = hResource.Codepage,
                            Reserved = hResource.Reserved,
                            ResourceData = hResource.ResourceData,
                        };
                    }
                }
            }

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

            var typeDirectory = FindOrCreateChildDirectory(resourceDirectory, (int)lpType);
            var nameDirectory = FindOrCreateChildDirectory(typeDirectory, (int)lpName);
            var entry = FindOrCreateEntry(nameDirectory, LangID_LangNeutral_SublangNeutral);
            entry.Child = new ResourceDataEntry
            {
                Codepage = 1252, // TODO?
                ResourceData = data,
            };

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

            var typeDirectory = FindOrCreateChildDirectory(resourceDirectory, lpType);
            var nameDirectory = FindOrCreateChildDirectory(typeDirectory, (int)lpName);
            var entry = FindOrCreateEntry(nameDirectory, LangID_LangNeutral_SublangNeutral);
            entry.Child = new ResourceDataEntry
            {
                Codepage = 1252, // TODO?
                ResourceData = data,
            };

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
            // 20 of data and 4 of magic
            const uint peMagicSize = 4;
            const uint peHeaderSize = 20;
            const uint oneSectionHeaderSize = 40;
            // offset relative to Lfanew, which is pointer to first byte in header
            const uint optionalHeaderBase = peMagicSize + peHeaderSize;
            const uint pe64InitializedDataSizeOffset = optionalHeaderBase + 8;
            const uint pe64SizeOfImageOffset = optionalHeaderBase + 56;
            const uint pe64DataDirectoriesOffset = optionalHeaderBase + 112;
            const uint pe32InitializedDataSizeOffset = optionalHeaderBase + 12;
            const uint pe32SizeOfImageOffset = optionalHeaderBase + 56;
            const uint pe32DataDirectoriesOffset = optionalHeaderBase + 96;
            // offset relative to each section header
            const uint virtualSizeOffset = 8;
            const uint virtualAddressOffset = 12;
            const uint rawSizeOffset = 16;
            const uint rawPointerOffset = 20;

            int resourceSectionIndex = -1;
            for (int i = 0; i < image.Sections.Count; i++)
            {
                if (image.Sections[i].Name == ".rsrc")
                {
                    resourceSectionIndex = i;
                    break;
                }
            }

            if (resourceSectionIndex == -1)
                throw new InvalidOperationException(".rsrc section not found");

            var isRsrcIsLastSection = image.Sections.Count - 1 == resourceSectionIndex;
            var resourceSection = image.Sections[resourceSectionIndex];

            var rsrcSectionData = new MemoryBinaryWriter();
            var resourceWriter = new ResourceWriter(resourceDirectory, resourceSection, rsrcSectionData);
            resourceWriter.Write();
            // patch immediate because position of rsrc will not be changed
            resourceWriter.Patch();

            uint rsrcSectionDataSize = (uint)rsrcSectionData.MemoryStream.Length;
            uint newSectionSize = GetAligned(rsrcSectionDataSize, image.PEOptionalHeader.NTSpecificFields.FileAlignment);
            uint newSectionVirtualSize = GetAligned(rsrcSectionDataSize, image.PEOptionalHeader.NTSpecificFields.SectionAlignment);

            int delta = (int)newSectionSize - (int)GetAligned(resourceSection.SizeOfRawData,
                image.PEOptionalHeader.NTSpecificFields.FileAlignment);
            int virtualDelta = (int)newSectionVirtualSize - (int)GetAligned(resourceSection.VirtualSize,
                image.PEOptionalHeader.NTSpecificFields.SectionAlignment);
            uint sectionBase = image.DOSHeader.Lfanew + peMagicSize + peHeaderSize + image.PEFileHeader.OptionalHeaderSize;

            uint trailingSectionVirtualStart = resourceSection.VirtualAddress + resourceSection.VirtualSize;
            uint trailingSectionStart = resourceSection.PointerToRawData + resourceSection.SizeOfRawData;
            uint trailingSectionLength = (uint)stream.Length - trailingSectionStart;

            bool needsMoveTrailingSections = !isRsrcIsLastSection && delta > 0;
            long finalImageSize = trailingSectionStart + Math.Max(delta, 0) + trailingSectionLength;

            // I wanted to use Memory Mapped File to overwrite some part of file
            // but it's impossible to achieve open once goal because
            // CreateFromFile with currentSectionIndex is not exists in
            // netstandard 2.0. So, I use read to byte[] instead.
            var buffer = new byte[finalImageSize];
            var memoryStream = new MemoryStream(buffer);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(memoryStream);

            if (needsMoveTrailingSections)
            {
                Array.Copy(buffer, trailingSectionStart,
                    buffer, trailingSectionStart + delta,
                    trailingSectionLength);

                for (int i = resourceSectionIndex + 1; i < image.Sections.Count; i++)
                {
                    uint currentSectionBase = sectionBase + oneSectionHeaderSize * (uint)i;

                    ModifyU32(buffer, currentSectionBase + virtualAddressOffset,
                        pointer => (uint)(pointer + virtualDelta));
                    ModifyU32(buffer, currentSectionBase + rawPointerOffset,
                        pointer => (uint)(pointer + delta));
                }
            }

            if (newSectionSize != resourceSection.SizeOfRawData)
            {
                // update size of .rsrc section
                uint resourceSectionBase = sectionBase + oneSectionHeaderSize * (uint)resourceSectionIndex;
                ModifyU32(buffer, resourceSectionBase + virtualSizeOffset, _ => rsrcSectionDataSize);
                ModifyU32(buffer, resourceSectionBase + rawSizeOffset, _ => newSectionSize);

                void PatchRVA(uint offset)
                {
                    ModifyU32(buffer, offset,
                        pointer => pointer >= trailingSectionVirtualStart ? (uint)(pointer + virtualDelta) : pointer);
                }

                // fix header
                if (image.PEOptionalHeader.StandardFields.IsPE64)
                {
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe64InitializedDataSizeOffset,
                        size => (uint)(size + delta));
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe64SizeOfImageOffset,
                        size => (uint)(size + virtualDelta));

                    if (delta > 0)
                    {
                        // fix RVA in DataDirectory
                        for (int i = 0; i < image.PEOptionalHeader.NTSpecificFields.NumberOfDataDir; i++)
                            PatchRVA((uint)(image.DOSHeader.Lfanew + pe64DataDirectoriesOffset + i * 8));
                    }

                    // index of ResourceTable is 2 in DataDirectories
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe64DataDirectoriesOffset + 2 * 8 + 4, _ => rsrcSectionDataSize);
                }
                else
                {
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe32InitializedDataSizeOffset,
                        size => (uint)(size + delta));
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe32SizeOfImageOffset,
                        size => (uint)(size + virtualDelta));

                    if (delta > 0)
                    {
                        // fix RVA in DataDirectory
                        for (int i = 0; i < image.PEOptionalHeader.NTSpecificFields.NumberOfDataDir; i++)
                            PatchRVA((uint)(image.DOSHeader.Lfanew + pe32DataDirectoriesOffset + i * 8));
                    }

                    // index of ResourceTable is 2 in DataDirectories
                    ModifyU32(buffer, image.DOSHeader.Lfanew + pe32DataDirectoriesOffset + 2 * 8 + 4, _ => rsrcSectionDataSize);
                }
            }

            Array.Copy(rsrcSectionData.MemoryStream.GetBuffer(), 0,
                buffer, resourceSection.PointerToRawData,
                rsrcSectionDataSize);

            // clear rest
            //Array.Fill is standard 2.1
            for (uint i = rsrcSectionDataSize; i < newSectionSize; i++)
                buffer[resourceSection.PointerToRawData + i] = 0;

            // write back the buffer data
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(buffer, 0, buffer.Length);
            stream.SetLength(buffer.LongLength);
            stream.Flush();
        }

        private static void ModifyU32(byte[] buffer, uint position, Func<uint, uint> modifier)
        {
            uint data = buffer[position + 0]
                   | ((uint)buffer[position + 1] << 8)
                   | ((uint)buffer[position + 2] << 16)
                   | ((uint)buffer[position + 3] << 24);

            data = modifier(data);

            buffer[position + 0] = (byte)(data & 0xFF);
            buffer[position + 1] = (byte)(data >> 8 & 0xFF);
            buffer[position + 2] = (byte)(data >> 16 & 0xFF);
            buffer[position + 3] = (byte)(data >> 24 & 0xFF);
        }

        public static uint GetAligned(uint integer, uint alignWith) => (integer + alignWith - 1) & ~(alignWith - 1);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
        }
    }
}
