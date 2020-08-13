// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

namespace ManagedCallingNative
{
    class ManagedCallingNative
    {
        static int Main(string[] args)
        {
            // Disable running on Windows 7 until IJW activation work is complete.
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || TestLibrary.Utilities.IsWindows7)
            {
                return 100;
            }

            bool success = true;
            Assembly ijwNativeDll = IjwHelper.LoadIjwAssembly("IjwNativeDll");

            TestFramework.BeginTestCase("Call native method returning int");
            Type testType = ijwNativeDll.GetType("TestClass");
            object testInstance = Activator.CreateInstance(testType);
            MethodInfo testMethod = testType.GetMethod("ManagedEntryPoint");
            int result = (int)testMethod.Invoke(testInstance, null);
            if (result != 100)
            {
                TestFramework.LogError("IJW", "Incorrect result returned: " + result);
                success = false;
            }
            TestFramework.EndTestCase();

            TestFramework.BeginTestCase("Negative: Load IJW dll as byte array");
            byte[] ijwBytes = File.ReadAllBytes("IjwNativeDll.dll");
            try
            {
                Assembly.Load(ijwBytes);
                TestFramework.LogError("IJW", "Loading IJW dll as byte array should have thrown");
                success = false;
            }
            catch { }
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
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
