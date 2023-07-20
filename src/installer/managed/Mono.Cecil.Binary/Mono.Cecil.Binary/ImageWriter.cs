//
// ImageWriter.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil.Binary
{
    using System.IO;
    using System.Text;
    using Mono.Cecil.Metadata;

    public sealed class ImageWriter : BaseImageVisitor
    {
        Image m_img;
        BinaryWriter m_binaryWriter;

        Section m_rsrcSect;
        MemoryBinaryWriter m_rsrcWriter;

        public ImageWriter(Image img, BinaryWriter bw)
        {
            m_img = img;
            m_binaryWriter = bw;
        }

        public Image GetImage()
        {
            return m_img;
        }

        public uint GetAligned(uint integer, uint alignWith)
        {
            return (integer + alignWith - 1) & ~(alignWith - 1);
        }

        public void Initialize()
        {
            Image img = m_img;
            ResourceWriter resWriter = null;

            uint sectAlign = img.PEOptionalHeader.NTSpecificFields.SectionAlignment;
            uint fileAlign = img.PEOptionalHeader.NTSpecificFields.FileAlignment;

            foreach (Section s in img.Sections)
            {
                if (s.Name == Section.Resources)
                {
                    m_rsrcSect = s;
                    m_rsrcWriter = new MemoryBinaryWriter();

                    resWriter = new ResourceWriter(img, m_rsrcSect, m_rsrcWriter);
                    resWriter.Write();
                }
            }

            uint oldRsrcSectSizeOfData = m_rsrcSect?.SizeOfRawData ?? 0;

            // size computations, fields setting, etc.
            uint nbSects = (uint)img.Sections.Count;
            img.PEFileHeader.NumberOfSections = (ushort)nbSects;

            if (m_rsrcSect != null)
                m_rsrcSect.VirtualSize = (uint)m_rsrcWriter.BaseStream.Length;

            var rvaMapping = new RVAMapping[img.Sections.Count];

            // start counting before sections headers
            // section start + section header sixe * number of sections
            uint headersEnd = 0x178 + 0x28 * nbSects;
            uint fileOffset = headersEnd;
            uint sectOffset = sectAlign;
            uint imageSize = 0;

            for (int i = 0; i < img.Sections.Count; i++)
            {
                Section sect = img.Sections[i];
                fileOffset = GetAligned(fileOffset, fileAlign);
                sectOffset = GetAligned(sectOffset, sectAlign);

                rvaMapping[i] = new RVAMapping(sect.VirtualAddress, sectOffset, sect.SizeOfRawData);
                sect.PointerToRawData = new RVA(fileOffset);
                sect.VirtualAddress = new RVA(sectOffset);
                sect.SizeOfRawData = GetAligned(sect.VirtualSize, fileAlign);

                fileOffset += sect.SizeOfRawData;
                sectOffset += sect.SizeOfRawData;
                imageSize += GetAligned(sect.SizeOfRawData, sectAlign);
            }

            MapRVAPointers(rvaMapping, img);

            if (resWriter != null)
                resWriter.Patch();

            img.PEOptionalHeader.StandardFields.InitializedDataSize -= oldRsrcSectSizeOfData;
            if (m_rsrcSect != null)
                img.PEOptionalHeader.StandardFields.InitializedDataSize += m_rsrcSect.SizeOfRawData;

            imageSize += headersEnd;
            img.PEOptionalHeader.NTSpecificFields.ImageSize = GetAligned(imageSize, sectAlign);

            if (m_rsrcSect != null)
                img.PEOptionalHeader.DataDirectories.ResourceTable = new DataDirectory(
                    m_rsrcSect.VirtualAddress, (uint)m_rsrcWriter.BaseStream.Length);
        }

        void MapRVAPointers(RVAMapping[] mappings, Image image)
        {
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.ExportTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.ImportTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.ResourceTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.ExceptionTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.CertificateTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.BaseRelocationTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.Debug, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.Copyright, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.GlobalPtr, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.TLSTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.LoadConfigTable, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.BoundImport, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.IAT, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.DelayImportDescriptor, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.CLIHeader, mappings);
            MapDataDirectory(ref image.PEOptionalHeader.DataDirectories.Reserved, mappings);
        }

        void MapRVAPointer(ref RVA rva, RVAMapping[] mappings)
        {
            if (rva == default) return;

            foreach (RVAMapping mapping in mappings)
            {
                if (rva >= mapping.OriginalVirtualAddress &&
                    rva < mapping.OriginalVirtualAddress + mapping.SizeOfRawDataOfRawData)
                {
                    rva = rva - mapping.OriginalVirtualAddress + mapping.TargetVirtualAddress;
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(rva), "Cannot map the rva to any section");
        }

        void MapDataDirectory(ref DataDirectory directory, RVAMapping[] mappings)
        {
            if (directory == default) return;
            if (directory.VirtualAddress == default) return;
            var rva = directory.VirtualAddress;
            MapRVAPointer(ref rva, mappings);
            directory.VirtualAddress = rva;
        }

        struct RVAMapping
        {
            public readonly RVA OriginalVirtualAddress;
            public readonly RVA TargetVirtualAddress;
            public readonly uint SizeOfRawDataOfRawData;

            public RVAMapping(RVA originalVirtualAddress, RVA targetVirtualAddress, uint sizeOfRawData)
            {
                OriginalVirtualAddress = originalVirtualAddress;
                TargetVirtualAddress = targetVirtualAddress;
                SizeOfRawDataOfRawData = sizeOfRawData;
            }
        }

        public override void VisitDOSHeader(DOSHeader header)
        {
            m_binaryWriter.Write(header.Start);
            m_binaryWriter.Write(header.Lfanew);
            m_binaryWriter.Write(header.End);

            m_binaryWriter.Write((ushort)0x4550);
            m_binaryWriter.Write((ushort)0);
        }

        public override void VisitPEFileHeader(PEFileHeader header)
        {
            m_binaryWriter.Write(header.Machine);
            m_binaryWriter.Write(header.NumberOfSections);
            m_binaryWriter.Write(header.TimeDateStamp);
            m_binaryWriter.Write(header.PointerToSymbolTable);
            m_binaryWriter.Write(header.NumberOfSymbols);
            m_binaryWriter.Write(header.OptionalHeaderSize);
            m_binaryWriter.Write((ushort)header.Characteristics);
        }

        public override void VisitNTSpecificFieldsHeader(PEOptionalHeader.NTSpecificFieldsHeader header)
        {
            WriteIntOrLong(header.ImageBase);
            m_binaryWriter.Write(header.SectionAlignment);
            m_binaryWriter.Write(header.FileAlignment);
            m_binaryWriter.Write(header.OSMajor);
            m_binaryWriter.Write(header.OSMinor);
            m_binaryWriter.Write(header.UserMajor);
            m_binaryWriter.Write(header.UserMinor);
            m_binaryWriter.Write(header.SubSysMajor);
            m_binaryWriter.Write(header.SubSysMinor);
            m_binaryWriter.Write(header.Reserved);
            m_binaryWriter.Write(header.ImageSize);
            m_binaryWriter.Write(header.HeaderSize);
            m_binaryWriter.Write(header.FileChecksum);
            m_binaryWriter.Write((ushort)header.SubSystem);
            m_binaryWriter.Write(header.DLLFlags);
            WriteIntOrLong(header.StackReserveSize);
            WriteIntOrLong(header.StackCommitSize);
            WriteIntOrLong(header.HeapReserveSize);
            WriteIntOrLong(header.HeapCommitSize);
            m_binaryWriter.Write(header.LoaderFlags);
            m_binaryWriter.Write(header.NumberOfDataDir);
        }

        public override void VisitStandardFieldsHeader(PEOptionalHeader.StandardFieldsHeader header)
        {
            m_binaryWriter.Write(header.Magic);
            m_binaryWriter.Write(header.LMajor);
            m_binaryWriter.Write(header.LMinor);
            m_binaryWriter.Write(header.CodeSize);
            m_binaryWriter.Write(header.InitializedDataSize);
            m_binaryWriter.Write(header.UninitializedDataSize);
            m_binaryWriter.Write(header.EntryPointRVA.Value);
            m_binaryWriter.Write(header.BaseOfCode.Value);
            if (!header.IsPE64)
                m_binaryWriter.Write(header.BaseOfData.Value);
        }

        void WriteIntOrLong(ulong value)
        {
            if (m_img.PEOptionalHeader.StandardFields.IsPE64)
                m_binaryWriter.Write(value);
            else
                m_binaryWriter.Write((uint)value);
        }

        public override void VisitDataDirectoriesHeader(PEOptionalHeader.DataDirectoriesHeader header)
        {
            m_binaryWriter.Write(header.ExportTable.VirtualAddress);
            m_binaryWriter.Write(header.ExportTable.Size);
            m_binaryWriter.Write(header.ImportTable.VirtualAddress);
            m_binaryWriter.Write(header.ImportTable.Size);
            m_binaryWriter.Write(header.ResourceTable.VirtualAddress);
            m_binaryWriter.Write(header.ResourceTable.Size);
            m_binaryWriter.Write(header.ExceptionTable.VirtualAddress);
            m_binaryWriter.Write(header.ExceptionTable.Size);
            m_binaryWriter.Write(header.CertificateTable.VirtualAddress);
            m_binaryWriter.Write(header.CertificateTable.Size);
            m_binaryWriter.Write(header.BaseRelocationTable.VirtualAddress);
            m_binaryWriter.Write(header.BaseRelocationTable.Size);
            m_binaryWriter.Write(header.Debug.VirtualAddress);
            m_binaryWriter.Write(header.Debug.Size);
            m_binaryWriter.Write(header.Copyright.VirtualAddress);
            m_binaryWriter.Write(header.Copyright.Size);
            m_binaryWriter.Write(header.GlobalPtr.VirtualAddress);
            m_binaryWriter.Write(header.GlobalPtr.Size);
            m_binaryWriter.Write(header.TLSTable.VirtualAddress);
            m_binaryWriter.Write(header.TLSTable.Size);
            m_binaryWriter.Write(header.LoadConfigTable.VirtualAddress);
            m_binaryWriter.Write(header.LoadConfigTable.Size);
            m_binaryWriter.Write(header.BoundImport.VirtualAddress);
            m_binaryWriter.Write(header.BoundImport.Size);
            m_binaryWriter.Write(header.IAT.VirtualAddress);
            m_binaryWriter.Write(header.IAT.Size);
            m_binaryWriter.Write(header.DelayImportDescriptor.VirtualAddress);
            m_binaryWriter.Write(header.DelayImportDescriptor.Size);
            m_binaryWriter.Write(header.CLIHeader.VirtualAddress);
            m_binaryWriter.Write(header.CLIHeader.Size);
            m_binaryWriter.Write(header.Reserved.VirtualAddress);
            m_binaryWriter.Write(header.Reserved.Size);
        }

        public override void VisitSection(Section sect)
        {
            m_binaryWriter.Write(Encoding.ASCII.GetBytes(sect.Name));
            int more = 8 - sect.Name.Length;
            for (int i = 0; i < more; i++)
                m_binaryWriter.Write((byte)0);

            m_binaryWriter.Write(sect.VirtualSize);
            m_binaryWriter.Write(sect.VirtualAddress.Value);
            m_binaryWriter.Write(sect.SizeOfRawData);
            m_binaryWriter.Write(sect.PointerToRawData.Value);
            m_binaryWriter.Write(sect.PointerToRelocations.Value);
            m_binaryWriter.Write(sect.PointerToLineNumbers.Value);
            m_binaryWriter.Write(sect.NumberOfRelocations);
            m_binaryWriter.Write(sect.NumberOfLineNumbers);
            m_binaryWriter.Write((uint)sect.Characteristics);
        }

        public override void VisitImportAddressTable(ImportAddressTable iat)
        {
        }

        public override void VisitCLIHeader(CLIHeader header)
        {
        }

        public override void VisitDebugHeader(DebugHeader header)
        {
        }

        public override void VisitImportTable(ImportTable it)
        {
        }

        public override void VisitImportLookupTable(ImportLookupTable ilt)
        {
        }

        public override void VisitHintNameTable(HintNameTable hnt)
        {
        }

        public override void TerminateImage(Image img)
        {
            if (m_rsrcSect != null)
                WriteSection(m_rsrcSect, m_rsrcWriter);
        }

        void WriteSection(Section sect, MemoryBinaryWriter sectWriter)
        {
            m_binaryWriter.BaseStream.Position = sect.VirtualAddress.Value;
            sectWriter.MemoryStream.WriteTo(m_binaryWriter.BaseStream);
            m_binaryWriter.Write(new byte [sect.SizeOfRawData - sectWriter.BaseStream.Length]);
        }
    }
}
