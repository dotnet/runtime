// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Devices.Tests
{
    public class ComputerTests
    {
        [Fact]
        public void Properties()
        {
            var computer = new Computer();

            var audio = computer.Audio;
            Assert.NotNull(audio);
            Assert.Same(audio, computer.Audio);

            var clipboard = computer.Clipboard;
            Assert.NotNull(clipboard);
            Assert.Same(clipboard, computer.Clipboard);

            var keyboard = computer.Keyboard;
            Assert.NotNull(keyboard);
            Assert.Same(keyboard, computer.Keyboard);

            var mouse = computer.Mouse;
            Assert.NotNull(mouse);
            Assert.Same(mouse, computer.Mouse);

            var ports = computer.Ports;
            Assert.NotNull(ports);
            Assert.Same(ports, computer.Ports);
        }
    }
}
