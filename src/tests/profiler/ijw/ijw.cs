// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Profiler.Tests
{
    class Ijw
    {
        static readonly Guid IjwProfilerGuid = new Guid("D6973314-9E66-4EAD-8129-9B1D3AD7CB85");

        // Managed -> native -> managed-by-pointer scenario. Exercises a reverse
        // (unmanaged->managed) marshaling stub, which must not emit a spurious
        // profiler code transition callback (it used to report a bogus FunctionID).
        // Regression test for https://github.com/dotnet/runtime/issues/120151.
        private static int CallManagedFunctionByPointer()
        {
            Assembly ijwDll = Assembly.Load("IjwProfileeDll");
            Type testType = ijwDll.GetType("TestClass")
                ?? throw new InvalidOperationException("Could not find type 'TestClass' in IjwProfileeDll.");
            object testInstance = Activator.CreateInstance(testType);
            MethodInfo method = testType.GetMethod("CallManagedFunctionByPointer")
                ?? throw new InvalidOperationException("Could not find method 'CallManagedFunctionByPointer' on TestClass.");
            return (int)method.Invoke(testInstance, null);
        }

        public static int Main(string[] args)
        {
            if (args.Length > 1 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                switch (args[1])
                {
                    case nameof(CallManagedFunctionByPointer):
                        return CallManagedFunctionByPointer();
                }
            }

            if (!RunProfilerTest(nameof(CallManagedFunctionByPointer)))
            {
                return 101;
            }

            return 100;
        }

        private static bool RunProfilerTest(string testName)
        {
            try
            {
                return ProfilerTestRunner.Run(profileePath: Assembly.GetExecutingAssembly().Location,
                                              testName: "Ijw",
                                              profilerClsid: IjwProfilerGuid,
                                              profileeArguments: testName) == 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }
    }
}
