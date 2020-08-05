// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Runtime.InteropServices;
    using TestLibrary;

    class ErrorTests
    {
        private readonly Server.Contract.IErrorMarshalTesting_VtableGap server;
        public ErrorTests()
        {
            this.server = (Server.Contract.IErrorMarshalTesting_VtableGap)new Server.Contract.Servers.ErrorMarshalTestingClass();
        }

        public void Run()
        {
            this.VerifyReturnHResult();
        }

        private void VerifyReturnHResult()
        {
            Console.WriteLine($"Verify preserved function signature");

            var hrs = new[]
            {
                    unchecked((int)0x80004001),
                    unchecked((int)0x80004003),
                    unchecked((int)0x80070005),
                    unchecked((int)0x80070057),
                    unchecked((int)0x8000ffff),
                    -1,
                    1,
                    2
                };

            foreach (var hr in hrs)
            {
                Assert.AreEqual(hr, this.server.Return_As_HResult_Struct(hr).hr);
            }
        }
    }
}
