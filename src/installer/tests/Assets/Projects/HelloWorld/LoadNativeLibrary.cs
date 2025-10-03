// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace HelloWorld
{
    public static class LoadNativeLibrary
    {
        private const string LibraryName = "mockhostpolicy";
        private const string LibraryName_AssemblyDirectory = $"{LibraryName}-{nameof(DllImportSearchPath.AssemblyDirectory)}";
        private const string LibraryName_System32 = $"{LibraryName}-{nameof(DllImportSearchPath.System32)}";

        public static void PInvoke(DllImportSearchPath? flags)
        {
            try
            {
                switch (flags)
                {
                    case DllImportSearchPath.AssemblyDirectory:
                        corehost_unload_assembly_directory();
                        break;
                    case DllImportSearchPath.System32:
                        corehost_unload_system32();
                        break;
                    default:
                        corehost_unload();
                        break;
                }

                Console.WriteLine($"Loading {GetLibraryName(flags)} via P/Invoke (flags: {(flags.HasValue ? flags : "default")}) succeeded");
            }
            catch (DllNotFoundException e)
            {
                Console.WriteLine($"Loading {GetLibraryName(flags)} via P/Invoke (flags: {(flags.HasValue ? flags : "default")}) failed: {e}");
            }
        }

        public static void UseAPI(DllImportSearchPath? flags)
        {
            string name = GetLibraryName(flags);
            bool success = NativeLibrary.TryLoad(name, typeof(LoadNativeLibrary).Assembly, flags, out _);
            Console.WriteLine($"Loading {name} via NativeLibrary API (flags: {(flags.HasValue ? flags : "default")}) {(success ? "succeeded" : "failed")}");
        }

        private static string GetLibraryName(DllImportSearchPath? flags)
        {
            string name = flags switch
            {
                DllImportSearchPath.AssemblyDirectory => LibraryName_AssemblyDirectory,
                DllImportSearchPath.System32 => LibraryName_System32,
                _ => LibraryName
            };
            return OperatingSystem.IsWindows() ? name : $"lib{name}";
        }

        [DllImport(LibraryName)]
        private static extern int corehost_unload();

        [DllImport(LibraryName_AssemblyDirectory, EntryPoint = nameof(corehost_unload))]
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        private static extern int corehost_unload_assembly_directory();

        [DllImport(LibraryName_System32, EntryPoint = nameof(corehost_unload))]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int corehost_unload_system32();
    }
}
