// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using static TestHelpers;

public class NativeLibraryTests : IDisposable
{
    private readonly Assembly assembly;
    private readonly string testBinDir;
    private readonly string libFullPath;

    public NativeLibraryTests()
    {
        assembly = System.Reflection.Assembly.GetExecutingAssembly();
        testBinDir = NativeLibraryToLoad.GetDirectory();
        libFullPath = NativeLibraryToLoad.GetFullPath();
    }

    [Fact]
    public void LoadLibraryFullPath_NameOnly()
    {
        string libName = libFullPath;
        EXPECT(LoadLibrary_NameOnly(libName));
        EXPECT(TryLoadLibrary_NameOnly(libName));
    }

    [Fact]
    public void LoadLibraryOnNonExistentFile_NameOnly()
    {
        string libName = Path.Combine(testBinDir, "notfound");
        EXPECT(LoadLibrary_NameOnly(libName), TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_NameOnly(libName), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibraryOnInvalidFile_NameOnly()
    {
        string libName = Path.Combine(testBinDir, "NativeLibrary.cpp");
        EXPECT(LoadLibrary_NameOnly(libName),
                OperatingSystem.IsWindows() ? TestResult.BadImage : TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_NameOnly(libName), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibraryRelativePaths_NameOnly()
    {
        {
            string libName = Path.Combine("..", NativeLibraryToLoad.InvalidName, NativeLibraryToLoad.GetLibraryFileName(NativeLibraryToLoad.InvalidName));
            EXPECT(LoadLibrary_NameOnly(libName), TestResult.DllNotFound);
            EXPECT(TryLoadLibrary_NameOnly(libName), TestResult.ReturnFailure);
        }

        {
            string libName = Path.Combine("..", nameof(NativeLibraryTests), NativeLibraryToLoad.GetLibraryFileName(NativeLibraryToLoad.Name));
            EXPECT(LoadLibrary_NameOnly(libName), TestResult.Success);
            EXPECT(TryLoadLibrary_NameOnly(libName), TestResult.Success);
        }
    }

    [Fact]
    public void LoadLibraryFullPath_WithAssembly()
    {
        string libName = libFullPath;
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null));
    }

    [Fact]
    public void LoadLibraryOnNonExistentFile_WithAssembly()
    {
        string libName = Path.Combine(testBinDir, "notfound");
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null), TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibraryOnInvalidFile_WithAssembly()
    {
        string libName = Path.Combine(testBinDir, "NativeLibrary.cpp");
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null),
                OperatingSystem.IsWindows() ? TestResult.BadImage : TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibraryNameOnly_WithAssembly()
    {
        string libName = NativeLibraryToLoad.Name;
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null));
    }

    [Fact]
    public void LoadLibraryFileName_WithAssembly()
    {
        string libName = NativeLibraryToLoad.GetFileName();
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void LoadLibraryFullPathWithoutNativePrefixOrSuffix_WithAssembly_Success()
    {
        // DllImport doesn't add a prefix if the name is preceded by a path specification.
        // Windows only needs a suffix, so adding only the suffix is successful
        string libName = Path.Combine(testBinDir, NativeLibraryToLoad.Name);
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null));
    }

    [Fact]
    [PlatformSpecific(~TestPlatforms.Windows)]
    public void LoadLibraryFullPathWithoutNativePrefixOrSuffix_WithAssembly_Failure()
    {
        // DllImport doesn't add a prefix if the name is preceded by a path specification.
        // Linux and Mac need both prefix and suffix
        string libName = Path.Combine(testBinDir, NativeLibraryToLoad.Name);
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, null), TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, null), TestResult.ReturnFailure);
    }

    public static bool HasKnownLibraryInSystemDirectory =>
        OperatingSystem.IsWindows()
        && File.Exists(Path.Combine(Environment.SystemDirectory, "url.dll"));

    [ConditionalFact(nameof(HasKnownLibraryInSystemDirectory))]
    public void LoadSystemLibrary_WithSearchPath()
    {
        string libName = "url.dll";
        // Library should be found in the system directory
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.System32));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.System32));

        // Library should not be found in the assembly directory and should be found in the system directory
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32));

        // Library should not be found in the assembly directory, but should fall back to the default OS search which includes CWD on Windows
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory));
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory));

        // Library should not be found in application directory
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.ApplicationDirectory), TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.ApplicationDirectory), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibrary_NullLibName()
    {
        EXPECT(LoadLibrary_WithAssembly(null, assembly, null), TestResult.ArgumentNull);
        EXPECT(TryLoadLibrary_WithAssembly(null, assembly, null), TestResult.ArgumentNull);
    }

    [Fact]
    public void LoadLibrary_NullAssembly()
    {
        string libName = NativeLibraryToLoad.Name;
        EXPECT(LoadLibrary_WithAssembly(libName, null, null), TestResult.ArgumentNull);
        EXPECT(TryLoadLibrary_WithAssembly(libName, null, null), TestResult.ArgumentNull);
    }

    [Fact]
    public void LoadLibrary_UsesFullPath_EvenWhen_AssemblyDirectory_Specified()
    {
        string libName = Path.Combine(testBinDir, Path.Combine("lib", NativeLibraryToLoad.Name));
        EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.DllNotFound);
        EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.ReturnFailure);
    }

    [Fact]
    public void LoadLibrary_AssemblyDirectory()
    {
        string suffix = "-in-subdirectory";
        string libName = $"{NativeLibraryToLoad.Name}{suffix}";

        string subdirectory = Path.Combine(testBinDir, "subdirectory");

        if (!TestLibrary.Utilities.IsNativeAot && !TestLibrary.PlatformDetection.IsMonoLLVMFULLAOT)
        {
            // Library should be found in the assembly directory
            Assembly assemblyInSubdirectory = Assembly.LoadFile(Path.Combine(subdirectory, $"{assembly.GetName().Name}{suffix}.dll"));
            EXPECT(LoadLibrary_WithAssembly(libName, assemblyInSubdirectory, DllImportSearchPath.AssemblyDirectory));
            EXPECT(TryLoadLibrary_WithAssembly(libName, assemblyInSubdirectory, DllImportSearchPath.AssemblyDirectory));
        }

        if (OperatingSystem.IsWindows())
        {
            string currentDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = subdirectory;

                // Library should not be found in the assembly directory, but should fall back to the default OS search which includes CWD on Windows
                EXPECT(LoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory));
                EXPECT(TryLoadLibrary_WithAssembly(libName, assembly, DllImportSearchPath.AssemblyDirectory));
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }
    }

    [Fact]
    public void Free()
    {
        string libName = libFullPath;
        IntPtr handle = NativeLibrary.Load(libName);

        // Valid Free
        EXPECT(FreeLibrary(handle));

        // Double Free
        if (OperatingSystem.IsWindows())
        {
            // The FreeLibrary() implementation simply calls the appropriate OS API
            // with the supplied handle. Not all OSes consistently return an error
            // when a library is double-freed.
            EXPECT(FreeLibrary(handle), TestResult.InvalidOperation);
        }

        // Null Free
        EXPECT(FreeLibrary(IntPtr.Zero));
    }

    public void Dispose() {}

    static TestResult LoadLibrary_NameOnly(string libPath)
    {
        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            handle = NativeLibrary.Load(libPath);
            if (handle == IntPtr.Zero)
                return TestResult.ReturnNull;
            return TestResult.Success;
        });

        NativeLibrary.Free(handle);

        return result;
    }

    static TestResult TryLoadLibrary_NameOnly(string libPath)
    {
        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            bool success = NativeLibrary.TryLoad(libPath, out handle);
            if (!success)
                return TestResult.ReturnFailure;
            if (handle == IntPtr.Zero)
                return TestResult.ReturnNull;
            return TestResult.Success;
        });

        NativeLibrary.Free(handle);

        return result;
    }


    static TestResult LoadLibrary_WithAssembly(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            handle = NativeLibrary.Load(libName, assembly, searchPath);
            if (handle == IntPtr.Zero)
                return TestResult.ReturnNull;
            return TestResult.Success;
        });

        NativeLibrary.Free(handle);

        return result;
    }

    static TestResult TryLoadLibrary_WithAssembly(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            bool success = NativeLibrary.TryLoad(libName, assembly, searchPath, out handle);
            if (!success)
                return TestResult.ReturnFailure;
            if (handle == IntPtr.Zero)
                return TestResult.ReturnNull;
            return TestResult.Success;
        });

        NativeLibrary.Free(handle);

        return result;
    }

    static TestResult FreeLibrary(IntPtr handle)
    {
        return Run(() => {
            NativeLibrary.Free(handle);
            return TestResult.Success;
        });
    }
}
