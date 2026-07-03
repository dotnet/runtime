// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace NativeCallingManaged
{
    public class NativeCallingManaged
    {
        [ActiveIssue("C++/CLI, IJW not supported on Mono", TestRuntimes.Mono)]
        [Fact]
        public static int TestEntryPoint()
        {
            if(Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return 100;
            }

            bool success = true;
            Assembly ijwNativeDll = Assembly.Load("IjwNativeCallingManagedDll");

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

            // Regression test for https://github.com/dotnet/runtime/issues/127166:
            // Native code calling a managed function with 17+ by-ref parameters
            // hit an OverflowException because StubSigBuilder::EnsureEnoughQuickBytes
            // only doubled the buffer size once, which was insufficient when the
            // internal signature (with preserved custom modifiers) exceeded 512 bytes.
            TestFramework.BeginTestCase("Call managed method with 18 by-ref parameters from native");
            MethodInfo sum18Method = testType.GetMethod("ManagedEntryPointSum18ByRef");
            long sum = (long)sum18Method.Invoke(testInstance, null);
            const long expectedSum = 153; // 0 + (1+2+...+16) + 17
            if (sum != expectedSum)
            {
                TestFramework.LogError("IJW", "Incorrect sum returned: " + sum + " (expected " + expectedSum + ")");
                success = false;
            }
            TestFramework.EndTestCase();

            return success ? 100 : 99;
        }
    }
}
