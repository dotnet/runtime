// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.PortableExecutable
{
    public struct SectionHeader
    {
        private readonly int _virtualSize;
        private readonly string _name;
        private readonly int _virtualAddress;
        private readonly int _sizeOfRawData;
        private readonly int _pointerToRawData;
        private readonly int _pointerToRelocations;
        private readonly int _pointerToLineNumbers;
        private readonly ushort _numberOfRelocations;
        private readonly ushort _numberOfLineNumbers;
        private readonly SectionCharacteristics _sectionCharacteristics;

        /// <summary>
        /// An 8-byte, null-padded UTF-8 encoded string. If the string is exactly 8 characters long, there is no terminating null. 
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// The total size of the section when loaded into memory. 
        /// If this value is greater than <see cref="SizeOfRawData"/>, the section is zero-padded. 
        /// This field is valid only for PE images and should be set to zero for object files.
        /// </summary>
        public int VirtualSize { get { return _virtualSize; } }

        /// <summary>
        /// For PE images, the address of the first byte of the section relative to the image base when the 
        /// section is loaded into memory. For object files, this field is the address of the first byte before
        /// relocation is applied; for simplicity, compilers should set this to zero. Otherwise, 
        /// it is an arbitrary value that is subtracted from offsets during relocation.
        /// </summary>
        public int VirtualAddress { get { return _virtualAddress; } }

        /// <summary>
        /// The size of the section (for object files) or the size of the initialized data on disk (for image files).
        /// For PE images, this must be a multiple of <see cref="PEHeader.FileAlignment"/>.
        /// If this is less than <see cref="VirtualSize"/>, the remainder of the section is zero-filled. 
        /// Because the <see cref="SizeOfRawData"/> field is rounded but the <see cref="VirtualSize"/> field is not, 
        /// it is possible for <see cref="SizeOfRawData"/> to be greater than <see cref="VirtualSize"/> as well.
        ///  When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public int SizeOfRawData { get { return _sizeOfRawData; } }

        /// <summary>
        /// The file pointer to the first page of the section within the COFF file. 
        /// For PE images, this must be a multiple of <see cref="PEHeader.FileAlignment"/>. 
        /// For object files, the value should be aligned on a 4 byte boundary for best performance. 
        /// When a section contains only uninitialized data, this field should be zero.
        /// </summary>
        public int PointerToRawData { get { return _pointerToRawData; } }

        /// <summary>
        /// The file pointer to the beginning of relocation entries for the section.
        /// This is set to zero for PE images or if there are no relocations.
        /// </summary>
        public int PointerToRelocations { get { return _pointerToRelocations; } }

        /// <summary>
        /// The file pointer to the beginning of line-number entries for the section. 
        /// This is set to zero if there are no COFF line numbers. 
        /// This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        public int PointerToLineNumbers { get { return _pointerToLineNumbers; } }

        /// <summary>
        /// The number of relocation entries for the section. This is set to zero for PE images.
        /// </summary>
        public ushort NumberOfRelocations { get { return _numberOfRelocations; } }

        /// <summary>
        /// The number of line-number entries for the section.
        ///  This value should be zero for an image because COFF debugging information is deprecated.
        /// </summary>
        public ushort NumberOfLineNumbers { get { return _numberOfLineNumbers; } }

        /// <summary>
        /// The flags that describe the characteristics of the section. 
        /// </summary>
        public SectionCharacteristics SectionCharacteristics { get { return _sectionCharacteristics; } }

        internal SectionHeader(ref PEBinaryReader reader)
        {
            _name = reader.ReadUTF8(PEFileConstants.SizeofSectionName);
            _virtualSize = reader.ReadInt32();
            _virtualAddress = reader.ReadInt32();
            _sizeOfRawData = reader.ReadInt32();
            _pointerToRawData = reader.ReadInt32();
            _pointerToRelocations = reader.ReadInt32();
            _pointerToLineNumbers = reader.ReadInt32();
            _numberOfRelocations = reader.ReadUInt16();
            _numberOfLineNumbers = reader.ReadUInt16();
            _sectionCharacteristics = (SectionCharacteristics)reader.ReadUInt32();
        }
    }
}