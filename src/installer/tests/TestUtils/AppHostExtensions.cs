// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.AppHost;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;

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
        /// The value of the subsystem field which indicates Windows GUI (Graphical UI)
        /// </summary>
        private const UInt16 WindowsGUISubsystem = 0x2;

        /// <summary>
        /// The value of the subsystem field which indicates Windows CUI (Console)
        /// </summary>
        private const UInt16 WindowsCUISubsystem = 0x3;

        public static void SetWindowsGraphicalUserInterfaceBit(string appHostPath)
        {
            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            string tempPath = appHostPath + ".tmp";
            File.Copy(appHostPath, tempPath, true);
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(tempPath))
            {
                using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                {
                    SetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }
            File.Move(tempPath, appHostPath, true);
        }

        public static void BindAppHost(string appHostPath)
        {
            string appDll = $"{Path.GetFileNameWithoutExtension(appHostPath)}.dll";

            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            string tempPath = appHostPath + ".tmp";
            File.Copy(appHostPath, tempPath, true);
            using (var sha256 = SHA256.Create())
            {
                // Replace the hash with the managed DLL name.
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("foobar"));
                var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
                BinaryUtils.SearchAndReplace(tempPath, Encoding.UTF8.GetBytes(hashStr), Encoding.UTF8.GetBytes(appDll));
            }
            File.Move(tempPath, appHostPath, true);
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

