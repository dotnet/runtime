// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

namespace NativeVarargsTest
{
    class NativeVarargsTest
    {
        static int Main(string[] args)
        {
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || TestLibrary.Utilities.IsWindows7 || TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            // Use the same seed for consistency between runs.
            int seed = 42;

            try
            {
                Assembly ijwNativeDll = IjwHelper.LoadIjwAssembly("IjwNativeVarargs");
                Type testType = ijwNativeDll.GetType("TestClass");
                object testInstance = Activator.CreateInstance(testType);
                MethodInfo testMethod = testType.GetMethod("RunTests");
                IEnumerable failedTests = (IEnumerable)testMethod.Invoke(testInstance, BindingFlags.DoNotWrapExceptions, null, new object[] {seed}, null);

                if (failedTests.OfType<object>().Any())
                {
                    Console.WriteLine("Failed Varargs tests:");
                    foreach (var failedTest in failedTests)
                    {
                        Console.WriteLine($"\t{failedTest}");
                    }
                    return 102;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }
    }
}
