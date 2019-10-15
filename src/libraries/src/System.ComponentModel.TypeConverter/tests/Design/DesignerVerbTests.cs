// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerVerbTests
    {
        public static IEnumerable<object[]> Ctor_String_EventHandler_TestData()
        {
            yield return new object[] { "Text", new EventHandler(EventHandler), "Text", "Text" };
            yield return new object[] { "(&.)Text", new EventHandler(EventHandler), "Text", "Text" };
            yield return new object[] { null, null, string.Empty, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_EventHandler_TestData))]
        public void Ctor_String_EventHandler(string text, EventHandler handler, string expectedText, string expectedPropertiesText)
        {
            var verb = new DesignerVerb(text, handler);
            Assert.Equal(new Guid("{74D21313-2AEE-11d1-8BFB-00A0C90F26F7}"), verb.CommandID.Guid);
            Assert.Equal(0x2000, verb.CommandID.ID);
            Assert.Empty(verb.Description);
            Assert.True(verb.Enabled);
            Assert.Equal(3, verb.OleStatus);
            Assert.True(verb.Enabled);
            Assert.Equal(expectedText, verb.Text);
            Assert.IsType<HybridDictionary>(verb.Properties);
            Assert.Same(verb.Properties, verb.Properties);
            DictionaryEntry entry = Assert.IsType<DictionaryEntry>(Assert.Single(verb.Properties));
            Assert.Equal("Text", entry.Key);
            Assert.Equal(expectedPropertiesText, entry.Value);
            Assert.True(verb.Supported);
            Assert.True(verb.Visible);
        }

        public static IEnumerable<object[]> Ctor_String_EventHandler_CommandID_TestData()
        {
            yield return new object[] { "Text", new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10), "Text", "Text" };
            yield return new object[] { "(&.)Text", new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10), "Text", "Text" };
            yield return new object[] { null, null, null, string.Empty, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_EventHandler_CommandID_TestData))]
        public void Ctor_String_EventHandler_CommandID(string text, EventHandler handler, CommandID commandID, string expectedText, string expectedPropertiesText)
        {
            var verb = new DesignerVerb(text, handler, commandID);
            Assert.Equal(commandID, verb.CommandID);
            Assert.Empty(verb.Description);
            Assert.True(verb.Enabled);
            Assert.Equal(3, verb.OleStatus);
            Assert.True(verb.Enabled);
            Assert.Equal(expectedText, verb.Text);
            Assert.IsType<HybridDictionary>(verb.Properties);
            Assert.Same(verb.Properties, verb.Properties);
            DictionaryEntry entry = Assert.IsType<DictionaryEntry>(Assert.Single(verb.Properties));
            Assert.Equal("Text", entry.Key);
            Assert.Equal(expectedPropertiesText, entry.Value);
            Assert.True(verb.Supported);
            Assert.True(verb.Visible);
        }

        [Fact]
        public void Ctor_NullProperties_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new NullPropertiesDesignerVerb("Text", new EventHandler(EventHandler)));
            Assert.Throws<NullReferenceException>(() => new NullPropertiesDesignerVerb("Text", new EventHandler(EventHandler), new CommandID(Guid.NewGuid(), 10)));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("Description", "Description")]
        public void Description_GetWithProperties_ReturnsExpected(string value, string expected)
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            verb.Properties["Description"] = value;
            Assert.Equal(expected, verb.Description);
        }

        [Fact]
        public void Description_GetWithPropertiesInvalidType_ThrowsInvalidCastException()
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            verb.Properties["Description"] = new object();
            Assert.Throws<InvalidCastException>(() => verb.Description);
        }

        [Fact]
        public void Description_GetWithNullProperties_ThrowsNullReferenceException()
        {
            var verb = new NullPropertiesAfterConstructionDesignerVerb("Text", new EventHandler(EventHandler));
            Assert.Throws<NullReferenceException>(() => verb.Description = "value");
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("Description", "Description")]
        public void Description_Set_GetReturnsExpected(string value, string expected)
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler))
            {
                Description = value
            };
            Assert.Equal(expected, verb.Description);
            Assert.Equal(value, verb.Properties["Description"]);

            // Set same.
            verb.Description = value;
            Assert.Equal(expected, verb.Description);
            Assert.Equal(value, verb.Properties["Description"]);
        }

        [Fact]
        public void Description_SetWithNullProperties_ThrowsNullReferenceException()
        {
            var verb = new NullPropertiesAfterConstructionDesignerVerb("Text", new EventHandler(EventHandler));
            Assert.Throws<NullReferenceException>(() => verb.Description);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("Text", "Text")]
        public void Text_GetWithProperties_ReturnsExpected(string value, string expected)
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            verb.Properties["Text"] = value;
            Assert.Equal(expected, verb.Text);
        }

        [Fact]
        public void Text_GetWithPropertiesInvalidType_ThrowsInvalidCastException()
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            verb.Properties["Text"] = new object();
            Assert.Throws<InvalidCastException>(() => verb.Text);
        }

        [Fact]
        public void Text_GetWithNullProperties_ThrowsNullReferenceException()
        {
            var verb = new NullPropertiesAfterConstructionDesignerVerb("Text", new EventHandler(EventHandler));
            Assert.Throws<NullReferenceException>(() => verb.Text);
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var verb = new DesignerVerb("Text", new EventHandler(EventHandler));
            Assert.Equal("Text : 74d21313-2aee-11d1-8bfb-00a0c90f26f7 : 8192 : Supported|Enabled|Visible", verb.ToString());
        }

        private static void EventHandler(object sender, EventArgs e) { }

        private class NullPropertiesDesignerVerb : DesignerVerb
        {
            public NullPropertiesDesignerVerb(string text, EventHandler handler) : base(text, handler)
            {
            }

            public NullPropertiesDesignerVerb(string text, EventHandler handler, CommandID startCommandID) : base(text, handler, startCommandID)
            {
            }

            public override IDictionary Properties => null;
        }

        private class NullPropertiesAfterConstructionDesignerVerb : DesignerVerb
        {
            public NullPropertiesAfterConstructionDesignerVerb(string text, EventHandler handler) : base(text, handler)
            {
            }

            public NullPropertiesAfterConstructionDesignerVerb(string text, EventHandler handler, CommandID startCommandID) : base(text, handler, startCommandID)
            {
            }

            private bool Constructed { get; set; }

            public override IDictionary Properties
            {
                get
                {
                    if (!Constructed)
                    {
                        Constructed = true;
                        return base.Properties;
                    }

                    return null;
                }
            }
        }
    }
}
