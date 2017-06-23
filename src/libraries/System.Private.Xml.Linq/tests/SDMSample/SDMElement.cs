// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_Element
    {
        /// <summary>
        /// Validate behavior of XElement simple creation.
        /// </summary>
        [Fact]
        public void CreateElementSimple()
        {
            const string ElementName = "Element";

            XElement element;

            // Test the constructor that takes only a name.
            element = new XElement(ElementName);
            Assert.Equal(ElementName, element.Name.ToString());
            Assert.Throws<ArgumentNullException>(() => new XElement((XName)null));
        }

        /// <summary>
        /// Validate behavior of XElement creation with content supplied.
        /// </summary>
        [Fact]
        public void CreateElementWithContent()
        {
            // Test the constructor that takes a name and some content.
            XElement level2Element = new XElement("Level2", "TextValue");
            XAttribute attribute = new XAttribute("Attribute", "AttributeValue");
            XCData cdata = new XCData("abcdefgh");
            string someValue = "text";

            XElement element = new XElement("Level1", level2Element, cdata, someValue, attribute);

            Assert.Equal("Level1", element.Name.ToString());

            Assert.Equal(
                new XNode[] { level2Element, cdata, new XText(someValue) },
                element.Nodes(),
                XNode.EqualityComparer);

            Assert.Equal(new[] { attribute.Name }, element.Attributes().Select(x => x.Name));
            Assert.Equal(new[] { attribute.Value }, element.Attributes().Select(x => x.Value));
        }

        /// <summary>
        /// Validate behavior of XElement creation with copy constructor.
        /// </summary>
        [Fact]
        public void CreateElementCopy()
        {
            // With attributes
            XElement level2Element = new XElement("Level2", "TextValue");
            XAttribute attribute = new XAttribute("Attribute", "AttributeValue");
            XCData cdata = new XCData("abcdefgh");
            string someValue = "text";

            XElement element = new XElement("Level1", level2Element, cdata, someValue, attribute);

            XElement elementCopy = new XElement(element);

            Assert.Equal("Level1", element.Name.ToString());

            Assert.Equal(
                new XNode[] { level2Element, cdata, new XText(someValue) },
                elementCopy.Nodes(),
                XNode.EqualityComparer);

            Assert.Equal(new[] { attribute.Name }, element.Attributes().Select(x => x.Name));
            Assert.Equal(new[] { attribute.Value }, element.Attributes().Select(x => x.Value));

            // Without attributes
            element = new XElement("Level1", level2Element, cdata, someValue);
            elementCopy = new XElement(element);

            Assert.Equal("Level1", element.Name.ToString());

            Assert.Equal(
                new XNode[] { level2Element, cdata, new XText(someValue) },
                elementCopy.Nodes(),
                XNode.EqualityComparer);

            Assert.Empty(elementCopy.Attributes());

            // Hsh codes of equal elements should be equal.
            Assert.Equal(XNode.EqualityComparer.GetHashCode(element), XNode.EqualityComparer.GetHashCode(elementCopy));

            // Null element is not allowed.
            Assert.Throws<ArgumentNullException>(() => new XElement((XElement)null));
        }

        /// <summary>
        /// Validate behavior of XElement creation from an XmlReader.
        /// </summary>
        [Fact]
        public void CreateElementFromReader()
        {
            string xml = "<Level1 a1='1' a2='2'><Level2><![CDATA[12345678]]>text</Level2></Level1>";
            string xml2 = "<Level1 />";
            string xml3 = "<x><?xml version='1.0' encoding='utf-8'?></x>";

            // With attributes
            using (TextReader textReader = new StringReader(xml))
            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                xmlReader.Read();

                XElement element = (XElement)XNode.ReadFrom(xmlReader);

                Assert.Equal("Level1", element.Name.ToString());
                Assert.Equal(new[] { "Level2" }, element.Elements().Select(x => x.Name.ToString()));

                Assert.Equal(new[] { "a1", "a2" }, element.Attributes().Select(x => x.Name.ToString()));
                Assert.Equal(new[] { "1", "2" }, element.Attributes().Select(x => x.Value));

                Assert.Equal("12345678text", element.Element("Level2").Value);
            }

            // Without attributes
            using (TextReader textReader = new StringReader(xml2))
            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                xmlReader.Read();

                var element = (XElement)XNode.ReadFrom(xmlReader);

                Assert.Equal("Level1", element.Name.ToString());
                Assert.Empty(element.Elements());
                Assert.Empty(element.Attributes());

                Assert.Empty(element.Value);
            }

            // XmlReader in start state results in exception
            using (TextReader textReader = new StringReader(xml))
            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                Assert.Throws<InvalidOperationException>(() => (XElement)XNode.ReadFrom(xmlReader));
            }

            // XmlReader not on an element results in exception.
            using (TextReader textReader = new StringReader(xml))
            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                xmlReader.Read();
                xmlReader.MoveToAttribute("a1");

                Assert.Throws<InvalidOperationException>(() => (XElement)XNode.ReadFrom(xmlReader));
            }

            // Illegal xml triggers exception that is bubbled out.
            using (TextReader textReader = new StringReader(xml3))
            using (XmlReader xmlReader = XmlReader.Create(textReader))
            {
                xmlReader.Read();

                Assert.Throws<XmlException>(() => (XElement)XNode.ReadFrom(xmlReader));
            }
        }

        /// <summary>
        /// Validate behavior of XElement EmptySequence method.
        /// </summary>
        [Fact]
        public void ElementEmptyElementSequence()
        {
            Assert.Empty(XElement.EmptySequence);
            Assert.Empty(XElement.EmptySequence);
        }

        /// <summary>
        /// Validate behavior of XElement HasAttributes/HasElements properties.
        /// </summary>
        [Fact]
        public void ElementHasAttributesAndElements()
        {
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", new XAttribute("a", "value"));
            XElement e3 = new XElement("x", new XElement("y"));
            XElement e4 = new XElement("x", new XCData("cdata-value"));

            Assert.False(e1.HasAttributes);
            Assert.True(e2.HasAttributes);

            Assert.False(e1.HasElements);
            Assert.False(e2.HasElements);
            Assert.True(e3.HasElements);
            Assert.False(e4.HasElements);
        }

        /// <summary>
        /// Validate behavior of the IsEmpty property.
        /// </summary>
        [Fact]
        public void ElementIsEmpty()
        {
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", 10);
            XElement e3 = new XElement("x", string.Empty);

            Assert.True(e1.IsEmpty);
            Assert.False(e2.IsEmpty);
            Assert.False(e3.IsEmpty);
        }

        /// <summary>
        /// Validate behavior of the Value property on XElement.
        /// </summary>
        [Fact]
        public void ElementValue()
        {
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "value");
            XElement e3 = new XElement("x", 100, 200);
            XElement e4 = new XElement("x", 100, "value", 200);
            XElement e5 = new XElement("x", string.Empty);
            XElement e6 = new XElement("x", 1, string.Empty, 5);
            XElement e7 = new XElement("x", new XElement("y", "inner1", new XElement("z", "foo"), "inner2"));
            XElement e8 = new XElement("x", "text1", new XElement("y", "inner"), "text2");
            XElement e9 = new XElement("x", "text1", new XText("abcd"), new XElement("y", "y"));
            XElement e10 = new XElement("x", new XComment("my comment"));

            Assert.Empty(e1.Value);
            Assert.Equal("value", e2.Value);
            Assert.Equal("100200", e3.Value);
            Assert.Equal("100value200", e4.Value);
            Assert.Empty(e5.Value);
            Assert.Equal("15", e6.Value);
            Assert.Equal("inner1fooinner2", e7.Value);
            Assert.Equal("text1innertext2", e8.Value);
            Assert.Equal("text1abcdy", e9.Value);
            Assert.Empty(e10.Value);

            Assert.Throws<ArgumentNullException>(() => e1.Value = null);

            e1.Value = string.Empty;
            e2.Value = "not-empty";

            Assert.Empty(e1.Value);
            Assert.Equal("not-empty", e2.Value);
        }

        /// <summary>
        /// Validates the explicit string conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToString()
        {
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", string.Empty);
            XElement e3 = new XElement("x", "value");

            Assert.Null((string)((XElement)null));
            Assert.Empty((string)e1);
            Assert.Empty((string)e2);
            Assert.Equal("value", (string)e3);
        }

        /// <summary>
        /// Validates the explicit boolean conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToBoolean()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (bool)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "true");
            XElement e4 = new XElement("x", "false");
            XElement e5 = new XElement("x", "0");
            XElement e6 = new XElement("x", "1");

            Assert.Throws<FormatException>(() => (bool)e1);
            Assert.Throws<FormatException>(() => (bool)e2);

            Assert.True((bool)e3);
            Assert.False((bool)e4);
            Assert.False((bool)e5);
            Assert.True((bool)e6);
        }

        /// <summary>
        /// Validates the explicit int32 conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToInt32()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (int)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "2147483648");
            XElement e4 = new XElement("x", "5");

            Assert.Throws<FormatException>(() => (int)e1);
            Assert.Throws<FormatException>(() => (int)e2);
            Assert.Throws<OverflowException>(() => (int)e3);

            Assert.Equal(5, (int)e4);
        }

        /// <summary>
        /// Validates the explicit uint32 conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToUInt32()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (uint)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "4294967296");
            XElement e4 = new XElement("x", "5");

            Assert.Throws<FormatException>(() => (uint)e1);
            Assert.Throws<FormatException>(() => (uint)e2);
            Assert.Throws<OverflowException>(() => (uint)e3);

            Assert.Equal(5u, (uint)e4);
        }

        /// <summary>
        /// Validates the explicit int64 conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToInt64()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (long)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "18446744073709551616");
            XElement e4 = new XElement("x", "5");

            Assert.Throws<FormatException>(() => (long)e1);
            Assert.Throws<FormatException>(() => (long)e2);
            Assert.Throws<OverflowException>(() => (long)e3);

            Assert.Equal(5L, (long)e4);
        }

        /// <summary>
        /// Validates the explicit uint64 conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToUInt64()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (ulong)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "18446744073709551616");
            XElement e4 = new XElement("x", "5");

            Assert.Throws<FormatException>(() => (ulong)e1);
            Assert.Throws<FormatException>(() => (ulong)e2);
            Assert.Throws<OverflowException>(() => (ulong)e3);

            Assert.Equal(5UL, (ulong)e4);
        }

        /// <summary>
        /// Validates the explicit float conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToFloat()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (float)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "5e+500");
            XElement e4 = new XElement("x", "5.0");

            Assert.Throws<FormatException>(() => (float)e1);
            Assert.Throws<FormatException>(() => (float)e2);
            Assert.Throws<OverflowException>(() => (float)e3);

            Assert.Equal(5.0f, (float)e4);
        }

        /// <summary>
        /// Validates the explicit double conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToDouble()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (double)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "5e+5000");
            XElement e4 = new XElement("x", "5.0");

            Assert.Throws<FormatException>(() => (double)e1);
            Assert.Throws<FormatException>(() => (double)e2);
            Assert.Throws<OverflowException>(() => (double)e3);

            Assert.Equal(5.0, (double)e4);
        }

        /// <summary>
        /// Validates the explicit decimal conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToDecimal()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (decimal)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "111111111111111111111111111111111111111111111111");
            XElement e4 = new XElement("x", "5.0");

            Assert.Throws<FormatException>(() => (decimal)e1);
            Assert.Throws<FormatException>(() => (decimal)e2);
            Assert.Throws<OverflowException>(() => (decimal)e3);

            Assert.Equal(5.0m, (decimal)e4);
        }

        /// <summary>
        /// Validates the explicit DateTime conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToDateTime()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (DateTime)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "1968-01-07");

            Assert.Throws<FormatException>(() => (DateTime)e1);
            Assert.Throws<FormatException>(() => (DateTime)e2);

            Assert.Equal(new DateTime(1968, 1, 7), (DateTime)e3);
        }

        /// <summary>
        /// Validates the explicit TimeSpan conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToTimeSpan()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (TimeSpan)((XElement)null));

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", "PT1H2M3S");

            Assert.Throws<FormatException>(() => (TimeSpan)e1);
            Assert.Throws<FormatException>(() => (TimeSpan)e2);

            Assert.Equal(new TimeSpan(1, 2, 3), (TimeSpan)e3);
        }

        /// <summary>
        /// Validates the explicit guid conversion operator on XElement.
        /// </summary>
        [Fact]
        public void ElementExplicitToGuid()
        {
            // Calling explicit operator with null should result in exception.
            Assert.Throws<ArgumentNullException>(() => (Guid)((XElement)null));

            string guid = "2b67e9fb-97ad-4258-8590-8bc8c2d32df5";

            // Test various values.
            XElement e1 = new XElement("x");
            XElement e2 = new XElement("x", "bogus");
            XElement e3 = new XElement("x", guid);

            Assert.Throws<FormatException>(() => (Guid)e1);
            Assert.Throws<FormatException>(() => (Guid)e2);

            Assert.Equal(new Guid(guid), (Guid)e3);
        }

        /// <summary>
        /// Validates the explicit conversion operators on XElement
        /// for nullable value types.
        /// </summary>
        [Fact]
        public void ElementExplicitToNullables()
        {
            string guid = "cd8d69ed-fef9-4283-aaf4-216463e4496f";

            bool? b = (bool?)new XElement("x", true);
            int? i = (int?)new XElement("x", 5);
            uint? u = (uint?)new XElement("x", 5);
            long? l = (long?)new XElement("x", 5);
            ulong? ul = (ulong?)new XElement("x", 5);
            float? f = (float?)new XElement("x", 5);
            double? n = (double?)new XElement("x", 5);
            decimal? d = (decimal?)new XElement("x", 5);
            DateTime? dt = (DateTime?)new XElement("x", "1968-01-07");
            TimeSpan? ts = (TimeSpan?)new XElement("x", "PT1H2M3S");
            Guid? g = (Guid?)new XElement("x", guid);

            Assert.True(b.Value);
            Assert.Equal(5, i.Value);
            Assert.Equal(5u, u.Value);
            Assert.Equal(5L, l.Value);
            Assert.Equal(5uL, ul.Value);
            Assert.Equal(5.0f, f.Value);
            Assert.Equal(5.0, n.Value);
            Assert.Equal(5.0m, d.Value);
            Assert.Equal(new DateTime(1968, 1, 7), dt.Value);
            Assert.Equal(new TimeSpan(1, 2, 3), ts.Value);
            Assert.Equal(new Guid(guid), g.Value);

            b = (bool?)((XElement)null);
            i = (int?)((XElement)null);
            u = (uint?)((XElement)null);
            l = (long?)((XElement)null);
            ul = (ulong?)((XElement)null);
            f = (float?)((XElement)null);
            n = (double?)((XElement)null);
            d = (decimal?)((XElement)null);
            dt = (DateTime?)((XElement)null);
            ts = (TimeSpan?)((XElement)null);
            g = (Guid?)((XElement)null);

            Assert.Null(b);
            Assert.Null(i);
            Assert.Null(u);
            Assert.Null(l);
            Assert.Null(ul);
            Assert.Null(f);
            Assert.Null(n);
            Assert.Null(d);
            Assert.Null(dt);
            Assert.Null(ts);
            Assert.Null(g);
        }

        /// <summary>
        /// Validate enumeration of element ancestors.
        /// </summary>
        [Fact]
        public void ElementAncestors()
        {
            XElement level3 = new XElement("Level3");
            XElement level2 = new XElement("Level2", level3);
            XElement level1 = new XElement("Level1", level2);
            XElement level0 = new XElement("Level1", level1);


            Assert.Equal(new XElement[] { level2, level1, level0 }, level3.Ancestors(), XNode.EqualityComparer);

            Assert.Equal(new XElement[] { level1, level0 }, level3.Ancestors("Level1"), XNode.EqualityComparer);

            Assert.Empty(level3.Ancestors(null));

            Assert.Equal(
                new XElement[] { level3, level2, level1, level0 },
                level3.AncestorsAndSelf(),
                XNode.EqualityComparer);

            Assert.Equal(new XElement[] { level3 }, level3.AncestorsAndSelf("Level3"), XNode.EqualityComparer);

            Assert.Empty(level3.AncestorsAndSelf(null));
        }

        /// <summary>
        /// Validate enumeration of element descendents.
        /// </summary>
        [Fact]
        public void ElementDescendents()
        {
            XComment comment = new XComment("comment");
            XElement level3 = new XElement("Level3");
            XElement level2 = new XElement("Level2", level3);
            XElement level1 = new XElement("Level1", level2, comment);
            XElement level0 = new XElement("Level1", level1);

            Assert.Equal(new XElement[] { level1, level2, level3 }, level1.DescendantsAndSelf(), XNode.EqualityComparer);

            Assert.Equal(
                new XNode[] { level0, level1, level2, level3, comment },
                level0.DescendantNodesAndSelf(),
                XNode.EqualityComparer);

            Assert.Empty(level0.DescendantsAndSelf(null));

            Assert.Equal(new XElement[] { level0, level1 }, level0.DescendantsAndSelf("Level1"), XNode.EqualityComparer);
        }

        /// <summary>
        /// Validate enumeration of element attributes.
        /// </summary>
        [Fact]
        public void ElementAttributes()
        {
            XElement e1 = new XElement("x");

            XElement e2 = new XElement(
                "x",
                new XAttribute("a1", "1"),
                new XAttribute("a2", "2"),
                new XAttribute("a3", "3"),
                new XAttribute("a4", "4"),
                new XAttribute("a5", "5"));

            XElement e3 = new XElement(
                "x",
                new XAttribute("a1", "1"),
                new XAttribute("a2", "2"),
                new XAttribute("a3", "3"));

            Assert.Null(e1.Attribute("foo"));
            Assert.Null(e2.Attribute("foo"));

            Assert.Equal(e2.Attribute("a3").Name.ToString(), "a3");
            Assert.Equal(e2.Attribute("a3").Value, "3");

            Assert.Equal(new[] { "a1", "a2", "a3", "a4", "a5" }, e2.Attributes().Select(x => x.Name.ToString()));
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, e2.Attributes().Select(x => x.Value));

            Assert.Equal(new[] { "a1" }, e2.Attributes("a1").Select(x => x.Name.ToString()));
            Assert.Equal(new[] { "5" }, e2.Attributes("a5").Select(x => x.Value));

            Assert.Empty(e2.Attributes(null));

            e2.RemoveAttributes();
            Assert.Empty(e2.Attributes());

            // Removal of non-existent attribute
            e1.SetAttributeValue("foo", null);
            Assert.Empty(e1.Attributes());

            // Add of non-existent attribute
            e1.SetAttributeValue("foo", "foo-value");
            Assert.Equal(e1.Attribute("foo").Name.ToString(), "foo");
            Assert.Equal(e1.Attribute("foo").Value, "foo-value");

            // Overwriting of existing attribute
            e1.SetAttributeValue("foo", "noo-value");
            Assert.Equal(e1.Attribute("foo").Name.ToString(), "foo");
            Assert.Equal(e1.Attribute("foo").Value, "noo-value");

            // Effective removal of existing attribute
            e1.SetAttributeValue("foo", null);
            Assert.Empty(e1.Attributes());

            // These 3 are in a specific order to exercise the attribute removal code.
            e3.SetAttributeValue("a2", null);
            Assert.Equal(2, e3.Attributes().Count());

            e3.SetAttributeValue("a3", null);
            Assert.Equal(1, e3.Attributes().Count());

            e3.SetAttributeValue("a1", null);
            Assert.Empty(e3.Attributes());
        }

        /// <summary>
        /// Validates remove methods on elements.
        /// </summary>
        [Fact]
        public void ElementRemove()
        {
            XElement e = new XElement(
                "x",
                new XAttribute("a1", 1),
                new XAttribute("a2", 2),
                new XText("abcd"),
                10,
                new XElement("y", new XComment("comment")),
                new XElement("z"));

            Assert.Equal(5, e.DescendantNodesAndSelf().Count());
            Assert.Equal(2, e.Attributes().Count());

            e.RemoveAll();
            Assert.Equal(1, e.DescendantNodesAndSelf().Count());
            Assert.Empty(e.Attributes());

            // Removing all from an already empty one.
            e.RemoveAll();
            Assert.Equal(1, e.DescendantNodesAndSelf().Count());
            Assert.Empty(e.Attributes());
        }

        /// <summary>
        /// Validate enumeration of the SetElementValue method on element/
        /// </summary>
        [Fact]
        public void ElementSetElementValue()
        {
            XElement e1 = new XElement("x");

            // Removal of non-existent element
            e1.SetElementValue("foo", null);
            Assert.Empty(e1.Elements());

            // Add of non-existent element
            e1.SetElementValue("foo", "foo-value");
            Assert.Equal(new XElement[] { new XElement("foo", "foo-value") }, e1.Elements(), XNode.EqualityComparer);

            // Overwriting of existing element
            e1.SetElementValue("foo", "noo-value");
            Assert.Equal(new XElement[] { new XElement("foo", "noo-value") }, e1.Elements(), XNode.EqualityComparer);

            // Effective removal of existing element
            e1.SetElementValue("foo", null);
            Assert.Empty(e1.Elements());
        }

        /// <summary>
        /// Tests XElement.GetDefaultNamespace().
        /// </summary>
        [Fact]
        public void ElementGetDefaultNamespace()
        {
            XNamespace ns = XNamespace.Get("http://test");
            XElement e = new XElement(ns + "foo");

            XNamespace n = e.GetDefaultNamespace();
            Assert.NotNull(n);
            Assert.Equal(XNamespace.None, n);

            e.SetAttributeValue("xmlns", ns);
            n = e.GetDefaultNamespace();
            Assert.NotNull(n);
            Assert.Equal(ns, n);
        }

        /// <summary>
        /// Tests XElement.GetNamespaceOfPrefix().
        /// </summary>
        [Fact]
        public void ElementGetNamespaceOfPrefix()
        {
            XNamespace ns = XNamespace.Get("http://test");
            XElement e = new XElement(ns + "foo");

            Assert.Throws<ArgumentNullException>(() => e.GetNamespaceOfPrefix(null));
            AssertExtensions.Throws<ArgumentException>(null, () => e.GetNamespaceOfPrefix(string.Empty));

            XNamespace n = e.GetNamespaceOfPrefix("xmlns");
            Assert.Equal("http://www.w3.org/2000/xmlns/", n.NamespaceName);

            n = e.GetNamespaceOfPrefix("xml");
            Assert.Equal("http://www.w3.org/XML/1998/namespace", n.NamespaceName);

            n = e.GetNamespaceOfPrefix("myns");
            Assert.Null(n);

            XDocument doc = new XDocument(e);
            e.SetAttributeValue("{http://www.w3.org/2000/xmlns/}myns", ns);
            n = e.GetNamespaceOfPrefix("myns");
            Assert.NotNull(n);
            Assert.Equal(ns, n);
        }

        /// <summary>
        /// Tests XElement.GetPrefixOfNamespace().
        /// </summary>
        [Fact]
        public void ElementGetPrefixOfNamespace()
        {
            Assert.Throws<ArgumentNullException>(() => new XElement("foo").GetPrefixOfNamespace(null));

            XNamespace ns = XNamespace.Get("http://test");
            XElement e = new XElement(ns + "foo");

            string prefix = e.GetPrefixOfNamespace(ns);
            Assert.Null(prefix);

            prefix = e.GetPrefixOfNamespace(XNamespace.Xmlns);
            Assert.Equal("xmlns", prefix);

            prefix = e.GetPrefixOfNamespace(XNamespace.Xml);
            Assert.Equal("xml", prefix);

            XElement parent = new XElement("parent", e);
            parent.SetAttributeValue("{http://www.w3.org/2000/xmlns/}myns", ns);
            prefix = e.GetPrefixOfNamespace(ns);
            Assert.Equal("myns", prefix);

            e = XElement.Parse("<foo:element xmlns:foo='http://xxx'></foo:element>");
            prefix = e.GetPrefixOfNamespace("http://xxx");
            Assert.Equal("foo", prefix);

            e =
                XElement.Parse(
                    "<foo:element xmlns:foo='http://foo' xmlns:bar='http://bar'><bar:element /></foo:element>");
            prefix = e.GetPrefixOfNamespace("http://foo");
            Assert.Equal("foo", prefix);
            prefix = e.Element(XName.Get("{http://bar}element")).GetPrefixOfNamespace("http://foo");
            Assert.Equal("foo", prefix);
            prefix = e.Element(XName.Get("{http://bar}element")).GetPrefixOfNamespace("http://bar");
            Assert.Equal("bar", prefix);
        }

        /// <summary>
        /// Tests cases where we're exporting unqualified elements that have xmlns attributes.
        /// In this specific scenario we expect XmlExceptions because the element itself
        /// is written to an XmlWriter with the empty namespace, and then when the attribute
        /// is written to the XmlWriter an exception occurs because the xmlns attribute
        /// would cause a retroactive change to the namespace of the already-written element.
        /// That is not allowed -- the element must be qualified.
        /// </summary>
        [Fact]
        public void ElementWithXmlnsAttribute()
        {
            // And with just xmlns local name
            XElement element = new XElement("MyElement", new XAttribute("xmlns", "http://tempuri/test"));

            Assert.Throws<XmlException>(() => element.ToString());

            // A qualified element name works.
            element = new XElement("{http://tempuri/test}MyElement", new XAttribute("xmlns", "http://tempuri/test"));

            Assert.Equal("<MyElement xmlns=\"http://tempuri/test\" />", element.ToString());
        }

        /// <summary>
        /// Tests the Equals methods on XElement.
        /// </summary>
        [Fact]
        public void ElementEquality()
        {
            XElement e1 = XElement.Parse("<x/>");
            XElement e2 = XElement.Parse("<x/>");
            XElement e3 = XElement.Parse("<x a='a'/>");
            XElement e4 = XElement.Parse("<x>x</x>");
            XElement e5 = XElement.Parse("<y/>");

            // Internal method.
            Assert.True(XNode.DeepEquals(e1, e1));
            Assert.True(XNode.DeepEquals(e1, e2));
            Assert.False(XNode.DeepEquals(e1, e3));
            Assert.False(XNode.DeepEquals(e1, e4));
            Assert.False(XNode.DeepEquals(e1, e5));

            // object.Equals override
            Assert.True(e1.Equals(e1));
            Assert.False(e1.Equals(e2));
            Assert.False(e1.Equals(e3));
            Assert.False(e1.Equals(e4));
            Assert.False(e1.Equals(e5));

            Assert.False(e1.Equals(null));
            Assert.False(e1.Equals("foo"));

            // Hash codes. The most we can say is that identical elements
            // should have the same hash codes.
            XElement e1a = XElement.Parse("<x/>");
            XElement e1b = XElement.Parse("<x/>");

            XElement e2a = XElement.Parse("<x>abc</x>");
            XElement e2b = XElement.Parse("<x>abc</x>");

            XElement e3a = XElement.Parse("<x><y/></x>");
            XElement e3b = XElement.Parse("<x><y/></x>");

            XElement e4a = XElement.Parse("<x><y/><!--comment--></x>");
            XElement e4b = XElement.Parse("<x><!--comment--><y/></x>");

            XElement e5a = XElement.Parse("<x a='a'/>");
            XElement e5b = XElement.Parse("<x a='a'/>");

            int hash = XNode.EqualityComparer.GetHashCode(e1a);
            Assert.Equal(XNode.EqualityComparer.GetHashCode(e1b), hash);

            hash = XNode.EqualityComparer.GetHashCode(e2a);
            Assert.Equal(XNode.EqualityComparer.GetHashCode(e2b), hash);

            hash = XNode.EqualityComparer.GetHashCode(e3a);
            Assert.Equal(XNode.EqualityComparer.GetHashCode(e3b), hash);

            hash = XNode.EqualityComparer.GetHashCode(e4a);
            Assert.Equal(XNode.EqualityComparer.GetHashCode(e4b), hash);

            hash = XNode.EqualityComparer.GetHashCode(e5a);
            Assert.Equal(XNode.EqualityComparer.GetHashCode(e5b), hash);

            // Attribute comparison
            e1 = XElement.Parse("<x a='a' />");
            e2 = XElement.Parse("<x b='b' />");
            e3 = XElement.Parse("<x a='a' b='b' />");
            e4 = XElement.Parse("<x b='b' a='a' />");
            e5 = XElement.Parse("<x a='b' />");

            Assert.False(XNode.DeepEquals(e1, e2));
            Assert.False(XNode.DeepEquals(e1, e3));
            Assert.False(XNode.DeepEquals(e1, e4));
            Assert.False(XNode.DeepEquals(e1, e5));

            Assert.False(XNode.DeepEquals(e2, e3));
            Assert.False(XNode.DeepEquals(e2, e4));
            Assert.False(XNode.DeepEquals(e2, e5));

            Assert.False(XNode.DeepEquals(e3, e4));
            Assert.False(XNode.DeepEquals(e3, e5));

            Assert.False(XNode.DeepEquals(e4, e5));
        }

        /// <summary>
        /// Tests that an element appended as a child element during iteration of its new 
        /// parent's content is returned in iteration.
        /// </summary>
        [Fact]
        public void ElementAppendedChildIsIterated()
        {
            XElement parent = new XElement("element", new XElement("child1"), new XElement("child2"));

            bool b1 = false, b2 = false, b3 = false, b4 = false;

            foreach (XElement child in parent.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "child1":
                        b1 = true;
                        parent.Add(new XElement("extra1"));
                        break;
                    case "child2":
                        b2 = true;
                        parent.Add(new XElement("extra2"));
                        break;
                    case "extra1":
                        b3 = true;
                        break;
                    case "extra2":
                        b4 = true;
                        break;
                    default:
                        Assert.True(false, string.Format("Uexpected element '{0}'", child.Name));
                        break;
                }
            }

            Assert.True(b1 || b2 || b3 || b4, "Appended child elements not included in parent iteration");
        }
    }
}
