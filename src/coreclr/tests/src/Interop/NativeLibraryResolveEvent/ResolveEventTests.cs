// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.InteropServices;

using Console = Internal.Console;

public class ALC : AssemblyLoadContext
{
    protected override Assembly Load(AssemblyName assemblyName)
    {
        return Assembly.Load(assemblyName);
    }
}

public class ResolveEventTests
{
    static int HandlerTracker = 1;

    public static int Main()
    {
        // Events on the Default Load Context

        try
        {
            AssemblyLoadContext.Default.ResolvingUnmanagedDll += HandlerFail;
            NativeSum(10, 10);
        }
        catch (DllNotFoundException e)
        {
            if (HandlerTracker != 0)
            {
                Console.WriteLine("Event Handlers not called as expected");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected exception: {e.Message}");
            return 102;
        }

        try
        {
            AssemblyLoadContext.Default.ResolvingUnmanagedDll += HandlerPass;

            if(NativeSum(10, 10) != 20)
            {
                Console.WriteLine("Unexpected ReturnValue from NativeSum()");
                return 103;
            }
            if (HandlerTracker != 0)
            {
                Console.WriteLine("Event Handlers not called as expected");
                return 104;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected exception: {e.Message}");
            return 105;
        }

        // Events on a Custom Load Context

        try
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testAsmDir = Path.Combine(currentDir, "..", "TestAsm", "TestAsm");

            ALC alc = new ALC();
            alc.ResolvingUnmanagedDll += HandlerPass;

            var assembly = alc.LoadFromAssemblyPath(Path.Combine(testAsmDir, "TestAsm.dll"));
            var type = assembly.GetType("TestAsm");
            var method = type.GetMethod("Sum");
            int value = (int)method.Invoke(null, new object[] { 10, 10 });

            if(value != 20)
            {
                Console.WriteLine("Unexpected ReturnValue from TestAsm.Sum()");
                return 106;
            }
            if (HandlerTracker != 1)
            {
                Console.WriteLine("Event Handlers not called as expected");
                return 107;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected exception: {e.Message}");
            return 108;
        }

        return 100;
    }

    public static IntPtr HandlerFail(Assembly assembly, string libraryName)
    {
        HandlerTracker--;
        return IntPtr.Zero;
    }

    public static IntPtr HandlerPass(Assembly assembly, string libraryName)
    {
        HandlerTracker++;
        return NativeLibrary.Load("ResolvedLib", assembly, null);
    }

    [DllImport("NativeLib")]
    static extern int NativeSum(int arg1, int arg2);

}
