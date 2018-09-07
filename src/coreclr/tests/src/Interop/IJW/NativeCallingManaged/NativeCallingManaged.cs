// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

namespace NativeCallingManaged
{
    class NativeCallingManaged
    {
        static int Main(string[] args)
        {
            bool success = true;
            // Load a fake mscoree.dll to avoid starting desktop
            LoadLibraryEx(Path.Combine(Environment.CurrentDirectory, "mscoree.dll"), IntPtr.Zero, 0);

            TestFramework.BeginScenario("Calling from managed to native IJW code");

            // Building with a reference to the IJW dll is difficult, so load via reflection instead
            TestFramework.BeginTestCase("Load IJW dll via reflection");
            Assembly ijwNativeDll = Assembly.Load("IjwNativeCallingManagedDll");
            TestFramework.EndTestCase();

            TestFramework.BeginTestCase("Call native method returning int");
            Type testType = ijwNativeDll.GetType("TestClass");
            object testInstance = Activator.CreateInstance(testType);
            MethodInfo testMethod = testType.GetMethod("ManagedEntryPoint");
            int result = (int)testMethod.Invoke(testInstance, null);
            if(result != 100)
            {
                TestFramework.LogError("IJW", "Incorrect result returned: " + result);
                success = false;
            }
            TestFramework.EndTestCase();

            TestFramework.BeginTestCase("Ensure .NET Framework was not loaded");
            IntPtr clrHandle = GetModuleHandle("mscoreei.dll");
            if (clrHandle != IntPtr.Zero)
            {
                TestFramework.LogError("IJW", ".NET Framework loaded by IJw module load");
                success = false;
            }
            TestFramework.EndTestCase();

            return success ? 100 : 99;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, int dwFlags);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
