// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.AppHost
{
    public static class BinaryUtils
    {
        internal static unsafe void SearchAndReplace(
            MemoryMappedViewAccessor accessor,
            byte[] searchPattern,
            byte[] patternToReplace,
            bool pad0s = true)
        {
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                int position = KMPSearch(searchPattern, bytes, accessor.Capacity);
                if (position < 0)
                {
                    throw new PlaceHolderNotFoundInAppHostException(searchPattern);
                }

                accessor.WriteArray(
                    position: position,
                    array: patternToReplace,
                    offset: 0,
                    count: patternToReplace.Length);

                if (pad0s)
                {
                    Pad0(searchPattern, patternToReplace, bytes, position);
                }
            }
            finally
            {
                if (pointer != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }

        private static unsafe void Pad0(byte[] searchPattern, byte[] patternToReplace, byte* bytes, int offset)
        {
            if (patternToReplace.Length < searchPattern.Length)
            {
                for (int i = patternToReplace.Length; i < searchPattern.Length; i++)
                {
                    bytes[i + offset] = 0x0;
                }
            }
        }

        public static unsafe void SearchAndReplace(
            string filePath, 
            byte[] searchPattern, 
            byte[] patternToReplace,
            bool pad0s = true)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    SearchAndReplace(accessor, searchPattern, patternToReplace, pad0s);
                }
            }
        }

        internal static unsafe int SearchInFile(MemoryMappedViewAccessor accessor, byte[] searchPattern)
        {
            var safeBuffer = accessor.SafeMemoryMappedViewHandle;
            return KMPSearch(searchPattern, (byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
        }

        public static unsafe int SearchInFile(string filePath, byte[] searchPattern)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                {
                    return SearchInFile(accessor, searchPattern);
                }
            }
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static int[] ComputeKMPFailureFunction(byte[] pattern)
        {
            int[] table = new int[pattern.Length];
            if (pattern.Length >= 1)
            {
                table[0] = -1;
            }
            if (pattern.Length >= 2)
            {
                table[1] = 0;
            }

            int pos = 2;
            int cnd = 0;
            while (pos < pattern.Length)
            {
                if (pattern[pos - 1] == pattern[cnd])
                {
                    table[pos] = cnd + 1;
                    cnd++;
                    pos++;
                }
                else if (cnd > 0)
                {
                    cnd = table[cnd];
                }
                else
                {
                    table[pos] = 0;
                    pos++;
                }
            }
            return table;
        }

        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        private static unsafe int KMPSearch(byte[] pattern, byte* bytes, long bytesLength)
        {
            int m = 0;
            int i = 0;
            int[] table = ComputeKMPFailureFunction(pattern);

            while (m + i < bytesLength)
            {
                if (pattern[i] == bytes[m + i])
                {
                    if (i == pattern.Length - 1)
                    {
                        return m;
                    }
                    i++;
                }
                else
                {
                    if (table[i] > -1)
                    {
                        m = m + i - table[i];
                        i = table[i];
                    }
                    else
                    {
                        m++;
                        i = 0;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// The first two bytes of a PE file are a constant signature.
        /// </summary>
        private const UInt16 PEFileSignature = 0x5A4D;

        /// <summary>
        /// The offset of the PE header pointer in the DOS header.
        /// </summary>
        private const int PEHeaderPointerOffset = 0x3C;

        /// <summary>
        /// The offset of the Subsystem field in the PE header.
        /// </summary>
        private const int SubsystemOffset = 0x5C;

        /// <summary>
        /// The value of the sybsystem field which indicates Windows GUI (Graphical UI)
        /// </summary>
        private const UInt16 WindowsGUISubsystem = 0x2;

        /// <summary>
        /// The value of the subsystem field which indicates Windows CUI (Console)
        /// </summary>
        private const UInt16 WindowsCUISubsystem = 0x3;

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
                if (((UInt16*)bytes)[0] != PEFileSignature || accessor.Capacity < PEHeaderPointerOffset + sizeof(UInt32))
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
                UInt32 peHeaderOffset = ((UInt32*)(bytes + PEHeaderPointerOffset))[0];

                if (accessor.Capacity < peHeaderOffset + SubsystemOffset + sizeof(UInt16))
                {
                    throw new AppHostNotPEFileException();
                }

                UInt16* subsystem = ((UInt16*)(bytes + peHeaderOffset + SubsystemOffset));

                // https://docs.microsoft.com/en-us/windows/desktop/Debug/pe-format#windows-subsystem
                // The subsystem of the prebuilt apphost should be set to CUI
                if (subsystem[0] != WindowsCUISubsystem)
                {
                    throw new AppHostNotCUIException();
                }

                // Set the subsystem to GUI
                subsystem[0] = WindowsGUISubsystem;
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
        internal static unsafe UInt16 GetWindowsGraphicalUserInterfaceBit(MemoryMappedViewAccessor accessor)
        {
            byte* pointer = null;

            try
            {
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                byte* bytes = pointer + accessor.PointerOffset;

                // https://en.wikipedia.org/wiki/Portable_Executable
                UInt32 peHeaderOffset = ((UInt32*)(bytes + PEHeaderPointerOffset))[0];

                if (accessor.Capacity < peHeaderOffset + SubsystemOffset + sizeof(UInt16))
                {
                    throw new AppHostNotPEFileException();
                }

                UInt16* subsystem = ((UInt16*)(bytes + peHeaderOffset + SubsystemOffset));

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

        public static unsafe UInt16 GetWindowsGraphicalUserInterfaceBit(string filePath)
        {
            using (var mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (var accessor = mappedFile.CreateViewAccessor())
                {
                    return GetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }
        }
		
        public static void CopyFile(string sourcePath, string destinationPath)
        {
            var destinationDirectory = new FileInfo(destinationPath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy file to destination path so it inherits the same attributes/permissions.
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

    }
}
