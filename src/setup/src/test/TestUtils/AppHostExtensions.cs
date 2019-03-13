// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class AppHostExtensions
    {
        // See: https://en.wikipedia.org/wiki/Knuth%E2%80%93Morris%E2%80%93Pratt_algorithm
        static int[] ComputeKMP_FailureFunction(byte[] pattern)
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
        static unsafe int KMP_Search(byte[] pattern, byte* bytes, int length)
        {
            int m = 0;
            int i = 0;
            int[] table = ComputeKMP_FailureFunction(pattern);

            while (m + i < length)
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

        public static unsafe int SearchAndReplace(string filePath, byte[] search, byte[] replace, bool terminateWithNul)
        {
            // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, false))
            {
                using (var mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream, null, fileStream.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                {
                    using (var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
                    {
                        var safeBuffer = accessor.SafeMemoryMappedViewHandle;
                        int offset = KMP_Search(search, (byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
                        if (offset < 0)
                        {
                            Console.WriteLine("The search pattern was not found in the file.");
                            return -1;
                        }
                        foreach (var b in replace)
                        {
                            accessor.Write(offset++, b);
                        }
                        // Terminate with '\0'
                        if (terminateWithNul)
                        {
                            accessor.Write(offset++, (byte) 0);
                        }
                        return 0;
                    }
                }
            }
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

        public static void SetWindowsGraphicalUserInterfaceBit(string appHostPath)
        {
            // Re-write ModifiedAppHostPath with the proper contents.
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostPath))
            {
                using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                {
                    SetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }
        }

        /// <summary>
        /// If the apphost file is a windows PE file (checked by looking at the first few bytes)
        /// this method will set its subsystem to GUI.
        /// </summary>
        /// <param name="accessor">The memory accessor which has the apphost file opened.</param>
        /// <param name="appHostSourcePath">The path to the source apphost.</param>
        private static unsafe void SetWindowsGraphicalUserInterfaceBit(
            MemoryMappedViewAccessor accessor)
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
                    throw new Exception("apphost is not a Windows exe.");
                }

                UInt32 peHeaderOffset = ((UInt32*)(bytes + PEHeaderPointerOffset))[0];

                if (accessor.Capacity < peHeaderOffset + SubsystemOffset + sizeof(UInt16))
                {
                    throw new Exception("apphost is not a Windows exe.");
                }

                UInt16* subsystem = ((UInt16*)(bytes + peHeaderOffset + SubsystemOffset));

                // https://docs.microsoft.com/en-us/windows/desktop/Debug/pe-format#windows-subsystem
                // The subsystem of the prebuilt apphost should be set to CUI
                if (subsystem[0] != WindowsCUISubsystem)
                {
                    throw new Exception("apphost is not a Windows CLI.");
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
    }
}

