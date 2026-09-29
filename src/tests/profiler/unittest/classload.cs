// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace Profiler.Tests
{
    class ClassLoadTest
    {
        private static readonly Guid ClassLoadGuid = new Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0");

        static int Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest();
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestClassLoad",
                                          profilerClsid: ClassLoadGuid);
        }

        static int RunTest()
        {
            LoadCollectibleAssembly();

            // Force a garbage collection to ensure the assembly is unloaded
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return 100;
        }

        private static void LoadCollectibleAssembly()
        {
            var collectibleContext = new AssemblyLoadContext("Collectible", true);

            var asmDir = Path.GetDirectoryName(typeof(ClassLoadTest).Assembly.Location);
            var dynamicLibrary = collectibleContext.LoadFromAssemblyPath(Path.Combine(asmDir, "unloadlibrary.dll"));
            var testType = dynamicLibrary.GetType("UnloadLibrary.TestClass");

            object instance = Activator.CreateInstance(testType);

            Console.WriteLine(instance.GetHashCode());
            collectibleContext.Unload();
        }
    }
}
