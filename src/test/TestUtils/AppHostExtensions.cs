// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.MemoryMappedFiles;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class AppHostExtensions
    {
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

