// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.HostModel.AppHost
{
    public static class PEUtils
    {
        /// <summary>
        /// Check whether the apphost file is a windows PE image by looking at the first few bytes.
        /// </summary>
        /// <param name="accessor">The memory accessor which has the apphost file opened.</param>
        /// <returns>true if the accessor represents a PE image, false otherwise.</returns>
        internal static unsafe bool IsPEImage(MemoryMappedViewAccessor accessor)
        {
            if (accessor.Capacity < PEOffsets.DosStub.PESignatureOffset + sizeof(uint))
                return false;

            // https://en.wikipedia.org/wiki/Portable_Executable
            // Validate that we're looking at Windows PE file
            ushort signature = AsLittleEndian(accessor.ReadUInt16(0));
            return signature == PEOffsets.DosImageSignature;
        }

        public static bool IsPEImage(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                if (reader.BaseStream.Length < PEOffsets.DosStub.PESignatureOffset + sizeof(uint))
                {
                    return false;
                }

                ushort signature = reader.ReadUInt16();
                return signature == PEOffsets.DosImageSignature;
            }
        }

        /// <summary>
        /// This method will attempt to set the subsystem to GUI. The apphost file should be a windows PE file.
        /// </summary>
        /// <param name="accessor">The memory accessor which has the apphost file opened.</param>
        internal static unsafe void SetWindowsGraphicalUserInterfaceBit(MemoryMappedViewAccessor accessor)
        {
            // https://learn.microsoft.com/windows/win32/debug/pe-format#windows-subsystem
            // The subsystem of the prebuilt apphost should be set to CUI
            uint peHeaderOffset;
            ushort subsystem = GetWindowsSubsystem(accessor, out peHeaderOffset);
            if (subsystem != (ushort)Subsystem.WindowsCui)
                throw new AppHostNotCUIException(subsystem);

            // Set the subsystem to GUI
            accessor.Write(peHeaderOffset + PEOffsets.PEHeader.Subsystem, AsLittleEndian((ushort)Subsystem.WindowsGui));
        }

        public static unsafe void SetWindowsGraphicalUserInterfaceBit(string filePath)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    SetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }
        }

        /// <summary>
        /// This method will return the subsystem CUI/GUI value. The apphost file should be a windows PE file.
        /// </summary>
        /// <param name="accessor">The memory accessor which has the apphost file opened.</param>
        internal static unsafe ushort GetWindowsGraphicalUserInterfaceBit(MemoryMappedViewAccessor accessor)
        {
            return GetWindowsSubsystem(accessor, out _);
        }

        public static unsafe ushort GetWindowsGraphicalUserInterfaceBit(string filePath)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    return GetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }
        }

        private static ushort GetWindowsSubsystem(MemoryMappedViewAccessor accessor, out uint peHeaderOffset)
        {
            // https://en.wikipedia.org/wiki/Portable_Executable
            if (accessor.Capacity < PEOffsets.DosStub.PESignatureOffset + sizeof(uint))
                throw new AppHostNotPEFileException("PESignature offset out of file range.");

            peHeaderOffset = AsLittleEndian(accessor.ReadUInt32(PEOffsets.DosStub.PESignatureOffset));
            if (accessor.Capacity < peHeaderOffset + PEOffsets.PEHeader.Subsystem + sizeof(ushort))
                throw new AppHostNotPEFileException("Subsystem offset out of file range.");

            // https://learn.microsoft.com/windows/win32/debug/pe-format#windows-subsystem
            return AsLittleEndian(accessor.ReadUInt16(peHeaderOffset + PEOffsets.PEHeader.Subsystem));
        }

        private static ushort AsLittleEndian(ushort value)
            => BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);

        private static uint AsLittleEndian(uint value)
            => BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }
}
