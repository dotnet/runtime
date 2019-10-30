// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Console = Internal.Console;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
public class CallbackTests
{
    public static int Main()
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            DllImportResolver resolver =
                (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
                {
                    if (dllImportSearchPath != DllImportSearchPath.System32)
                    {
                        Console.WriteLine($"Unexpected dllImportSearchPath: {dllImportSearchPath.ToString()}");
                        throw new ArgumentException();
                    }

                    return NativeLibrary.Load("ResolveLib", asm, null);
                };

            DllImportResolver anotherResolver =
                (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
                    IntPtr.Zero;

            try
            {
                NativeSum(10, 10);
                Console.WriteLine("Exception expected: no callback registered yet");
                return 101;
            }
            catch (DllNotFoundException) {}

            try
            {
                NativeLibrary.SetDllImportResolver(null, resolver);

                Console.WriteLine("Exception expected: assembly parameter null");
                return 102;
            }
            catch (ArgumentNullException) { }

            try
            {
                NativeLibrary.SetDllImportResolver(assembly, null);

                Console.WriteLine("Exception expected: resolver parameter null");
                return 103;
            }
            catch (ArgumentNullException) { }

            // Set a resolver callback
            NativeLibrary.SetDllImportResolver(assembly, resolver);

            try
            {
                // Try to set another resolver on the same assembly.
                NativeLibrary.SetDllImportResolver(assembly, anotherResolver);

                Console.WriteLine("Exception expected: Trying to register second resolver");
                return 104;
            }
            catch (InvalidOperationException) { }

            if (NativeSum(10, 10) != 20)
            {
                Console.WriteLine("Unexpected ReturnValue from NativeSum()");
                return 105;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected exception: {e.ToString()} {e.Message}");
            return 106;
        }

        return 100;
    }

    [DllImport("NativeLib")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int NativeSum(int arg1, int arg2);
}
