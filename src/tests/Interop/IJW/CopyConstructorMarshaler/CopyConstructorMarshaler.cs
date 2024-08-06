// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace CopyConstructorMarshaler
{
    public class CopyConstructorMarshaler
    {
        //[Fact]
        public static int TestEntryPoint()
        {
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || TestLibrary.Utilities.IsWindows7)
            {
                return 100;
            }

            try
            {
                Assembly ijwNativeDll = Assembly.Load("IjwCopyConstructorMarshaler");
                Type testType = ijwNativeDll.GetType("TestClass");
                object testInstance = Activator.CreateInstance(testType);
                MethodInfo testMethod = testType.GetMethod("PInvokeNumCopies");

                // On x86, we will copy in the IL stub to the final arg slot.
                int platformExtra = 0;
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    platformExtra = 1;
                }
    
                // PInvoke will copy once. Once from the managed to native parameter.
                Assert.Equal(1 + platformExtra, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopies");

                // Reverse PInvoke will copy 2 times. One from the same path as the PInvoke,
                // and one from the reverse P/Invoke call.
                Assert.Equal(2 + platformExtra, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("PInvokeNumCopiesDerivedType");

                // PInvoke will copy once from the managed to native parameter.
                Assert.Equal(1 + platformExtra, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopiesDerivedType");

                // Reverse PInvoke will copy 2 times. One from the same path as the PInvoke,
                // and one from the reverse P/Invoke call.
                Assert.Equal(2 + platformExtra, (int)testMethod.Invoke(testInstance, null));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }

        //[Fact]
        public static void CopyConstructorsInArgumentStackSlots()
        {
            Assembly ijwNativeDll = Assembly.Load("IjwCopyConstructorMarshaler");
            Type testType = ijwNativeDll.GetType("TestClass");
            object testInstance = Activator.CreateInstance(testType);
            MethodInfo testMethod = testType.GetMethod("ExposedThisCopyConstructorScenario");

            Assert.Equal(0, (int)testMethod.Invoke(testInstance, null));
        }

        [Fact]
        public static void CopyConstructorsInArgumentStackSlotsWithUnsafeValueType()
        {
            Assembly ijwNativeDll = Assembly.Load("IjwCopyConstructorMarshaler");
            Type testType = ijwNativeDll.GetType("TestClass");
            object testInstance = Activator.CreateInstance(testType);
            MethodInfo testMethod = testType.GetMethod("ExposedThisUnsafeValueTypeCopyConstructorScenario");

            Assert.Equal(0, (int)testMethod.Invoke(testInstance, null));
        }
    }
}
