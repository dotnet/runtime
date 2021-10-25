// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using TestLibrary;
using Xunit;

using Assert = Xunit.Assert;

namespace AssemblyDependencyResolverTests
{
    class NativeDependencyTests : TestBase
    {
        string _componentDirectory;
        string _componentAssemblyPath;

        protected override void Initialize()
        {
            HostPolicyMock.Initialize(TestBasePath, CoreRoot);
            _componentDirectory = Path.Combine(TestBasePath, $"TestComponent_{Guid.NewGuid().ToString().Substring(0, 8)}");

            Directory.CreateDirectory(_componentDirectory);
            _componentAssemblyPath = CreateMockFile("TestComponent.dll");
        }

        protected override void Cleanup()
        {
            if (Directory.Exists(_componentDirectory))
            {
                Directory.Delete(_componentDirectory, recursive: true);
            }
        }

        public void TestSimpleNameAndNoPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("{0}", "{0}", OS.Windows | OS.OSX | OS.Linux);
        }

        public void TestSimpleNameAndNoPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("{0}.dll", "{0}", OS.Windows);
            ValidateNativeLibraryResolutions("{0}.dylib", "{0}", OS.OSX);
            ValidateNativeLibraryResolutions("{0}.so", "{0}", OS.Linux);
        }

        public void TestSimpleNameAndLibPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}", "{0}", OS.OSX | OS.Linux);
        }

        public void TestRelativeNameAndLibPrefixAndNoSuffix()
        {
            // The lib prefix is not added if the lookup is a relative path.
            ValidateNativeLibraryWithRelativeLookupResolutions("lib{0}", "{0}", 0);
        }

        public void TestSimpleNameAndLibPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}.dll", "{0}", 0);
            ValidateNativeLibraryResolutions("lib{0}.dylib", "{0}", OS.OSX);
            ValidateNativeLibraryResolutions("lib{0}.so", "{0}", OS.Linux);
        }

        public void TestNameWithSuffixAndNoPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("{0}", "{0}.dll", 0);
            ValidateNativeLibraryResolutions("{0}", "{0}.dylib", 0);
            ValidateNativeLibraryResolutions("{0}", "{0}.so", 0);
        }

        public void TestNameWithSuffixAndNoPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("{0}.dll", "{0}.dll", OS.Windows | OS.OSX | OS.Linux);
            ValidateNativeLibraryResolutions("{0}.dylib", "{0}.dylib", OS.Windows | OS.OSX | OS.Linux);
            ValidateNativeLibraryResolutions("{0}.so", "{0}.so", OS.Windows | OS.OSX | OS.Linux);
        }

        public void TestNameWithSuffixAndNoPrefixAndDoubleSuffix()
        {
            // Unixes add the suffix even if one is already present.
            ValidateNativeLibraryResolutions("{0}.dll.dll", "{0}.dll", 0);
            ValidateNativeLibraryResolutions("{0}.dylib.dylib", "{0}.dylib", OS.OSX);
            ValidateNativeLibraryResolutions("{0}.so.so", "{0}.so", OS.Linux);
        }

        public void TestNameWithSuffixAndPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}", "{0}.dll", 0);
            ValidateNativeLibraryResolutions("lib{0}", "{0}.dylib", 0);
            ValidateNativeLibraryResolutions("lib{0}", "{0}.so", 0);
        }

        public void TestNameWithSuffixAndPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}.dll", "{0}.dll", OS.OSX | OS.Linux);
            ValidateNativeLibraryResolutions("lib{0}.dylib", "{0}.dylib", OS.OSX | OS.Linux);
            ValidateNativeLibraryResolutions("lib{0}.so", "{0}.so", OS.OSX | OS.Linux);
        }

        public void TestRelativeNameWithSuffixAndPrefixAndSuffix()
        {
            // The lib prefix is not added if the lookup is a relative path
            ValidateNativeLibraryWithRelativeLookupResolutions("lib{0}.dll", "{0}.dll", 0);
            ValidateNativeLibraryWithRelativeLookupResolutions("lib{0}.dylib", "{0}.dylib", 0);
            ValidateNativeLibraryWithRelativeLookupResolutions("lib{0}.so", "{0}.so", 0);
        }

        public void TestNameWithPrefixAndNoPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("{0}", "lib{0}", 0);
        }

        public void TestNameWithPrefixAndPrefixAndNoSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}", "lib{0}", OS.Windows | OS.OSX | OS.Linux);
        }

        public void TestNameWithPrefixAndNoPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("{0}.dll", "lib{0}", 0);
            ValidateNativeLibraryResolutions("{0}.dylib", "lib{0}", 0);
            ValidateNativeLibraryResolutions("{0}.so", "lib{0}", 0);
        }

        public void TestNameWithPrefixAndPrefixAndSuffix()
        {
            ValidateNativeLibraryResolutions("lib{0}.dll", "lib{0}", OS.Windows);
            ValidateNativeLibraryResolutions("lib{0}.dylib", "lib{0}", OS.OSX);
            ValidateNativeLibraryResolutions("lib{0}.so", "lib{0}", OS.Linux);
        }

        public void TestWindowsAddsSuffixEvenWithOnePresent()
        {
            ValidateNativeLibraryResolutions("{0}.ext.dll", "{0}.ext", OS.Windows);
        }

        public void TestWindowsDoesntAddSuffixWhenExectubaleIsPresent()
        {
            ValidateNativeLibraryResolutions("{0}.dll.dll", "{0}.dll", 0);
            ValidateNativeLibraryResolutions("{0}.dll.exe", "{0}.dll", 0);
            ValidateNativeLibraryResolutions("{0}.exe.dll", "{0}.exe", 0);
            ValidateNativeLibraryResolutions("{0}.exe.exe", "{0}.exe", 0);
        }

        private void TestLookupWithSuffixPrefersUnmodifiedSuffixOnUnixes()
        {
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.dylib", "lib{0}.dylib", "{0}.dylib", OS.OSX);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.so", "lib{0}.so", "{0}.so", OS.Linux);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.dylib", "{0}.dylib.dylib", "{0}.dylib", OS.OSX);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.so", "{0}.so.so", "{0}.so", OS.Linux);
        }

        private void TestLookupWithoutSuffixPrefersWithSuffixOnUnixes()
        {
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.dylib", "lib{0}.dylib", "{0}", OS.OSX);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.so", "lib{0}.so", "{0}", OS.Linux);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.dylib", "{0}", "{0}", OS.OSX);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.so", "{0}", "{0}", OS.Linux);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.dylib", "lib{0}", "{0}", OS.OSX);
            ValidateNativeLibraryResolutionsWithTwoFiles("{0}.so", "lib{0}", "{0}", OS.Linux);
        }

        public void TestFullPathLookupWithMatchingFileName()
        {
            ValidateFullPathNativeLibraryResolutions("{0}", "{0}", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("{0}.dll", "{0}.dll", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("{0}.dylib", "{0}.dylib", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("{0}.so", "{0}.so", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("lib{0}", "lib{0}", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dll", "lib{0}.dll", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dylib", "lib{0}.dylib", OS.Windows | OS.OSX | OS.Linux);
            ValidateFullPathNativeLibraryResolutions("lib{0}.so", "lib{0}.so", OS.Windows | OS.OSX | OS.Linux);
        }

        public void TestFullPathLookupWithDifferentFileName()
        {
            ValidateFullPathNativeLibraryResolutions("lib{0}", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("{0}.dll", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("{0}.dylib", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("{0}.so", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dll", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dylib", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.so", "{0}", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dll", "{0}.dll", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.dylib", "{0}.dylib", 0);
            ValidateFullPathNativeLibraryResolutions("lib{0}.so", "{0}.so", 0);
        }

        [Flags]
        private enum OS
        {
            Windows = 0x1,
            OSX = 0x2,
            Linux = 0x4
        }

        private void ValidateNativeLibraryResolutions(
            string fileNamePattern,
            string lookupNamePattern,
            OS resolvesOnOSes)
        {
            string newDirectory = Guid.NewGuid().ToString().Substring(0, 8);
            string nativeLibraryPath = CreateMockFile(Path.Combine(newDirectory, string.Format(fileNamePattern, "NativeLibrary")));
            ValidateNativeLibraryResolutions(
                Path.GetDirectoryName(nativeLibraryPath),
                nativeLibraryPath,
                string.Format(lookupNamePattern, "NativeLibrary"),
                resolvesOnOSes);
        }

        private void ValidateNativeLibraryWithRelativeLookupResolutions(
            string fileNamePattern,
            string lookupNamePattern,
            OS resolvesOnOSes)
        {
            string newDirectory = Guid.NewGuid().ToString().Substring(0, 8);
            string nativeLibraryPath = CreateMockFile(Path.Combine(newDirectory, string.Format(fileNamePattern, "NativeLibrary")));
            ValidateNativeLibraryResolutions(
                Path.GetDirectoryName(Path.GetDirectoryName(nativeLibraryPath)),
                nativeLibraryPath,
                Path.Combine(newDirectory, string.Format(lookupNamePattern, "NativeLibrary")),
                resolvesOnOSes);
        }

        private void ValidateFullPathNativeLibraryResolutions(
            string fileNamePattern,
            string lookupNamePattern,
            OS resolvesOnOSes)
        {
            string newDirectory = Guid.NewGuid().ToString().Substring(0, 8);
            string nativeLibraryPath = CreateMockFile(Path.Combine(newDirectory, string.Format(fileNamePattern, "NativeLibrary")));
            ValidateNativeLibraryResolutions(
                Path.GetDirectoryName(nativeLibraryPath),
                nativeLibraryPath,
                Path.Combine(Path.GetDirectoryName(nativeLibraryPath), string.Format(lookupNamePattern, "NativeLibrary")),
                resolvesOnOSes);
        }

        private void ValidateNativeLibraryResolutionsWithTwoFiles(
            string fileNameToResolvePattern,
            string otherFileNamePattern,
            string lookupNamePattern,
            OS resolvesOnOSes)
        {
            string newDirectory = Guid.NewGuid().ToString().Substring(0, 8);
            string nativeLibraryPath = CreateMockFile(Path.Combine(newDirectory, string.Format(fileNameToResolvePattern, "NativeLibrary")));
            CreateMockFile(Path.Combine(newDirectory, string.Format(otherFileNamePattern, "NativeLibrary")));
            ValidateNativeLibraryResolutions(
                Path.GetDirectoryName(nativeLibraryPath),
                nativeLibraryPath,
                string.Format(lookupNamePattern, "NativeLibrary"),
                resolvesOnOSes);
        }

        private void ValidateNativeLibraryResolutions(
            string nativeLibraryPaths,
            string expectedResolvedFilePath,
            string lookupName,
            OS resolvesOnOSes)
        {
            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                0,
                "",
                $"{nativeLibraryPaths}",
                ""))
            {
                AssemblyDependencyResolver resolver = new AssemblyDependencyResolver(
                    Path.Combine(TestBasePath, _componentAssemblyPath));

                string result = resolver.ResolveUnmanagedDllToPath(lookupName);
                if (OperatingSystem.IsWindows())
                {
                    if (resolvesOnOSes.HasFlag(OS.Windows))
                    {
                        Assert.Equal(expectedResolvedFilePath, result);
                    }
                    else
                    {
                        Assert.Null(result);
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (resolvesOnOSes.HasFlag(OS.OSX))
                    {
                        Assert.Equal(expectedResolvedFilePath, result);
                    }
                    else
                    {
                        Assert.Null(result);
                    }
                }
                else
                {
                    if (resolvesOnOSes.HasFlag(OS.Linux))
                    {
                        Assert.Equal(expectedResolvedFilePath, result);
                    }
                    else
                    {
                        Assert.Null(result);
                    }
                }
            }
        }

        private string CreateMockFile(string relativePath)
        {
            string fullPath = Path.Combine(_componentDirectory, relativePath);
            if (!File.Exists(fullPath))
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, "Mock file");
            }

            return fullPath;
        }
    }
}
