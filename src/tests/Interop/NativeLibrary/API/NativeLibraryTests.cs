// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

enum TestResult {
    Success,
    ReturnFailure,
    ReturnNull,
    IncorrectEvaluation,
    ArgumentNull,
    ArgumentBad,
    DllNotFound,
    BadImage,
    InvalidOperation,
    EntryPointNotFound,
    GenericException
    };

public class NativeLibraryTest
{
    static string CurrentTest;
    static bool Verbose = false;

    public static int Main()
    {
        bool success = true;

        Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
        string testBinDir = Path.GetDirectoryName(assembly.Location);
        string libFullPath = NativeLibraryToLoad.GetFullPath();
        string libName;
        IntPtr handle;

        try
        {
            // -----------------------------------------------
            //         Simple LoadLibrary() API Tests
            // -----------------------------------------------

            // Calls on correct full-path to native lib
            libName = libFullPath;
            success &= EXPECT(LoadLibrarySimple(libName));
            success &= EXPECT(TryLoadLibrarySimple(libName));

            // Calls on non-existant file
            libName = Path.Combine(testBinDir, "notfound");
            success &= EXPECT(LoadLibrarySimple(libName), TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibrarySimple(libName), TestResult.ReturnFailure);

            // Calls on an invalid file
            libName = Path.Combine(testBinDir, "NativeLibrary.cpp");
            success &= EXPECT(LoadLibrarySimple(libName),
                (TestLibrary.Utilities.IsWindows) ? TestResult.BadImage : TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibrarySimple(libName), TestResult.ReturnFailure);

            // Calls on null input
            libName = null;
            success &= EXPECT(LoadLibrarySimple(libName), TestResult.ArgumentNull);
            success &= EXPECT(TryLoadLibrarySimple(libName), TestResult.ArgumentNull);

            // -----------------------------------------------
            //         Advanced LoadLibrary() API Tests
            // -----------------------------------------------

            // Advanced LoadLibrary() API Tests
            // Calls on correct full-path to native lib
            libName = libFullPath;
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null));
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null));

            // Calls on non-existant file
            libName = Path.Combine(testBinDir, "notfound");
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null), TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null), TestResult.ReturnFailure);

            // Calls on an invalid file
            libName = Path.Combine(testBinDir, "NativeLibrary.cpp");
            // The VM can only distinguish BadImageFormatException from DllNotFoundException on Windows.
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null),
                (TestLibrary.Utilities.IsWindows) ? TestResult.BadImage : TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null), TestResult.ReturnFailure);

            // Calls on just Native Library name
            libName = NativeLibraryToLoad.Name;
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null));
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null));

            // Calls on Native Library name with correct prefix-suffix
            libName = NativeLibraryToLoad.GetFileName();
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null));
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null));

            // Calls on full path without prefix-siffix
            libName = Path.Combine(testBinDir, NativeLibraryToLoad.Name);
            // DllImport doesn't add a prefix if the name is preceeded by a path specification.
            // Windows only needs a suffix, but Linux and Mac need both prefix and suffix
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null),
                (TestLibrary.Utilities.IsWindows) ? TestResult.Success : TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null),
                (TestLibrary.Utilities.IsWindows) ? TestResult.Success : TestResult.ReturnFailure);

            // Check for loading a native binary in the system32 directory.
            // The choice of the binary is arbitrary, and may not be available on
            // all Windows platforms (ex: Nano server)
            libName = "url.dll";
            if (TestLibrary.Utilities.IsWindows &&
                File.Exists(Path.Combine(Environment.SystemDirectory, libName)))
            {
                // Calls on a valid library from System32 directory
                success &= EXPECT(LoadLibraryAdvanced(libName, assembly, DllImportSearchPath.System32));
                success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, DllImportSearchPath.System32));

                // Calls on a valid library from application directory
                success &= EXPECT(LoadLibraryAdvanced(libName, assembly, DllImportSearchPath.ApplicationDirectory), TestResult.DllNotFound);
                success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, DllImportSearchPath.ApplicationDirectory), TestResult.ReturnFailure);
            }

            // Calls with null libName input
            success &= EXPECT(LoadLibraryAdvanced(null, assembly, null), TestResult.ArgumentNull);
            success &= EXPECT(TryLoadLibraryAdvanced(null, assembly, null), TestResult.ArgumentNull);

            // Calls with null assembly
            libName = NativeLibraryToLoad.Name;
            success &= EXPECT(LoadLibraryAdvanced(libName, null, null), TestResult.ArgumentNull);
            success &= EXPECT(TryLoadLibraryAdvanced(libName, null, null), TestResult.ArgumentNull);

            // Ensure that a lib is not picked up from current directory when
            // a different full-path is specified.
            libName = Path.Combine(testBinDir, Path.Combine("lib", NativeLibraryToLoad.Name));
            success &= EXPECT(LoadLibraryAdvanced(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.DllNotFound);
            success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.ReturnFailure);

            // -----------------------------------------------
            //         FreeLibrary Tests
            // -----------------------------------------------

            libName = libFullPath;
            handle = NativeLibrary.Load(libName);

            // Valid Free
            success &= EXPECT(FreeLibrary(handle));

            // Double Free
            if (TestLibrary.Utilities.IsWindows)
            {
                // The FreeLibrary() implementation simply calls the appropriate OS API
                // with the supplied handle. Not all OSes consistently return an error
                // when a library is double-freed.
                success &= EXPECT(FreeLibrary(handle), TestResult.InvalidOperation);
            }

            // Null Free
            success &= EXPECT(FreeLibrary(IntPtr.Zero));

            // -----------------------------------------------
            //         GetLibraryExport Tests
            // -----------------------------------------------
            libName = libFullPath;
            handle = NativeLibrary.Load(libName);

            // Valid Call (with some hard-coded name mangling)
            success &= EXPECT(GetLibraryExport(handle, TestLibrary.Utilities.IsX86 ? "_NativeSum@8" : "NativeSum"));
            success &= EXPECT(TryGetLibraryExport(handle, TestLibrary.Utilities.IsX86 ? "_NativeSum@8" : "NativeSum"));

            // Call with null handle
            success &= EXPECT(GetLibraryExport(IntPtr.Zero, "NativeSum"), TestResult.ArgumentNull);
            success &= EXPECT(TryGetLibraryExport(IntPtr.Zero, "NativeSum"), TestResult.ArgumentNull);

            // Call with null string
            success &= EXPECT(GetLibraryExport(handle, null), TestResult.ArgumentNull);
            success &= EXPECT(TryGetLibraryExport(handle, null), TestResult.ArgumentNull);

            // Call with wrong string
            success &= EXPECT(GetLibraryExport(handle, "NonNativeSum"), TestResult.EntryPointNotFound);
            success &= EXPECT(TryGetLibraryExport(handle, "NonNativeSum"), TestResult.ReturnFailure);

            NativeLibrary.Free(handle);
        }
        catch (Exception e)
        {
            // Catch any exceptions in NativeLibrary calls directly within this function.
            // These calls are used to setup the environment for tests that follow, and are not expected to fail.
            // If they do fail (ex: incorrect build environment) fail with an error code, rather than segmentation fault.
            Console.WriteLine(String.Format("Unhandled exception {0}", e));
            success = false;
        }

        return (success) ? 100 : -100;
    }

    static bool EXPECT(TestResult actualValue, TestResult expectedValue = TestResult.Success)
    {
        if (actualValue == expectedValue)
        {
            if (Verbose)
                Console.WriteLine(String.Format("{0} : {1} : [OK]", CurrentTest, actualValue));
            return true;
        }
        else
        {
            Console.WriteLine(String.Format(" {0} : {1} : [FAIL]", CurrentTest, actualValue));
            return false;
        }
    }

    static TestResult Run (Func<TestResult> test)
    {
        TestResult result;

        try
        {
            result = test();
        }
        catch (ArgumentNullException)
        {
            return TestResult.ArgumentNull;
        }
        catch (ArgumentException)
        {
            return TestResult.ArgumentBad;
        }
        catch (DllNotFoundException)
        {
            return TestResult.DllNotFound;
        }
        catch (BadImageFormatException)
        {
            return TestResult.BadImage;
        }
        catch (InvalidOperationException)
        {
            return TestResult.InvalidOperation;
        }
        catch (EntryPointNotFoundException)
        {
            return TestResult.EntryPointNotFound;
        }
        catch (Exception)
        {
            return TestResult.GenericException;
        }

        return result;
    }

    static TestResult LoadLibrarySimple(string libPath)
    {
        CurrentTest = String.Format("LoadLibrary({0})", libPath);

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

    static TestResult TryLoadLibrarySimple(string libPath)
    {
        CurrentTest = String.Format("TryLoadLibrary({0})", libPath);

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


    static TestResult LoadLibraryAdvanced(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        CurrentTest = String.Format("LoadLibrary({0}, {1}, {2})", libName, assembly, searchPath);

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

    static TestResult TryLoadLibraryAdvanced(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        CurrentTest = String.Format("TryLoadLibrary({0}, {1}, {2})", libName, assembly, searchPath);

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
        CurrentTest = String.Format("FreeLibrary({0})", handle);

        return Run(() => {
            NativeLibrary.Free(handle);
            return TestResult.Success;
        });
    }

    static TestResult GetLibraryExport(IntPtr handle, string name)
    {
        CurrentTest = String.Format("GetLibraryExport({0}, {1})", handle, name);

        return Run(() => {
            IntPtr address = NativeLibrary.GetExport(handle, name);
            if (address == IntPtr.Zero)
                return TestResult.ReturnNull;
            if (RunExportedFunction(address, 1, 1) != 2)
                return TestResult.IncorrectEvaluation;
            return TestResult.Success;
        });
    }

    static TestResult TryGetLibraryExport(IntPtr handle, string name)
    {
        CurrentTest = String.Format("TryGetLibraryExport({0}, {1})", handle, name);

        return Run(() => {
            IntPtr address = IntPtr.Zero;
            bool success = NativeLibrary.TryGetExport(handle, name, out address);
            if (!success)
                return TestResult.ReturnFailure;
            if (address == IntPtr.Zero)
                return TestResult.ReturnNull;
            if (RunExportedFunction(address, 1, 1) != 2)
                return TestResult.IncorrectEvaluation;
            return TestResult.Success;
        });
    }

    [DllImport(NativeLibraryToLoad.Name)]
    static extern int RunExportedFunction(IntPtr address, int arg1, int arg2);
}
