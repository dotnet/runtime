// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Runtime.InteropServices;
    using TestLibrary;

    internal class NETServerTest
    {
        public void Run()
        {
            Console.WriteLine($"Running {nameof(NETServerTest)}");

            // Initialize CoreShim and hostpolicymock
            HostPolicyMock.Initialize(Environment.CurrentDirectory, null);
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_ASSEMBLYNAME", "NETServer");
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_TYPENAME", "ConsumeNETServerTesting");

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty))
            {
                Type t = Type.GetTypeFromCLSID(Guid.Parse(Server.Contract.Guids.ConsumeNETServerTesting));
                dynamic obj = Activator.CreateInstance(t);

                try
                {
                    Assert.IsTrue(obj.EqualByCCW(obj));
                    Assert.IsTrue(obj.NotEqualByRCW(obj));
                }
                finally
                {
                    obj.ReleaseResources();
                }
            }
        }
    }
}
