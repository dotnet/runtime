// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Authors:
//	Lawrence Pit (loz@cable.a2000.nl)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Portions (C) 2004 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections;
using System.Globalization;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class SecurityElementTest
    {
        private static SecurityElement CreateElement()
        {
            SecurityElement elem = new SecurityElement("IPermission");
            elem.AddAttribute("class", "System");
            elem.AddAttribute("version", "1");

            SecurityElement child = new SecurityElement("ConnectAccess");
            elem.AddChild(child);

            SecurityElement grandchild = new SecurityElement("ENDPOINT", "some text");
            grandchild.AddAttribute("transport", "All");
            grandchild.AddAttribute("host", "localhost");
            grandchild.AddAttribute("port", "8080");
            child.AddChild(grandchild);

            SecurityElement grandchild2 = new SecurityElement("ENDPOINT");
            grandchild2.AddAttribute("transport", "Tcp");
            grandchild2.AddAttribute("host", "www.ximian.com");
            grandchild2.AddAttribute("port", "All");
            child.AddChild(grandchild2);

            return elem;
        }

        [Fact]
        public void Constructor1()
        {
            const string tagName = "testTag";
            SecurityElement se = new SecurityElement(tagName);
            Assert.Null(se.Attributes);
            Assert.Null(se.Children);
            Assert.Equal(tagName, se.Tag);
            Assert.Null(se.Text);
        }

        [Fact]
        public void Constructor1_EmptyString()
        {
            SecurityElement se = new SecurityElement(string.Empty);
            Assert.Null(se.Attributes);
            Assert.Null(se.Children);
            Assert.Equal(string.Empty, se.Tag);
            Assert.Null(se.Text);
        }

        [Theory]
        [InlineData("Nam>e")]
        [InlineData("Na<me")]
        public void Constructor1_Tag_Invalid(string tagName)
        {
            try
            {
                new SecurityElement(tagName);
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf(tagName) != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Constructor1_Tag_Null()
        {
            try
            {
                new SecurityElement(null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("tag", ex.ParamName);
            }
        }

        [Fact]
        public void Constructor2()
        {
            SecurityElement se = new SecurityElement("tag", "text");
            Assert.Null(se.Attributes);
            Assert.Null(se.Children);
            Assert.Equal("tag", se.Tag);
            Assert.Equal("text", se.Text);
        }

        [Theory]
        [InlineData("Nam>e")]
        [InlineData("Na<me")]
        public void Constructor2_Tag_Invalid(string invalid)
        {
            try
            {
                new SecurityElement(invalid, "text");
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf(invalid) != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Constructor2_Tag_Null()
        {
            try
            {
                new SecurityElement(null, "text");
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("tag", ex.ParamName);
            }
        }

        [Fact]
        public void Constructor2_Text_Null()
        {
            SecurityElement se = new SecurityElement("tag", null);
            Assert.Null(se.Attributes);
            Assert.Null(se.Children);
            Assert.Equal("tag", se.Tag);
            Assert.Null(se.Text);
        }

        [Fact]
        public void AddAttribute_Name_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.AddAttribute(null, "valid");
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("name", ex.ParamName);
            }
        }

        [Fact]
        public void AddAttribute_Value_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.AddAttribute("valid", null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("value", ex.ParamName);
            }
        }

        [Fact]
        public void AddAttribute_Name_Invalid()
        {
            SecurityElement elem = CreateElement();
            AssertExtensions.Throws<ArgumentException>(null, () => elem.AddAttribute("<invalid>", "valid"));
        }

        [Fact]
        public void AddAttribute_Value_Invalid()
        {
            SecurityElement elem = CreateElement();
            AssertExtensions.Throws<ArgumentException>(null, () => elem.AddAttribute("valid", "invalid\""));
        }

        [Fact]
        public void AddAttribute_InvalidValue2()
        {
            SecurityElement elem = CreateElement();
            elem.AddAttribute("valid", "valid&");
            // in xml world this is actually not considered valid
            // but it is by MS.Net
        }

        [Fact]
        public void AddAttribute_InvalidValue3()
        {
            SecurityElement elem = CreateElement();
            AssertExtensions.Throws<ArgumentException>(null, () => elem.AddAttribute("valid", "<invalid>"));
        }

        [Fact]
        public void AddAttribute_Duplicate()
        {
            SecurityElement elem = CreateElement();
            elem.AddAttribute("valid", "first time");
            AssertExtensions.Throws<ArgumentException>(null, () => elem.AddAttribute("valid", "second time"));
        }

        [Fact]
        public void AddAttribute()
        {
            SecurityElement elem = CreateElement();
            elem.AddAttribute("valid", "valid\'");
        }

        [Fact]
        public void AddChild_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.AddChild(null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("child", ex.ParamName);
            }
        }

        [Fact]
        public void AddChild()
        {
            SecurityElement elem = CreateElement();
            int n = elem.Children.Count;
            // add itself
            elem.AddChild(elem);
            Assert.Equal((n + 1), elem.Children.Count);
        }

        [Fact]
        public void Attributes_Name_Invalid_MS()
        {
            SecurityElement elem = CreateElement();
            Hashtable h = elem.Attributes;
            h.Add("<invalid>", "valid");
            AssertExtensions.Throws<ArgumentException>(null, () => elem.Attributes = h);
        }

        [Fact]
        public void Attributes_Value_Invalid()
        {
            SecurityElement elem = CreateElement();
            Hashtable h = elem.Attributes;
            h.Add("valid", "\"invalid\"");
            try
            {
                elem.Attributes = h;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("\"invalid\"") != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Attributes()
        {
            SecurityElement elem = CreateElement();
            Hashtable h = elem.Attributes;

            h = elem.Attributes;
            h.Add("foo", "bar");
            Assert.True(elem.Attributes.Count != h.Count);

            elem.Attributes = h;
            Assert.NotNull(elem.Attribute("foo"));
        }

        [Fact]
        public void Equal()
        {
            SecurityElement elem = CreateElement();
            SecurityElement elem2 = CreateElement();
            Assert.True(elem.Equal(elem2));
            SecurityElement child = (SecurityElement)elem2.Children[0];
            child = (SecurityElement)child.Children[1];
            child.Text = "some text";
            Assert.False(elem.Equal(elem2));
        }

        [Fact]
        public void Escape()
        {
            Assert.Equal("foo&lt;&gt;&quot;&apos;&amp; bar",
                SecurityElement.Escape("foo<>\"'& bar"));
            Assert.Null(SecurityElement.Escape(null));
        }

        [Fact]
        public void IsValidAttributeName()
        {
            Assert.False(SecurityElement.IsValidAttributeName("x x"));
            Assert.False(SecurityElement.IsValidAttributeName("x<x"));
            Assert.False(SecurityElement.IsValidAttributeName("x>x"));
            Assert.True(SecurityElement.IsValidAttributeName("x\"x"));
            Assert.True(SecurityElement.IsValidAttributeName("x'x"));
            Assert.True(SecurityElement.IsValidAttributeName("x&x"));
            Assert.False(SecurityElement.IsValidAttributeName(null));
            Assert.True(SecurityElement.IsValidAttributeName(string.Empty));
        }

        [Fact]
        public void IsValidAttributeValue()
        {
            Assert.True(SecurityElement.IsValidAttributeValue("x x"));
            Assert.False(SecurityElement.IsValidAttributeValue("x<x"));
            Assert.False(SecurityElement.IsValidAttributeValue("x>x"));
            Assert.False(SecurityElement.IsValidAttributeValue("x\"x"));
            Assert.True(SecurityElement.IsValidAttributeValue("x'x"));
            Assert.True(SecurityElement.IsValidAttributeValue("x&x"));
            Assert.False(SecurityElement.IsValidAttributeValue(null));
            Assert.True(SecurityElement.IsValidAttributeValue(string.Empty));
        }

        [Fact]
        public void IsValidTag()
        {
            Assert.False(SecurityElement.IsValidTag("x x"));
            Assert.False(SecurityElement.IsValidTag("x<x"));
            Assert.False(SecurityElement.IsValidTag("x>x"));
            Assert.True(SecurityElement.IsValidTag("x\"x"));
            Assert.True(SecurityElement.IsValidTag("x'x"));
            Assert.True(SecurityElement.IsValidTag("x&x"));
            Assert.False(SecurityElement.IsValidTag(null));
            Assert.True(SecurityElement.IsValidTag(string.Empty));
        }

        [Fact]
        public void IsValidText()
        {
            Assert.True(SecurityElement.IsValidText("x x"));
            Assert.False(SecurityElement.IsValidText("x<x"));
            Assert.False(SecurityElement.IsValidText("x>x"));
            Assert.True(SecurityElement.IsValidText("x\"x"));
            Assert.True(SecurityElement.IsValidText("x'x"));
            Assert.True(SecurityElement.IsValidText("x&x"));
            Assert.False(SecurityElement.IsValidText(null));
            Assert.True(SecurityElement.IsValidText(string.Empty));
        }

        [Fact]
        public void SearchForChildByTag_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.SearchForChildByTag(null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("tag", ex.ParamName);
            }
        }

        [Fact]
        public void SearchForChildByTag()
        {
            SecurityElement elem = CreateElement();
            SecurityElement child = elem.SearchForChildByTag("doesnotexist");
            Assert.Null(child);

            child = elem.SearchForChildByTag("ENDPOINT");
            Assert.Null(child);

            child = (SecurityElement)elem.Children[0];
            child = child.SearchForChildByTag("ENDPOINT");
            Assert.Equal("All", child.Attribute("transport"));
        }

        [Fact]
        public void SearchForTextOfTag_Tag_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.SearchForTextOfTag(null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("tag", ex.ParamName);
            }
        }

        [Fact]
        public void SearchForTextOfTag()
        {
            SecurityElement elem = CreateElement();
            string s = elem.SearchForTextOfTag("ENDPOINT");
            Assert.Equal("some text", s);
        }

        [Fact]
        public void Tag()
        {
            SecurityElement se = new SecurityElement("Values");
            Assert.Equal("Values", se.Tag);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture,
                "<Values/>{0}", Environment.NewLine),
                se.ToString());
            se.Tag = "abc:Name";
            Assert.Equal("abc:Name", se.Tag);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture,
                "<abc:Name/>{0}", Environment.NewLine),
                se.ToString());
            se.Tag = "Name&Address";
            Assert.Equal("Name&Address", se.Tag);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture,
                "<Name&Address/>{0}", Environment.NewLine),
                se.ToString());
            se.Tag = string.Empty;
            Assert.Equal(string.Empty, se.Tag);
            Assert.Equal(string.Format(CultureInfo.InvariantCulture,
                "</>{0}", Environment.NewLine),
                se.ToString());
        }

        [Theory]
        [InlineData("Nam>e")]
        [InlineData("Na<me")]
        public void Tag_Invalid(string invalid)
        {
            SecurityElement se = new SecurityElement("Values");
            try
            {
                se.Tag = invalid;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf(invalid) != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Tag_Null()
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.Tag = null;
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("Tag", ex.ParamName);
            }
        }

        [Fact]
        public void Text()
        {
            SecurityElement elem = CreateElement();
            elem.Text = "Basic string";
            Assert.Equal("Basic string", elem.Text);
            elem.Text = null;
            Assert.Null(elem.Text);
            elem.Text = string.Empty;
            Assert.Equal(string.Empty, elem.Text);
            elem.Text = "&lt;sample&amp;practice&unresolved;&gt;";
            Assert.Equal("<sample&practice&unresolved;>", elem.Text);
        }

        [Theory]
        [InlineData("TagWith<Bracket")]
        [InlineData("TagWith>Bracket")]
        public void Text_Invalid(string invalid)
        {
            SecurityElement elem = CreateElement();
            try
            {
                elem.Text = invalid;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf(invalid) != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void MultipleAttributes()
        {
            SecurityElement se = new SecurityElement("Multiple");
            se.AddAttribute("Attribute1", "One");
            se.AddAttribute("Attribute2", "Two");

            string expected = string.Format("<Multiple Attribute1=\"One\"{0}Attribute2=\"Two\"/>{0}", Environment.NewLine);
            Assert.Equal(expected, se.ToString());
        }

        [Fact]
        public void FromString_Null()
        {
            try
            {
                SecurityElement.FromString(null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("xml", ex.ParamName);
            }
        }
    }
}
