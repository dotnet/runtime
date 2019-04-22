// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Devices.Tests
{
    public class ServerComputerTests
    {
        [Fact]
        public void Properties()
        {
            var computer = new ServerComputer();

            Assert.Equal(System.Environment.MachineName, computer.Name);

            var clock = computer.Clock;
            Assert.NotNull(clock);
            Assert.Same(clock, computer.Clock);

            var fileSystem = computer.FileSystem;
            Assert.NotNull(fileSystem);
            Assert.Same(fileSystem, computer.FileSystem);

            var info = computer.Info;
            Assert.NotNull(info);
            Assert.Same(info, computer.Info);

            var network = computer.Network;
            Assert.NotNull(network);
            Assert.Same(network, computer.Network);

            var registry = computer.Registry;
            Assert.NotNull(registry);
            Assert.Same(registry, computer.Registry);
        }
    }
}
