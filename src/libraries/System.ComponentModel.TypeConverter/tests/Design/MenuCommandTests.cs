// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
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
            Assert.False(command.Checked);
            Assert.Same(commandId, command.CommandID);
            Assert.True(command.Enabled);
            Assert.Equal(3, command.OleStatus);
            Assert.IsType<HybridDictionary>(command.Properties);
            Assert.Empty(command.Properties);
            Assert.Same(command.Properties, command.Properties);
            Assert.True(command.Supported);
            Assert.True(command.Visible);
        }

        [Theory]
        [InlineData(true, 7, 3)]
        [InlineData(false, 3, 7)]
        public void Checked_Set_GetReturnsExpected(bool value, int expectedOleStatus1, int expectedOleStatus2)
        {
            var command = new MenuCommand(null, null)
            {
                Checked = value 
            };
            Assert.Equal(value, command.Checked);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set same.
            command.Checked = value;
            Assert.Equal(value, command.Checked);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set different.
            command.Checked = !value;
            Assert.Equal(!value, command.Checked);
            Assert.Equal(expectedOleStatus2, command.OleStatus);
        }

        [Fact]
        public void Checked_SetWithCommandChanged_CallsHandler()
        {
            var command = new MenuCommand(null, null);
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(command, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            command.CommandChanged += handler;

            // Set different.
            command.Checked = true;
            Assert.True(command.Checked);
            Assert.Equal(1, callCount);

            // Set same.
            command.Checked = true;
            Assert.True(command.Checked);
            Assert.Equal(1, callCount);

            // Set different.
            command.Checked = false;
            Assert.False(command.Checked);
            Assert.Equal(2, callCount);

            // Remove handler.
            command.CommandChanged -= handler;
            command.Checked = true;
            Assert.True(command.Checked);
            Assert.Equal(2, callCount);
        }

        [Theory]
        [InlineData(true, 3, 1)]
        [InlineData(false, 1, 3)]
        public void Enabled_Set_GetReturnsExpected(bool value, int expectedOleStatus1, int expectedOleStatus2)
        {
            var command = new MenuCommand(null, null)
            {
                Enabled = value
            };
            Assert.Equal(value, command.Enabled);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set same.
            command.Enabled = value;
            Assert.Equal(value, command.Enabled);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set different.
            command.Enabled = !value;
            Assert.Equal(!value, command.Enabled);
            Assert.Equal(expectedOleStatus2, command.OleStatus);
        }

        [Fact]
        public void Enabled_SetWithCommandChanged_CallsHandler()
        {
            var command = new MenuCommand(null, null);
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(command, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            command.CommandChanged += handler;

            // Set different.
            command.Enabled = false;
            Assert.False(command.Enabled);
            Assert.Equal(1, callCount);

            // Set same.
            command.Enabled = false;
            Assert.False(command.Enabled);
            Assert.Equal(1, callCount);

            // Set different.
            command.Enabled = true;
            Assert.True(command.Enabled);
            Assert.Equal(2, callCount);

            // Remove handler.
            command.CommandChanged -= handler;
            command.Enabled = false;
            Assert.False(command.Enabled);
            Assert.Equal(2, callCount);
        }

        [Theory]
        [InlineData(true, 3, 2)]
        [InlineData(false, 2, 3)]
        public void Supported_Set_GetReturnsExpected(bool value, int expectedOleStatus1, int expectedOleStatus2)
        {
            var command = new MenuCommand(null, null)
            {
                Supported = value
            };
            Assert.Equal(value, command.Supported);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set same.
            command.Supported = value;
            Assert.Equal(value, command.Supported);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set different.
            command.Supported = !value;
            Assert.Equal(!value, command.Supported);
            Assert.Equal(expectedOleStatus2, command.OleStatus);
        }

        [Fact]
        public void Supported_SetWithCommandChanged_CallsHandler()
        {
            var command = new MenuCommand(null, null);
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(command, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            command.CommandChanged += handler;

            // Set different.
            command.Supported = false;
            Assert.False(command.Supported);
            Assert.Equal(1, callCount);

            // Set same.
            command.Supported = false;
            Assert.False(command.Supported);
            Assert.Equal(1, callCount);

            // Set different.
            command.Supported = true;
            Assert.True(command.Supported);
            Assert.Equal(2, callCount);

            // Remove handler.
            command.CommandChanged -= handler;
            command.Supported = false;
            Assert.False(command.Supported);
            Assert.Equal(2, callCount);
        }

        [Theory]
        [InlineData(true, 3, 19)]
        [InlineData(false, 19, 3)]
        public void Visible_Set_GetReturnsExpected(bool value, int expectedOleStatus1, int expectedOleStatus2)
        {
            var command = new MenuCommand(null, null)
            {
                Visible = value
            };
            Assert.Equal(value, command.Visible);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set same.
            command.Visible = value;
            Assert.Equal(value, command.Visible);
            Assert.Equal(expectedOleStatus1, command.OleStatus);

            // Set different.
            command.Visible = !value;
            Assert.Equal(!value, command.Visible);
            Assert.Equal(expectedOleStatus2, command.OleStatus);
        }

        [Fact]
        public void Visible_SetWithCommandChanged_CallsHandler()
        {
            var command = new MenuCommand(null, null);
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(command, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };
            command.CommandChanged += handler;

            // Set different.
            command.Visible = false;
            Assert.False(command.Visible);
            Assert.Equal(1, callCount);

            // Set same.
            command.Visible = false;
            Assert.False(command.Visible);
            Assert.Equal(1, callCount);

            // Set different.
            command.Visible = true;
            Assert.True(command.Visible);
            Assert.Equal(2, callCount);

            // Remove handler.
            command.CommandChanged -= handler;
            command.Visible = false;
            Assert.False(command.Visible);
            Assert.Equal(2, callCount);
        }

        public static IEnumerable<object[]> OnCommandChanged_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new EventArgs() };
        }

        [Theory]
        [MemberData(nameof(OnCommandChanged_TestData))]
        public void OnCommandChanged_Invoke_CallsCommandChanged(EventArgs eventArgs)
        {
            var command = new SubMenuCommand(null, null);
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(command, sender);
                Assert.Same(eventArgs, e);
                callCount++;
            };
            command.CommandChanged += handler;

            // Call with handler.
            command.OnCommandChanged(eventArgs);
            Assert.Equal(1, callCount);

            // Remove handler.
            command.CommandChanged -= handler;
            command.OnCommandChanged(eventArgs);
            Assert.Equal(1, callCount);
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

            yield return new object[] { new MenuCommand(new EventHandler(EventHandler), null), " : Supported|Enabled|Visible" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(MenuCommand command, string expected)
        {
            Assert.Equal(expected, command.ToString());
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

        private static object CalledEventSender { get; set; }

        private static void EventHandler(object sender, EventArgs e)
        {
            CalledEventSender = sender;
            Assert.Same(EventArgs.Empty, e);
        }

        private static void ThrowCanceledCheckoutException(object sender, EventArgs e)
        {
            throw CheckoutException.Canceled;
        }

        private static void ThrowNonCanceledCheckoutException(object sender, EventArgs e)
        {
            throw new CheckoutException();
        }

        private class SubMenuCommand : MenuCommand
        {
            public SubMenuCommand(EventHandler handler, CommandID command) : base(handler, command)
            {
            }

            public new void OnCommandChanged(EventArgs e) => base.OnCommandChanged(e);
        }
    }
}
