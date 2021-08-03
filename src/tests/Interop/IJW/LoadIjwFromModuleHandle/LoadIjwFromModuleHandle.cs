// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Internal.Runtime.InteropServices;
using TestLibrary;

using Console = Internal.Console;

namespace LoadIjwFromModuleHandle
{
    class LoadIjwFromModuleHandle
    {
        unsafe static int Main(string[] args)
        {
            // Disable running on Windows 7 until IJW activation work is complete.
            if(Environment.OSVersion.Platform != PlatformID.Win32NT || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1))
            {
                return 100;
            }

            try
            {
                HostPolicyMock.Initialize(Environment.CurrentDirectory, null);

                Console.WriteLine("Verify that we can load an IJW assembly from native code.");
                string ijwModulePath = Path.Combine(Environment.CurrentDirectory, "IjwNativeCallingManagedDll.dll");
                IntPtr ijwNativeHandle = NativeLibrary.Load(ijwModulePath);

                using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                    0,
                    ijwModulePath,
                    string.Empty,
                    string.Empty))
                fixed (char* path = ijwModulePath)
                {
                    InMemoryAssemblyLoader.LoadInMemoryAssembly(ijwNativeHandle, (IntPtr)path);
                }

                NativeEntryPointDelegate nativeEntryPoint = Marshal.GetDelegateForFunctionPointer<NativeEntryPointDelegate>(NativeLibrary.GetExport(ijwNativeHandle, "NativeEntryPoint"));

                Assert.AreEqual(100, nativeEntryPoint());

                Console.WriteLine("Test calls from managed to native to managed when an IJW assembly was first loaded via native.");

                Assembly ijwAssemblyManaged = Assembly.Load("IjwNativeCallingManagedDll");
                Type testType = ijwAssemblyManaged.GetType("TestClass");
                object testInstance = Activator.CreateInstance(testType);
                MethodInfo testMethod = testType.GetMethod("ManagedEntryPoint");

                Assert.AreEqual(100, (int)testMethod.Invoke(testInstance, null));

                MethodInfo changeReturnedValueMethod = testType.GetMethod("ChangeReturnedValue");
                MethodInfo getReturnValueMethod = testType.GetMethod("GetReturnValue");

                int newValue = 42;
                changeReturnedValueMethod.Invoke(null, new object[] { newValue });

                Assert.AreEqual(newValue, (int)getReturnValueMethod.Invoke(null, null));

                // Native images are only loaded into memory once. As a result, the stubs in the vtfixup table
                // will always point to JIT stubs that exist in the first ALC that the module was loaded into.
                // As a result, if an IJW module is loaded into two different ALCs, or if the module is
                // first loaded via a native call and then loaded via the managed loader, the call stack can change ALCs when
                // jumping from managed->native->managed code within the IJW module.
                Assert.AreEqual(100, (int)testMethod.Invoke(testInstance, null));
                return 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return 101;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int NativeEntryPointDelegate();

    }
}
