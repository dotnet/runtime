// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace R2RDump
{
    class R2RReader
    {
        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        private readonly byte[] _image;

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// True if the image is ReadyToRun
        /// </summary>
        public bool IsR2R { get; }

        /// <summary>
        /// The type of target machine
        /// </summary>
        public Machine Machine { get; }

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory; 
        /// must be a multiple of 64K.
        /// </summary>
        public ulong ImageBase { get; }

        /// <summary>
        /// The ReadyToRun header
        /// </summary>
        public R2RHeader R2RHeader { get; }

        /// <summary>
        /// Initializes the fields of the R2RHeader
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe R2RReader(string filename)
        {
            Filename = filename;
            _image = File.ReadAllBytes(filename);

            fixed (byte* p = _image)
            {
                IntPtr ptr = (IntPtr)p;
                PEReader peReader = new PEReader(p, _image.Length);

                IsR2R = (peReader.PEHeaders.CorHeader.Flags == CorFlags.ILLibrary);
                if (!IsR2R)
                {
                    throw new System.BadImageFormatException("The file is not a ReadyToRun image");
                }

                Machine = peReader.PEHeaders.CoffHeader.Machine;
                ImageBase = peReader.PEHeaders.PEHeader.ImageBase;

                SectionHeader textSection;
                int nSections = peReader.PEHeaders.CoffHeader.NumberOfSections;
                for (int i = 0; i < nSections; i++)
                {
                    SectionHeader section = peReader.PEHeaders.SectionHeaders[i];
                    if (section.Name.Equals(".text"))
                    {
                        textSection = section;
                    }
                }

                DirectoryEntry r2rHeaderDirectory = peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                int r2rHeaderOffset = r2rHeaderDirectory.RelativeVirtualAddress - textSection.VirtualAddress + textSection.PointerToRawData;
                R2RHeader = new R2RHeader(_image, (uint)r2rHeaderDirectory.RelativeVirtualAddress, r2rHeaderOffset);
                if (r2rHeaderDirectory.Size != R2RHeader.Size)
                {
                    throw new System.BadImageFormatException("The calculated size of the R2RHeader doesn't match the size saved in the ManagedNativeHeaderDirectory");
                }
            }
        }
    }
}
