// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace NativeCallingManaged
{
    public class NativeCallingManaged
    {
        [ActiveIssue("C++/CLI, IJW not supported on Mono", TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static int TestEntryPoint()
        {
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

        // Regression coverage for https://github.com/dotnet/runtime/issues/90563. Module::GetISymUnmanagedReader
        // hands diasymreader a no-op IMetaDataImport2 instead of the module's real (RW-locked) importer.
        // IjwNativeCallingManagedDll carries [assembly:Debuggable(true, true)] (== 0x101), so invoking one of its
        // managed methods drives the JIT getBoundaries -> ISymUnmanagedReader path -- the second consumer of the
        // no-op importer, distinct from the Exception.StackTrace path. This test JITs such a method by invoking it,
        // then asserts the source file and line resolve from the module's classic PDB. Two guarantees fall out:
        //   * On a checked build, if a future diasymreader upgrade calls a metadata method the no-op importer does
        //     not implement, running this path fires the NOOPMD_NYI assert instead of silently passing.
        //   * A resolved, non-zero line proves the classic PDB was actually read (the jit will silently drop it otherwise)
        [ActiveIssue("C++/CLI, IJW not supported on Mono", TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void ManagedMethodResolvesSourceLineFromClassicPdb()
        {
            Assembly ijwNativeDll = Assembly.Load("IjwNativeCallingManagedDll");

            Type testType = ijwNativeDll.GetType("TestClass");
            MethodInfo throwMethod = testType.GetMethod("ThrowFromManaged");

            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => throwMethod.Invoke(null, null));
            Exception thrown = tie.InnerException;
            Assert.NotNull(thrown);

            StackFrame ijwFrame = null;
            foreach (StackFrame frame in new StackTrace(thrown, fNeedFileInfo: true).GetFrames())
            {
                if (frame.GetMethod()?.DeclaringType == testType)
                {
                    ijwFrame = frame;
                    break;
                }
            }

            Assert.NotNull(ijwFrame);
            Assert.EndsWith("IjwNativeCallingManagedDll.cpp", ijwFrame.GetFileName() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.True(ijwFrame.GetFileLineNumber() > 0, "Expected a non-zero source line resolved from the module's classic PDB.");
        }
    }
}
