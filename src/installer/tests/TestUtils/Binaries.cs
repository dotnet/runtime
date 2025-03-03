// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class Binaries
    {
        public static string GetExeName(string exeName) =>
            exeName + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);

        public static (string, string) GetSharedLibraryPrefixSuffix()
        {
            if (OperatingSystem.IsWindows())
                return (string.Empty, ".dll");

            if (OperatingSystem.IsMacOS())
                return ("lib", ".dylib");

            return ("lib", ".so");
        }

        public static string GetSharedLibraryFileNameForCurrentPlatform(string libraryName)
        {
            (string prefix, string suffix) = GetSharedLibraryPrefixSuffix();
            return prefix + libraryName + suffix;
        }

        public static class AppHost
        {
            public static string FileName = GetExeName("apphost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class CoreClr
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("coreclr");
            public static string FilePath = Path.Combine(TestContext.BuiltDotNet.GreatestVersionSharedFxPath, FileName);

            public static string MockName = GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr");
            public static string MockPath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName);
        }

        public static class DotNet
        {
            public static string FileName = GetExeName("dotnet");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class HostFxr
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("hostfxr");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string MockName_2_2 = GetSharedLibraryFileNameForCurrentPlatform("mockhostfxr_2_2");
            public static string MockName_5_0 = GetSharedLibraryFileNameForCurrentPlatform("mockhostfxr_5_0");
            public static string MockPath_2_2 = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName_2_2);
            public static string MockPath_5_0 = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName_5_0);
        }

        public static class HostPolicy
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);

            public static string MockName = GetSharedLibraryFileNameForCurrentPlatform("mockhostpolicy");
            public static string MockPath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, MockName);
        }

        public static class NetHost
        {
            public static string FileName = GetSharedLibraryFileNameForCurrentPlatform("nethost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static class SingleFileHost
        {
            public static string FileName = GetExeName("singlefilehost");
            public static string FilePath = Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, FileName);
        }

        public static (IEnumerable<string> Assemblies, IEnumerable<string> NativeLibraries) GetRuntimeFiles()
        {
            var runtimePackDir = TestContext.BuiltDotNet.GreatestVersionSharedFxPath;
            var assemblies = Directory.GetFiles(runtimePackDir, "*.dll").Where(f => IsAssembly(f));

            (string prefix, string suffix) = Binaries.GetSharedLibraryPrefixSuffix();
            var nativeLibraries = Directory.GetFiles(runtimePackDir, $"{prefix}*{suffix}").Where(f => !IsAssembly(f) && Path.GetExtension(f) != ".json");

            return (assemblies, nativeLibraries);

            static bool IsAssembly(string filePath)
            {
                if (Path.GetExtension(filePath) != ".dll")
                    return false;

                using (var fs = File.OpenRead(filePath))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(fs))
                {
                    return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
                }
            }
        }

        public static class CetCompat
        {
            // We only support CET shadow stack compatibility for Windows x64 currently
            public static bool IsSupported => OperatingSystem.IsWindows() && TestContext.BuildArchitecture == "x64";

            // https://learn.microsoft.com/windows/win32/debug/pe-format#debug-type
            private const int IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS = 20;

            // https://learn.microsoft.com/windows/win32/debug/pe-format#extended-dll-characteristics
            private const ushort IMAGE_DLLCHARACTERISTICS_EX_CET_COMPAT = 0x1;

            /// <summary>
            /// Determine if a PE image is marked with CET shadow stack compatibility
            /// </summary>
            /// <param name="filePath">Path to the image</param>
            /// <returns>True if image is marked compatible, false otherwise</returns>
            public static bool IsMarkedCompatible(string filePath)
            {
                using (PEReader reader = new PEReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
                {
                    foreach (DebugDirectoryEntry entry in reader.ReadDebugDirectory())
                    {
                        if ((int)entry.Type != IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS)
                            continue;

                        // Get the extended DLL characteristics debug directory entry
                        PEMemoryBlock data = reader.GetSectionData(entry.DataRelativeVirtualAddress);
                        ushort dllCharacteristics = data.GetReader().ReadUInt16();

                        // Check for the CET compat bit
                        return (dllCharacteristics & IMAGE_DLLCHARACTERISTICS_EX_CET_COMPAT) != 0;
                    }

                    // Not marked compatible - no debug directory entry for extended DLL characteristics
                    return false;
                }
            }

            /// <summary>
            /// Create a PE image with with CET compatability enabled/disabled
            /// </summary>
            /// <param name="setCetCompatBit">True to set CET compat bit, false to not set, null to omit extended DLL characteristics</param>
            /// <returns>PE image blob</returns>
            public static BlobBuilder CreatePEImage(bool? setCetCompatBit)
            {
                // Create a PE image with with CET compatability enabled/disabled
                DebugDirectoryBuilder debugDirectoryBuilder = new DebugDirectoryBuilder();
                if (setCetCompatBit.HasValue)
                {
                    debugDirectoryBuilder.AddEntry<ushort>(
                        (DebugDirectoryEntryType)IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS,
                        version: 0,
                        stamp: 0,
                        setCetCompatBit.Value ? IMAGE_DLLCHARACTERISTICS_EX_CET_COMPAT : (ushort)0,
                        (BlobBuilder b, ushort data) => b.WriteUInt16(data));
                }
                ManagedPEBuilder peBuilder = new ManagedPEBuilder(
                    PEHeaderBuilder.CreateExecutableHeader(),
                    new MetadataRootBuilder(new MetadataBuilder()),
                    ilStream: new BlobBuilder(),
                    debugDirectoryBuilder: debugDirectoryBuilder);

                BlobBuilder peBlob = new BlobBuilder();
                peBuilder.Serialize(peBlob);
                return peBlob;
            }
        }
    }
}
