// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerVerbTests
    {
        public static IEnumerable<object[]> Ctor_Text_EventHandler_TestData()
        {
            yield return new object[] { "Text", new EventHandler(EventHandler), "Text" };
            yield return new object[] { "(&.)Text", new EventHandler(EventHandler), "Text" };
            yield return new object[] { null, null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Text_EventHandler_TestData))]
        public void Ctor_Text_EventHandler(string text, EventHandler handler, string expectedText)
        {
            var verb = new DesignerVerb(text, handler);
            Assert.Equal(expectedText ?? string.Empty, verb.Text);
            Assert.Equal(new Guid("{74D21313-2AEE-11d1-8BFB-00A0C90F26F7}"), verb.CommandID.Guid);
            Assert.Equal(0x2000, verb.CommandID.ID);
            Assert.Empty(verb.Description);
        }

        public static IEnumerable<object[]> Ctor_Text_EventHandler_CommandID_TestData()
        {
            yield return new object[] { "Text", new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10), "Text" };
            yield return new object[] { "(&.)Text", new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10), "Text" };
            yield return new object[] { null, null, null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Text_EventHandler_CommandID_TestData))]
        public void Ctor_Text_EventHandler_CommandID(string text, EventHandler handler, CommandID commandID, string expectedText)
        {
            var verb = new DesignerVerb(text, handler, commandID);
            Assert.Equal(expectedText ?? string.Empty, verb.Text);
            Assert.Same(commandID, verb.CommandID);
            Assert.Empty(verb.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Description")]
        public void Description_Set_GetReturnsExpected(string value)
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler)) { Description = value };
            Assert.Equal(value ?? string.Empty, verb.Description);
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            Assert.Equal("Text : 74d21313-2aee-11d1-8bfb-00a0c90f26f7 : 8192 : Supported|Enabled|Visible", verb.ToString());
        }

        private static void EventHandler(object sender, EventArgs e) { }
    }
}
