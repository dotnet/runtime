// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace ManagedCallingNative
{
    public class ManagedCallingNative
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // Disable running on Windows 7 until IJW activation work is complete.
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || TestLibrary.Utilities.IsWindows7)
            {
                return 100;
            }

            bool success = true;
            Assembly ijwNativeDll = Assembly.Load("IjwNativeDll");

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

            return success ? 100 : 99;
        }
    }
}
