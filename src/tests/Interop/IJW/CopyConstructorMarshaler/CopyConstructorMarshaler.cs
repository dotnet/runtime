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

                // PInvoke will copy twice. Once from argument to parameter, and once from the managed to native parameter.
                Assert.Equal(2, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopies");

                // Reverse PInvoke will copy 3 times. Two are from the same paths as the PInvoke,
                // and the third is from the reverse P/Invoke call.
                Assert.Equal(3, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("PInvokeNumCopiesDerivedType");

                // PInvoke will copy twice. Once from argument to parameter, and once from the managed to native parameter.
                Assert.Equal(2, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopiesDerivedType");

                // Reverse PInvoke will copy 3 times. Two are from the same paths as the PInvoke,
                // and the third is from the reverse P/Invoke call.
                Assert.Equal(3, (int)testMethod.Invoke(testInstance, null));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, int dwFlags);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
