// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.DependencyAnalysis;
using ILCompiler.Win32Resources;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Provides methods for modifying the embedded native resources
    /// in a PE image. It currently only works on Windows, because it
    /// requires various kernel32 APIs.
    /// </summary>
    public class ResourceUpdater : IDisposable
    {
        private readonly Stream stream;
        private readonly PEReader _reader;
        private readonly ResourceData _resourceData;
        private readonly bool leaveOpen;

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
            : this(new FileStream(peFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
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
        public ResourceUpdater(Stream stream, bool leaveOpen = false)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
            try
            {
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
            using var module = new PEReader(File.Open(peFile, FileMode.Open));
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

            _resourceData.AddResource((ushort)lpType, (ushort)lpName, LangID_LangNeutral_SublangNeutral, data);

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

            _resourceData.AddResource(lpType, (ushort)lpName, LangID_LangNeutral_SublangNeutral, data);

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
            const int peMagicSize = 4;
            const int peHeaderSize = 20;
            const int oneSectionHeaderSize = 40;
            // offset relative to Lfanew, which is pointer to first byte in header
            const int optionalHeaderBase = peMagicSize + peHeaderSize;
            const int pe64InitializedDataSizeOffset = optionalHeaderBase + 8;
            const int pe64SizeOfImageOffset = optionalHeaderBase + 56;
            const int pe64DataDirectoriesOffset = optionalHeaderBase + 112;
            const int pe32InitializedDataSizeOffset = optionalHeaderBase + 12;
            const int pe32SizeOfImageOffset = optionalHeaderBase + 56;
            const int pe32DataDirectoriesOffset = optionalHeaderBase + 96;
            // offset relative to each section header
            const int virtualSizeOffset = 8;
            const int virtualAddressOffset = 12;
            const int rawSizeOffset = 16;
            const int rawPointerOffset = 20;

            int resourceSectionIndex = -1;
            for (int i = 0; i < _reader.PEHeaders.SectionHeaders.Length; i++)
            {
                if (_reader.PEHeaders.SectionHeaders[i].Name == ".rsrc")
                {
                    resourceSectionIndex = i;
                    break;
                }
            }

            if (resourceSectionIndex == -1)
                throw new InvalidOperationException(".rsrc section not found");

            var isRsrcIsLastSection = _reader.PEHeaders.SectionHeaders.Length - 1 == resourceSectionIndex;
            var resourceSection = _reader.PEHeaders.SectionHeaders[resourceSectionIndex];

            var objectDataBuilder = new ObjectDataBuilder(false);
            _resourceData.WriteResources(resourceSection.VirtualAddress, ref objectDataBuilder);
            var rsrcSectionData = objectDataBuilder.ToData();

            int fileAlignment = _reader.PEHeaders.PEHeader!.FileAlignment;
            int sectionAlignment = _reader.PEHeaders.PEHeader!.SectionAlignment;

            int rsrcSectionDataSize = rsrcSectionData.Length;
            int newSectionSize = GetAligned(rsrcSectionDataSize, fileAlignment);
            int newSectionVirtualSize = GetAligned(rsrcSectionDataSize, sectionAlignment);

            int delta = newSectionSize - GetAligned(resourceSection.SizeOfRawData, fileAlignment);
            int virtualDelta = newSectionVirtualSize - GetAligned(resourceSection.VirtualSize, sectionAlignment);

            int trailingSectionVirtualStart = resourceSection.VirtualAddress + resourceSection.VirtualSize;
            int trailingSectionStart = resourceSection.PointerToRawData + resourceSection.SizeOfRawData;
            int trailingSectionLength = (int)(stream.Length - trailingSectionStart);

            bool needsMoveTrailingSections = !isRsrcIsLastSection && delta > 0;
            long finalImageSize = trailingSectionStart + Math.Max(delta, 0) + trailingSectionLength;

            // I wanted to use Memory Mapped File to overwrite some part of file
            // but it's impossible to achieve open once goal because
            // CreateFromFile with currentSectionIndex is not exists in
            // netstandard 2.0. So, I use read to byte[] instead.
            byte[] buffer = new byte[finalImageSize];
            var memoryStream = new MemoryStream(buffer);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(memoryStream);

            int peSignatureOffset = ReadI32(buffer, 0x3c);
            int sectionBase = peSignatureOffset + peMagicSize + peHeaderSize + (ushort)_reader.PEHeaders.CoffHeader.SizeOfOptionalHeader;

            if (needsMoveTrailingSections)
            {
                Array.Copy(buffer, trailingSectionStart,
                    buffer, trailingSectionStart + delta,
                    trailingSectionLength);

                for (int i = resourceSectionIndex + 1; i < _reader.PEHeaders.SectionHeaders.Length; i++)
                {
                    int currentSectionBase = sectionBase + oneSectionHeaderSize * i;

                    ModifyI32(buffer, currentSectionBase + virtualAddressOffset,
                        pointer => pointer + virtualDelta);
                    ModifyI32(buffer, currentSectionBase + rawPointerOffset,
                        pointer => pointer + delta);
                }
            }

            if (rsrcSectionDataSize != resourceSection.VirtualSize)
            {
                // update size of .rsrc section
                int resourceSectionBase = sectionBase + oneSectionHeaderSize * resourceSectionIndex;
                ModifyI32(buffer, resourceSectionBase + virtualSizeOffset, _ => rsrcSectionDataSize);
                ModifyI32(buffer, resourceSectionBase + rawSizeOffset, _ => newSectionSize);

                void PatchRVA(int offset)
                {
                    ModifyI32(buffer, offset,
                        pointer => pointer >= trailingSectionVirtualStart ? pointer + virtualDelta : pointer);
                }

                // fix header
                if (_reader.PEHeaders.PEHeader.Magic == PEMagic.PE32Plus)
                {
                    ModifyI32(buffer, peSignatureOffset + pe64InitializedDataSizeOffset,
                        size => size + delta);
                    ModifyI32(buffer, peSignatureOffset + pe64SizeOfImageOffset,
                        size => size + virtualDelta);

                    if (delta > 0)
                    {
                        // fix RVA in DataDirectory
                        for (int i = 0; i < _reader.PEHeaders.PEHeader.NumberOfRvaAndSizes; i++)
                            PatchRVA(peSignatureOffset + pe64DataDirectoriesOffset + i * 8);
                    }

                    // index of ResourceTable is 2 in DataDirectories
                    ModifyI32(buffer, peSignatureOffset + pe64DataDirectoriesOffset + 2 * 8 + 4, _ => rsrcSectionDataSize);
                }
                else
                {
                    ModifyI32(buffer, peSignatureOffset + pe32InitializedDataSizeOffset, size => size + delta);
                    ModifyI32(buffer, peSignatureOffset + pe32SizeOfImageOffset, size => size + virtualDelta);

                    if (delta > 0)
                    {
                        // fix RVA in DataDirectory
                        for (int i = 0; i < _reader.PEHeaders.PEHeader.NumberOfRvaAndSizes; i++)
                            PatchRVA(peSignatureOffset + pe32DataDirectoriesOffset + i * 8);
                    }

                    // index of ResourceTable is 2 in DataDirectories
                    ModifyI32(buffer, peSignatureOffset + pe32DataDirectoriesOffset + 2 * 8 + 4, _ => rsrcSectionDataSize);
                }
            }

            Array.Copy(rsrcSectionData, 0,
                buffer, resourceSection.PointerToRawData,
                rsrcSectionDataSize);

            // clear rest
            //Array.Fill is standard 2.1
            for (int i = rsrcSectionDataSize; i < newSectionSize; i++)
                buffer[resourceSection.PointerToRawData + i] = 0;

            // write back the buffer data
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(buffer, 0, buffer.Length);
            stream.SetLength(buffer.LongLength);
            stream.Flush();
        }

        private static int ReadI32(byte[] buffer, int position)
        {
            return buffer[position + 0]
                        | (buffer[position + 1] << 8)
                        | (buffer[position + 2] << 16)
                        | (buffer[position + 3] << 24);
        }

        private static void ModifyI32(byte[] buffer, int position, Func<int, int> modifier)
        {
            int data = modifier(ReadI32(buffer, position));

            buffer[position + 0] = (byte)(data & 0xFF);
            buffer[position + 1] = (byte)(data >> 8 & 0xFF);
            buffer[position + 2] = (byte)(data >> 16 & 0xFF);
            buffer[position + 3] = (byte)(data >> 24 & 0xFF);
        }

        public static int GetAligned(int integer, int alignWith) => (integer + alignWith - 1) & ~(alignWith - 1);

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
