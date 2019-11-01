// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

namespace CopyConstructorMarshaler
{
    class CopyConstructorMarshaler
    {
        static int Main(string[] args)
        {
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || TestLibrary.Utilities.IsWindows7)
            {
                return 100;
            }

            try
            {
                Assembly ijwNativeDll = IjwHelper.LoadIjwAssembly("IjwCopyConstructorMarshaler");
                Type testType = ijwNativeDll.GetType("TestClass");
                object testInstance = Activator.CreateInstance(testType);
                MethodInfo testMethod = testType.GetMethod("PInvokeNumCopies");

                // PInvoke will copy twice. Once from argument to parameter, and once from the managed to native parameter.
                Assert.AreEqual(2, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopies");

                // Reverse PInvoke will copy 3 times. Two are from the same paths as the PInvoke,
                // and the third is from the reverse P/Invoke call.
                Assert.AreEqual(3, (int)testMethod.Invoke(testInstance, null));
                
                testMethod = testType.GetMethod("PInvokeNumCopiesDerivedType");

                // PInvoke will copy twice. Once from argument to parameter, and once from the managed to native parameter.
                Assert.AreEqual(2, (int)testMethod.Invoke(testInstance, null));

                testMethod = testType.GetMethod("ReversePInvokeNumCopiesDerivedType");

                // Reverse PInvoke will copy 3 times. Two are from the same paths as the PInvoke,
                // and the third is from the reverse P/Invoke call.
                Assert.AreEqual(3, (int)testMethod.Invoke(testInstance, null));
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
