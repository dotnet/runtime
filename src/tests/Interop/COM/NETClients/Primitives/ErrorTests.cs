// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Runtime.InteropServices;
    using TestLibrary;

    class ErrorTests
    {
        private readonly Server.Contract.Servers.ErrorMarshalTesting server;
        public ErrorTests()
        {
            this.server = (Server.Contract.Servers.ErrorMarshalTesting)new Server.Contract.Servers.ErrorMarshalTestingClass();
        }

        public void Run()
        {
            this.VerifyExpectedException();
            this.VerifyReturnHResult();
        }

        private void VerifyExpectedException()
        {
            Console.WriteLine($"Verify expected exception from HRESULT");

            Assert.Throws<NotImplementedException>(() => { this.server.Throw_HResult(unchecked((int)0x80004001)); });
            Assert.Throws<NullReferenceException>(() => { this.server.Throw_HResult(unchecked((int)0x80004003)); });
            Assert.Throws<UnauthorizedAccessException>(() => { this.server.Throw_HResult(unchecked((int)0x80070005)); });
            Assert.Throws<OutOfMemoryException>(() => { this.server.Throw_HResult(unchecked((int)0x8007000E)); });
            Assert.Throws<ArgumentException>(() => { this.server.Throw_HResult(unchecked((int)0x80070057)); });
            Assert.Throws<COMException>(() => { this.server.Throw_HResult(unchecked((int)0x8000ffff)); });
            Assert.Throws<COMException>(() => { this.server.Throw_HResult(unchecked((int)-1)); });
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
                Assert.AreEqual(hr, this.server.Return_As_HResult(hr));
                Assert.AreEqual(hr, this.server.Return_As_HResult_Struct(hr).hr);
            }
        }
    }
}
