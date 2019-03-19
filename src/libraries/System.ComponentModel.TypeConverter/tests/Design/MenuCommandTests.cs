// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class MenuCommandTests
    {
        public static IEnumerable<object[]> Ctor_EventHandler_CommandID_TestData()
        {
            yield return new object[] { new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10) };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_EventHandler_CommandID_TestData))]
        public void Ctor_EventHandler_CommandID(EventHandler handler, CommandID commandId)
        {
            var command = new MenuCommand(handler, commandId);
            Assert.Same(commandId, command.CommandID);
            Assert.Empty(command.Properties);

            Assert.True(command.Enabled);
            Assert.False(command.Checked);
            Assert.True(command.Supported);
            Assert.True(command.Visible);
            Assert.Equal(3, command.OleStatus);
        }

        [Fact]
        public void Properties_GetMultipleTimes_ReturnsEmpty()
        {
            var command = new MenuCommand(null, null);
            Assert.Same(command.Properties, command.Properties);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Enabled_Set_GetReturnsExpected(bool value)
        {
            var command = new MenuCommand(null, null) { Enabled = value };
            Assert.Equal(value, command.Enabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Checked_Set_GetReturnsExpected(bool value)
        {
            var command = new MenuCommand(null, null) { Checked = value };
            Assert.Equal(value, command.Checked);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Supported_Set_GetReturnsExpected(bool value)
        {
            var command = new MenuCommand(null, null) { Supported = value };
            Assert.Equal(value, command.Supported);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Visible_Set_GetReturnsExpected(bool value)
        {
            var command = new MenuCommand(null, null) { Visible = value };
            Assert.Equal(value, command.Visible);
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[]
            {
                new MenuCommand(new EventHandler(EventHandler), new CommandID(Guid.Empty, 0))
                {
                    Enabled = false,
                    Checked = false,
                    Supported = false,
                    Visible = false
                },
                "00000000-0000-0000-0000-000000000000 : 0 : "
            };

            yield return new object[]
            {
                new MenuCommand(new EventHandler(EventHandler), new CommandID(Guid.Empty, 0))
                {
                    Enabled = true,
                    Checked = true,
                    Supported = true,
                    Visible = true
                },
                "00000000-0000-0000-0000-000000000000 : 0 : Supported|Enabled|Visible|Checked"
            };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(MenuCommand command, string expected)
        {
            Assert.Equal(expected, command.ToString());
        }

        [Fact]
        public void ToString_NullCommandI_ReturnsExpected()
        {
            var command = new MenuCommand(new EventHandler(EventHandler), null);
            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Equal(" : Supported|Enabled|Visible", command.ToString());
            }
            else
            {
                Assert.Throws<NullReferenceException>(() => command.ToString());
            }
        }

        [Fact]
        public void Invoke_NonNullEventHandler_Success()
        {
            var command = new MenuCommand(new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10));
            command.Invoke();
            Assert.Same(command, CalledEventSender);
        }

        [Fact]
        public void Invoke_EventHandlerThrowsCanceledCheckoutException_Nop()
        {
            var command = new MenuCommand(new EventHandler(ThrowCanceledCheckoutException), new CommandID(Guid.NewGuid(), 10));
            command.Invoke();
            command.Invoke("arg");
        }

        [Fact]
        public void Invoke_EventHandlerThrowsCanceledCheckoutException_ThrowsException()
        {
            var command = new MenuCommand(new EventHandler(ThrowNonCanceledCheckoutException), new CommandID(Guid.NewGuid(), 10));
            Assert.Throws<CheckoutException>(() => command.Invoke());
            Assert.Throws<CheckoutException>(() => command.Invoke("arg"));
        }

        [Fact]
        public void Invoke_NullEventHandler_Nop()
        {
            var command = new MenuCommand(null, new CommandID(Guid.NewGuid(), 10));
            command.Invoke();
            command.Invoke("arg");
        }

        [Fact]
        public void SetStatus_CommandChanged_Invokes()
        {
            int calledCommandChanged = 0;

            var command = new MenuCommand(null, null);
            command.CommandChanged += (sender, e) =>
            {
                calledCommandChanged++;

                Assert.Same(command, sender);
                Assert.Equal(EventArgs.Empty, e);
            };

            command.Checked = false;
            Assert.Equal(0, calledCommandChanged);

            command.Checked = true;
            Assert.Equal(1, calledCommandChanged);

            command.Enabled = true;
            Assert.Equal(1, calledCommandChanged);

            command.Enabled = false;
            Assert.Equal(2, calledCommandChanged);

            command.Visible = true;
            Assert.Equal(2, calledCommandChanged);

            command.Visible = false;
            Assert.Equal(3, calledCommandChanged);

            command.Supported = true;
            Assert.Equal(3, calledCommandChanged);

            command.Supported = false;
            Assert.Equal(4, calledCommandChanged);
        }

        private static object CalledEventSender { get; set; }

        private static void EventHandler(object sender, EventArgs e)
        {
            CalledEventSender = sender;
            
            Assert.Equal(EventArgs.Empty, e);
        }

        private static void ThrowCanceledCheckoutException(object sender, EventArgs e)
        {
            throw CheckoutException.Canceled;
        }

        private static void ThrowNonCanceledCheckoutException(object sender, EventArgs e)
        {
            throw new CheckoutException();
        }
    }
}
