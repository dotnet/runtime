// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;

    using CoClass = Server.Contract.Servers;

    public class Program
    {
        static void Validate_Activation()
        {
            Console.WriteLine($"{nameof(Validate_Activation)}...");

            var test = new CoClass.ConsumeNETServerTesting();
            test.ReleaseResources();

            // The CoClass should be the activated type, _not_ the activation interface.
            Assert.Equal(typeof(CoClass.ConsumeNETServerTestingClass), test.GetType());
            Assert.True(typeof(CoClass.ConsumeNETServerTestingClass).IsCOMObject);
            Assert.False(typeof(CoClass.ConsumeNETServerTesting).IsCOMObject);
            Assert.True(Marshal.IsComObject(test));
        }

        static void Validate_Activation_CreateInstance()
        {
            Console.WriteLine($"{nameof(Validate_Activation_CreateInstance)}...");

            Type t = Type.GetTypeFromCLSID(Guid.Parse(Guids.ConsumeNETServerTesting));
            Assert.True(t.IsCOMObject);

            object obj = Activator.CreateInstance(t);
            var test = (CoClass.ConsumeNETServerTesting)obj;
            test.ReleaseResources();

            Assert.True(Marshal.IsComObject(test));

            // Use the overload that takes constructor arguments. This tests the path where the runtime searches for the
            // constructor to use (which has some special-casing for COM) instead of just always using the default.
            obj = Activator.CreateInstance(t, Array.Empty<object>());
            test = (CoClass.ConsumeNETServerTesting)obj;
            test.ReleaseResources();

            Assert.True(Marshal.IsComObject(test));
        }

        static void Validate_CCW_Wasnt_Unwrapped()
        {
            Console.WriteLine($"{nameof(Validate_CCW_Wasnt_Unwrapped)}...");

            var test = new CoClass.ConsumeNETServerTesting();
            test.ReleaseResources();

            // The CoClass should be the activated type, _not_ the implementation class.
            // This indicates the real implementation class is wrapped in its CCW and exposed
            // to the runtime as an RCW.
            Assert.NotEqual(typeof(ConsumeNETServerTesting), test.GetType());
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
                Assert.Equal(rcw, inst);
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
                Assert.True(test.EqualByCCW(test));
                Assert.True(test.NotEqualByRCW(test));
            }
            finally
            {
                test.ReleaseResources();
            }
        }

        [Fact]
        public static int TestEntryPoint()
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
                    Validate_Activation_CreateInstance();
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
