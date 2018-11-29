// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

using Console = Internal.Console;

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
        string libName;
        IntPtr handle;

        // -----------------------------------------------
        //         Simple LoadLibrary() API Tests
        // -----------------------------------------------

        // Calls on correct full-path to native lib
        libName = Path.Combine(testBinDir, GetNativeLibraryName());
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
        libName = Path.Combine(testBinDir, GetNativeLibraryName());
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
        libName = GetNativeLibraryPlainName();
        success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null));
        success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null));

        // Calls on Native Library name with correct prefix-suffix
        libName = GetNativeLibraryName();
        success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null));
        success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null));

        // Calls on full path without prefix-siffix
        libName = Path.Combine(testBinDir, GetNativeLibraryPlainName());
        // DllImport doesn't add a prefix if the name is preceeded by a path specification.
        // Windows only needs a suffix, but Linux and Mac need both prefix and suffix
        success &= EXPECT(LoadLibraryAdvanced(libName, assembly, null),
            (TestLibrary.Utilities.IsWindows) ? TestResult.Success : TestResult.DllNotFound);
        success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, null),
            (TestLibrary.Utilities.IsWindows) ? TestResult.Success : TestResult.ReturnFailure);

        if (TestLibrary.Utilities.IsWindows)
        {
            libName = GetWin32LibName();

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
        libName = GetNativeLibraryPlainName();
        success &= EXPECT(LoadLibraryAdvanced(libName, null, null), TestResult.ArgumentNull);
        success &= EXPECT(TryLoadLibraryAdvanced(libName, null, null), TestResult.ArgumentNull);

        // Ensure that a lib is not picked up from current directory when
        // a different full-path is specified.
        libName = Path.Combine(testBinDir, Path.Combine("lib", GetNativeLibraryPlainName()));
        success &= EXPECT(LoadLibraryAdvanced(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.DllNotFound);
        success &= EXPECT(TryLoadLibraryAdvanced(libName, assembly, DllImportSearchPath.AssemblyDirectory), TestResult.ReturnFailure);

        // -----------------------------------------------
        //         FreeLibrary Tests
        // -----------------------------------------------

        libName = Path.Combine(testBinDir, GetNativeLibraryName());
        handle = Marshal.LoadLibrary(libName);

        // Valid Free
        success &= EXPECT(FreeLibrary(handle));

        // Double Free
        success &= EXPECT(FreeLibrary(handle), TestResult.InvalidOperation);

        // Null Free
        success &= EXPECT(FreeLibrary(IntPtr.Zero));

        // -----------------------------------------------
        //         GetLibraryExport Tests
        // -----------------------------------------------
        libName = Path.Combine(testBinDir, GetNativeLibraryName());
        handle = Marshal.LoadLibrary(libName);

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

        Marshal.FreeLibrary(handle);

        return (success) ? 100 : -100;
    }

    static string GetNativeLibraryPlainName()
    {
        return "NativeLibrary";
    }

    static string GetWin32LibName()
    {
        return "msi.dll";
    }

    static string GetNativeLibraryName()
    {
        string baseName = GetNativeLibraryPlainName();

        if (TestLibrary.Utilities.IsWindows)
        {
            return baseName + ".dll";
        }
        if (TestLibrary.Utilities.IsLinux)
        {
            return "lib" + baseName + ".so";
        }
        if (TestLibrary.Utilities.IsMacOSX)
        {
            return "lib" + baseName + ".dylib";
        }

        return "ERROR";
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
        catch (ArgumentNullException e)
        {
            return  TestResult.ArgumentNull;
        }
        catch (ArgumentException e)
        {
            return TestResult.ArgumentBad;
        }
        catch (DllNotFoundException e)
        {
            return TestResult.DllNotFound;
        }
        catch (BadImageFormatException e)
        {
            return TestResult.BadImage;
        }
        catch (InvalidOperationException e)
        {
            return TestResult.InvalidOperation;
        }
        catch (EntryPointNotFoundException e)
        {
            return TestResult.EntryPointNotFound;
        }
        catch (Exception e)
        {
            //Console.WriteLine(e.ToString());
            return TestResult.GenericException;
        }

        return result;
    }

    static TestResult LoadLibrarySimple(string libPath)
    {
        CurrentTest = String.Format("LoadLibrary({0})", libPath);

        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            handle = Marshal.LoadLibrary(libPath);
            if (handle == IntPtr.Zero)
                return  TestResult.ReturnNull;
            return TestResult.Success;
        });

        Marshal.FreeLibrary(handle);

        return result;
    }

    static TestResult TryLoadLibrarySimple(string libPath)
    {
        CurrentTest = String.Format("TryLoadLibrary({0})", libPath);

        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            bool success = Marshal.TryLoadLibrary(libPath, out handle);
            if(!success)
                return TestResult.ReturnFailure;
            if (handle == null)
                return  TestResult.ReturnNull;
            return TestResult.Success;
        });

        Marshal.FreeLibrary(handle);

        return result;
    }


    static TestResult LoadLibraryAdvanced(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        CurrentTest = String.Format("LoadLibrary({0}, {1}, {2})", libName, assembly, searchPath);

        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            handle = Marshal.LoadLibrary(libName, assembly, searchPath);
            if (handle == IntPtr.Zero)
                return  TestResult.ReturnNull;
            return TestResult.Success;
        });

        Marshal.FreeLibrary(handle);

        return result;
    }

    static TestResult TryLoadLibraryAdvanced(string libName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        CurrentTest = String.Format("TryLoadLibrary({0}, {1}, {2})", libName, assembly, searchPath);

        IntPtr handle = IntPtr.Zero;

        TestResult result = Run(() => {
            bool success = Marshal.TryLoadLibrary(libName, assembly, searchPath, out handle);
            if (!success)
                return  TestResult.ReturnFailure;
            if (handle == IntPtr.Zero)
                return  TestResult.ReturnNull;
            return TestResult.Success;
        });

        Marshal.FreeLibrary(handle);

        return result;
    }

    static TestResult FreeLibrary(IntPtr handle)
    {
        CurrentTest = String.Format("FreeLibrary({0})", handle);

        return Run(() => {
            Marshal.FreeLibrary(handle);
            return TestResult.Success;
        });
    }

    static TestResult GetLibraryExport(IntPtr handle, string name)
    {
        CurrentTest = String.Format("GetLibraryExport({0}, {1})", handle, name);

        return Run(() => {
            IntPtr address = Marshal.GetLibraryExport(handle, name);
            if (address == null)
                return  TestResult.ReturnNull;
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
            bool success = Marshal.TryGetLibraryExport(handle, name, out address);
            if (!success)
                return  TestResult.ReturnFailure;
            if (address == null)
                return  TestResult.ReturnNull;
            if (RunExportedFunction(address, 1, 1) != 2)
                return TestResult.IncorrectEvaluation;
            return TestResult.Success;
        });
    }
 
    [DllImport("NativeLibrary")]
    static extern int RunExportedFunction(IntPtr address, int arg1, int arg2);
}
