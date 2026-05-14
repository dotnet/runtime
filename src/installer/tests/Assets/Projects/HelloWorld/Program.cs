// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace HelloWorld
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));
            Console.WriteLine(RuntimeInformation.FrameworkDescription);

            if (args.Length == 0)
                return;

            switch (args[0])
            {
                case "load_native_library_pinvoke":
                    LoadNativeLibrary.PInvoke(null);
                    LoadNativeLibrary.PInvoke(DllImportSearchPath.AssemblyDirectory);
                    LoadNativeLibrary.PInvoke(DllImportSearchPath.System32);
                    break;
                case "load_native_library_api":
                    LoadNativeLibrary.UseAPI(null);
                    LoadNativeLibrary.UseAPI(DllImportSearchPath.AssemblyDirectory);
                    LoadNativeLibrary.UseAPI(DllImportSearchPath.System32);
                    break;
                case "load_shared_library":
                    var asm = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("SharedLibrary"));
                    PropertyInfo property = asm.GetType("SharedLibrary.SharedType").GetProperty("Value");
                    Console.WriteLine($"SharedLibrary.SharedType.Value = {property.GetValue(null)}");
                    break;
                case "print_properties":
                    foreach (string propertyName in args[1..])
                    {
                        var propertyValue = (string)System.AppContext.GetData(propertyName);
                        if (string.IsNullOrEmpty(propertyValue))
                        {
                            Console.WriteLine($"Property '{propertyName}' was not found.");
                            continue;
                        }

                        Console.WriteLine($"AppContext.GetData({propertyName}) = {propertyValue}");
                    }
                    break;
                case "throw_exception":
                    // Disable core dumps - test is intentionally crashing
                    Utilities.CoreDump.Disable();
                    throw new Exception("Goodbye World!");
                case "launch_self":
                    // Launch copies of this app in parallel.
                    // When run via dotnet <app_dll>, GetCommandLineArgs[0] is the managed
                    // dll - forward it so the child knows what to launch.
                    string fileName = Environment.ProcessPath!;
                    string entry = Environment.GetCommandLineArgs()[0];
                    bool forwardEntry = entry != Environment.ProcessPath;

                    var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
                    {
                        var startInfo = new ProcessStartInfo { FileName = fileName };
                        if (forwardEntry)
                            startInfo.ArgumentList.Add(entry);
                        using var process = Process.Start(startInfo)!;
                        process.WaitForExit();
                    })).ToArray();
                    Task.WaitAll(tasks);
                    break;
                default:
                    break;
            }
        }
    }
}
