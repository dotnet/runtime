// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

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
