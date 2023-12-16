// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using Xunit;

public class FakeInjectedCode
{
    int NonStatic(String argument) { return 0;}
    static bool WrongReturnType(String argument) { return false;}
    static int Return0(String argument) { return 0;}
    static int Return1(String argument) { return 1;}
    static int ThrowAnything(String argument) { throw new Exception("Throwing something");}
    static int ParseArgument(String argument) { return int.Parse(argument);}
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class Program
{
    public static class NativeMethods
    {
        [DllImport("ExecInDefAppDomDll")]
        public static extern int
        CallExecuteInDefaultAppDomain(
            [In, MarshalAs( UnmanagedType.LPWStr )] String assemblyPath,
            [In, MarshalAs( UnmanagedType.LPWStr )] String typeName,
            [In, MarshalAs( UnmanagedType.LPWStr )] String methodName,
            [In, MarshalAs( UnmanagedType.LPWStr )] String argument,
            [In, Out, MarshalAs( UnmanagedType.I4 )] ref int result
        );
    }

    static int TestExecuteInAppDomain(string assemblyPath, string typeName, string methodName, string argument, int expectedHResult, int expectedResult)
    {
        bool passed = true;
        try
        {
            int result = 0;
            int hresult = NativeMethods.CallExecuteInDefaultAppDomain(assemblyPath, typeName, methodName, argument, ref result);

            if (hresult != expectedHResult)
            {
                Console.WriteLine($"Bad HRESULT: expected {expectedHResult:X} actual {hresult:X}");
                passed = false;
            }
            else if (result != expectedResult)
            {
                Console.WriteLine($"Bad result: expected {expectedResult} actual {result}");
                passed = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected exception: {e}");
            passed = false;
        }
        return passed ? 0 : 1;
    }

    [Fact]
    [SkipOnMono("The legacy CoreCLR activation API is not supported on Mono.")]
    public static int TestEntryPoint()
    {
        int result = 100;
        String myPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        String injectedPath = System.IO.Path.GetDirectoryName(myPath) + "/InjectedCode.dll";
        String bogusPath = myPath + "random";

        const int S_OK = unchecked((int)0);
        const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);
        const int COR_E_TYPELOAD = unchecked((int)0x80131522);
        const int COR_E_MISSINGMETHOD = unchecked((int)0x80131513);
        const int COR_E_EXCEPTION = unchecked((int)0x80131500);
        const int COR_E_FORMAT = unchecked((int)0x80131537);

        result += TestExecuteInAppDomain(bogusPath, "BogusType", "Return0", "None", COR_E_FILENOTFOUND, 0);

        result += TestExecuteInAppDomain(myPath, "BogusType", "Return0", "None", COR_E_TYPELOAD, 0);

        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "NonStatic", "None", COR_E_MISSINGMETHOD, 0);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "WrongReturnType", "None", COR_E_MISSINGMETHOD, 0);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "Return0", "None", S_OK, 0);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "Return1", "None", S_OK, 1);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "ThrowAnything", "None", COR_E_EXCEPTION, 0);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "ParseArgument", "0", S_OK, 0);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "ParseArgument", "200", S_OK, 200);
        result += TestExecuteInAppDomain(myPath, "FakeInjectedCode", "ParseArgument", "None", COR_E_FORMAT, 0);
        result += TestExecuteInAppDomain(injectedPath, "InjectedCode", "ParseArgument", "300", S_OK, 300);

        return result;
    }

}
