// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Profiler.Tests
{
    class ModuleLoadTest
    {
        private static readonly Guid ModuleLoadGuid = new Guid("1774B2E5-028B-4FA8-9DE5-26218CBCBBAC");

        public static int RunTest(string[] args)
        {
            var type = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestAssembly"), AssemblyBuilderAccess.Run)
                                      .DefineDynamicModule("TestModule")
                                      .DefineType("TestClass", TypeAttributes.Public)
                                      .CreateType();

            var obj = Activator.CreateInstance(type);
            if (obj == null)
            {
                throw new NullReferenceException();
            }

            // Trigger module load in multiple threads
            int threadCount = 20;
            List<Thread> threads = new(threadCount);
            for (int i = 0; i < threadCount; i++)
                threads.Add(new Thread(() => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("unloadlibrary"))));

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestModuleLoad",
                                          profilerClsid: ModuleLoadGuid);
        }
    }
}
