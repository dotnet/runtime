// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.MemoryMappedFiles;

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
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                // https://en.wikipedia.org/wiki/Portable_Executable
                // Validate that we're looking at Windows PE file
                if (((ushort*)bytes)[0] != PEOffsets.DosImageSignature
                    || accessor.Capacity < PEOffsets.DosStub.PESignatureOffset + sizeof(uint))
                {
                    return false;
                }
                return true;
            }
            finally
            {
                if (pointer != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
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
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                // https://en.wikipedia.org/wiki/Portable_Executable
                uint peHeaderOffset = ((uint*)(bytes + PEOffsets.DosStub.PESignatureOffset))[0];

                if (accessor.Capacity < peHeaderOffset + PEOffsets.PEHeader.Subsystem + sizeof(ushort))
                {
                    throw new AppHostNotPEFileException("Subsystem offset out of file range.");
                }

                ushort* subsystem = ((ushort*)(bytes + peHeaderOffset + PEOffsets.PEHeader.Subsystem));

                // https://docs.microsoft.com/en-us/windows/desktop/Debug/pe-format#windows-subsystem
                // The subsystem of the prebuilt apphost should be set to CUI
                if (subsystem[0] != (ushort)PEOffsets.Subsystem.WindowsCui)
                {
                    throw new AppHostNotCUIException(subsystem[0]);
                }

                // Set the subsystem to GUI
                subsystem[0] = (ushort)PEOffsets.Subsystem.WindowsGui;
            }
            finally
            {
                if (pointer != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
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
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                // https://en.wikipedia.org/wiki/Portable_Executable
                uint peHeaderOffset = ((uint*)(bytes + PEOffsets.DosStub.PESignatureOffset))[0];

                if (accessor.Capacity < peHeaderOffset + PEOffsets.PEHeader.Subsystem + sizeof(ushort))
                {
                    throw new AppHostNotPEFileException("Subsystem offset out of file range.");
                }

                ushort* subsystem = ((ushort*)(bytes + peHeaderOffset + PEOffsets.PEHeader.Subsystem));

                return subsystem[0];
            }
            finally
            {
                if (pointer != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
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
    }
}
