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
        [Fact]
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
