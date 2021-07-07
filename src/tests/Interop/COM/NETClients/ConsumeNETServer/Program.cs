// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Server.Contract;

    using CoClass = Server.Contract.Servers;

    class Program
    {
        static void Validate_Activation()
        {
            Console.WriteLine($"{nameof(Validate_Activation)}...");

            var test = new CoClass.ConsumeNETServerTesting();
            test.ReleaseResources();

            // The CoClass should be the activated type, _not_ the activation interface.
            Assert.AreEqual(test.GetType(), typeof(CoClass.ConsumeNETServerTestingClass));
            Assert.IsTrue(typeof(CoClass.ConsumeNETServerTestingClass).IsCOMObject);
            Assert.IsFalse(typeof(CoClass.ConsumeNETServerTesting).IsCOMObject);
            Assert.IsTrue(Marshal.IsComObject(test));
        }

        static void Validate_CCW_Wasnt_Unwrapped()
        {
            Console.WriteLine($"{nameof(Validate_CCW_Wasnt_Unwrapped)}...");

            var test = new CoClass.ConsumeNETServerTesting();
            test.ReleaseResources();

            // The CoClass should be the activated type, _not_ the implementation class.
            // This indicates the real implementation class is wrapped in its CCW and exposed
            // to the runtime as an RCW.
            Assert.AreNotEqual(test.GetType(), typeof(ConsumeNETServerTesting));
        }

        static void Validate_Client_CCW_RCW()
        {
            Console.WriteLine($"{nameof(Validate_Client_CCW_RCW)}...");

            IntPtr ccw = IntPtr.Zero;

            // Validate the client side view is consistent
            var test = new CoClass.ConsumeNETServerTesting();
            try
            {
                ccw = test.GetCCW();
                object rcw = Marshal.GetObjectForIUnknown(ccw);
                object inst = test.GetRCW();
                Assert.AreEqual(rcw, inst);
            }
            finally
            {
                test.ReleaseResources();
            }
        }

        static void Validate_Server_CCW_RCW()
        {
            Console.WriteLine($"{nameof(Validate_Server_CCW_RCW)}...");

            // Validate the server side view is consistent
            var test = new CoClass.ConsumeNETServerTesting();
            try
            {
                Assert.IsTrue(test.EqualByCCW(test));
                Assert.IsTrue(test.NotEqualByRCW(test));
            }
            finally
            {
                test.ReleaseResources();
            }
        }

        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            // Initialize CoreShim and hostpolicymock
            HostPolicyMock.Initialize(Environment.CurrentDirectory, null);
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_ASSEMBLYNAME", "NETServer");
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_TYPENAME", "ConsumeNETServerTesting");

            try
            {
                using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty))
                {
                    Validate_Activation();
                    Validate_CCW_Wasnt_Unwrapped();
                    Validate_Client_CCW_RCW();
                    Validate_Server_CCW_RCW();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
