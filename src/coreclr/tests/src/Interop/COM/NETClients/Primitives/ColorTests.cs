// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using TestLibrary;

    class ColorTests
    {
        private readonly Server.Contract.Servers.ColorTesting server;
        public ColorTests()
        {
            this.server = (Server.Contract.Servers.ColorTesting)new Server.Contract.Servers.ColorTestingClass();
        }

        public void Run()
        {
            this.VerifyColorMarshalling();
            this.VerifyGetRed();
        }

        private void VerifyColorMarshalling()
        {
            Assert.IsTrue(server.AreColorsEqual(Color.Green, ColorTranslator.ToOle(Color.Green)));
        }

        private void VerifyGetRed()
        {
            Assert.AreEqual(Color.Red, server.GetRed());
        }
    }
}
