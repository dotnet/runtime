// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_Container
    {
        /// <summary>
        /// Tests the Add methods on Container.
        /// </summary>
        [Fact]
        public void ContainerAdd()
        {
            XElement element = new XElement("foo");

            // Adding null does nothing.
            element.Add(null);
            Assert.Empty(element.Nodes());

            // Add node, attribute, string, some other value, and an IEnumerable.
            XComment comment = new XComment("this is a comment");
            XComment comment2 = new XComment("this is a comment 2");
            XComment comment3 = new XComment("this is a comment 3");
            XAttribute attribute = new XAttribute("att", "att-value");
            string str = "this is a string";
            int other = 7;

            element.Add(comment);
            element.Add(attribute);
            element.Add(str);
            element.Add(other);
            element.Add(new XComment[] { comment2, comment3 });

            Assert.Equal(
                new XNode[] { comment, new XText(str + other), comment2, comment3 },
                element.Nodes(),
                XNode.EqualityComparer);

            Assert.Equal(new[] { attribute.Name }, element.Attributes().Select(x => x.Name));
            Assert.Equal(new[] { attribute.Value }, element.Attributes().Select(x => x.Value));

            element.RemoveAll();
            Assert.Empty(element.Nodes());

            // Now test params overload.
            element.Add(comment, attribute, str, other);

            Assert.Equal(new XNode[] { comment, new XText(str + other) }, element.Nodes(), XNode.EqualityComparer);

            Assert.Equal(new[] { attribute.Name }, element.Attributes().Select(x => x.Name));
            Assert.Equal(new[] { attribute.Value }, element.Attributes().Select(x => x.Value));

            // Not allowed to add a document as a child.
            XDocument document = new XDocument();
            AssertExtensions.Throws<ArgumentException>(null, () => element.Add(document));
        }

        /// <summary>
        /// Tests the AddAttributes method on Container.
        /// </summary>
        [Fact]
        public void ContainerAddAttributes()
        {
            // Not allowed to add attributes in the general case.
            // The only general case of a container is a document.
            XDocument document = new XDocument();
            AssertExtensions.Throws<ArgumentException>(null, () => document.Add(new XAttribute("foo", "bar")));

            // Can add to elements, but no duplicates allowed.
            XElement e = new XElement("element");
            XAttribute a1 = new XAttribute("foo", "bar1");
            XAttribute a2 = new XAttribute("foo", "bar2");
            e.Add(a1);

            Assert.Throws<InvalidOperationException>(() => e.Add(a2));

            // Can add the same attribute to different parent elements;
            // it gets copied.
            XElement e2 = new XElement("element2");
            e2.Add(a1);

            Assert.Same(a1, e.Attribute("foo"));
            Assert.NotSame(a1, e2.Attribute("foo"));
        }

        /// <summary>
        /// Tests the AddFirst methods on Container.
        /// </summary>
        [Fact]
        public void ContainerAddFirst()
        {
            XElement element = new XElement("foo");

            // Adding null does nothing.
            element.AddFirst(null);
            Assert.Empty(element.Nodes());

            // Add a sentinel value.
            XText text = new XText("abcd");
            element.AddFirst(text);

            // Add node and string.
            XComment comment = new XComment("this is a comment");
            string str = "this is a string";

            element.AddFirst(comment);
            element.AddFirst(str);

            Assert.Equal(new XNode[] { new XText(str), comment, text }, element.Nodes(), XNode.EqualityComparer);

            element.RemoveAll();
            Assert.Empty(element.Nodes());

            // Now test params overload.
            element.AddFirst(text);
            element.AddFirst(comment, str);

            Assert.Equal(new XNode[] { comment, new XText(str), text }, element.Nodes(), XNode.EqualityComparer);

            // Can't use to add attributes.
            XAttribute a = new XAttribute("foo", "bar");
            AssertExtensions.Throws<ArgumentException>(null, () => element.AddFirst(a));
        }

        /// <summary>
        /// Tests the Content/AllContent methods on Container
        /// </summary>
        [Fact]
        public void ContainerContent()
        {
            XElement element = new XElement(
                "foo",
                new XAttribute("att1", "a1"),
                new XComment("my comment"),
                new XElement("bar", new XText("abcd"), new XElement("inner")),
                100);

            // Content should include just the elements, no attributes
            // or contents of nested elements.
            IEnumerator allContent = element.Nodes().GetEnumerator();
            allContent.MoveNext();
            object obj1 = allContent.Current;
            allContent.MoveNext();
            object obj2 = allContent.Current;
            allContent.MoveNext();
            object obj3 = allContent.Current;
            bool b = allContent.MoveNext();

            Assert.Equal((XNode)obj1, new XComment("my comment"), XNode.EqualityComparer);
            Assert.Equal(
                (XNode)obj2,
                new XElement("bar", new XText("abcd"), new XElement("inner")),
                XNode.EqualityComparer);
            Assert.Equal((XNode)obj3, new XText("100"), XNode.EqualityComparer);
            Assert.False(b);
        }

        /// <summary>
        /// Validate enumeration of container descendents.
        /// </summary>
        [Fact]
        public void ContainerDescendents()
        {
            XComment comment = new XComment("comment");
            XElement level3 = new XElement("Level3");
            XElement level2 = new XElement("Level2", level3);
            XElement level1 = new XElement("Level1", level2, comment);
            XElement level0 = new XElement("Level1", level1);

            Assert.Equal(new XElement[] { level2, level3 }, level1.Descendants(), XNode.EqualityComparer);

            Assert.Equal(
                new XNode[] { level1, level2, level3, comment },
                level0.DescendantNodes(),
                XNode.EqualityComparer);

            Assert.Empty(level0.Descendants(null));

            Assert.Equal(new XElement[] { level1 }, level0.Descendants("Level1"), XNode.EqualityComparer);
        }

        /// <summary>
        /// Validate enumeration of container elements.
        /// </summary>
        [Fact]
        public void ContainerElements()
        {
            XElement level1_1 = new XElement("level1");

            XElement level1_2 = new XElement("level1", new XElement("level1"), new XElement("level2"));

            XElement element = new XElement("level0", new XComment("my comment"), level1_1, level1_2);

            XElement empty = new XElement("empty");

            // Can't find anything in an empty element
            Assert.Null(empty.Element("foo"));

            // Can't find element with no name or bogus name.
            Assert.Null(element.Element(null));
            Assert.Null(element.Element("foo"));

            // Check element by name
            Assert.Equal(level1_1, element.Element("level1"));

            // Check element sequence -- should not include nested elements.
            Assert.Equal(new XElement[] { level1_1, level1_2 }, element.Elements(), XNode.EqualityComparer);

            // Check element sequence by name.
            Assert.Empty(element.Elements(null));
            Assert.Equal(new XElement[] { level1_1, level1_2 }, element.Elements("level1"), XNode.EqualityComparer);
        }

        /// <summary>
        /// Validate ReplaceNodes on container.
        /// </summary>
        [Fact]
        public void ContainerReplaceNodes()
        {
            XElement element = new XElement(
                "foo",
                new XAttribute("att", "bar"),
                "abc",
                new XElement("nested", new XText("abcd")));

            // Replace with a node, attribute, string, some other value, and an IEnumerable.
            // ReplaceNodes does not remove attributes.
            XComment comment = new XComment("this is a comment");
            XComment comment2 = new XComment("this is a comment 2");
            XComment comment3 = new XComment("this is a comment 3");
            XAttribute attribute = new XAttribute("att2", "att-value");
            string str = "this is a string";

            TimeSpan other1 = new TimeSpan(1, 2, 3);

            element.ReplaceNodes(comment, attribute, str, other1, new XComment[] { comment2, comment3 });

            Assert.Equal(
                new XNode[] { comment, new XText(str + XmlConvert.ToString(other1)), comment2, comment3 },
                element.Nodes(),
                XNode.EqualityComparer);

            Assert.Equal(2, element.Attributes().Count());

            Assert.Equal(element.Attribute("att").Name, "att");
            Assert.Equal(element.Attribute("att").Value, "bar");

            Assert.Equal(element.Attribute("att2").Name, "att2");
            Assert.Equal(element.Attribute("att2").Value, "att-value");
        }

        /// <summary>
        /// Validate the behavior of annotations on Container.
        /// </summary>
        [Fact]
        public void ContainerAnnotations()
        {
            XElement element1 = new XElement("e1");
            XElement element2 = new XElement("e2");

            // Check argument null exception on add.
            Assert.Throws<ArgumentNullException>(() => element1.AddAnnotation(null));

            // Before adding anything, should not be able to get any annotations.
            Assert.Null(element1.Annotation(typeof(object)));
            element1.RemoveAnnotations(typeof(object));
            Assert.Null(element1.Annotation(typeof(object)));

            // First annotation: 2 cases, object[] and other.
            object obj1 = "hello";
            element1.AddAnnotation(obj1);
            Assert.Null(element1.Annotation(typeof(byte)));
            Assert.Same(obj1, element1.Annotation(typeof(string)));
            element1.RemoveAnnotations(typeof(string));
            Assert.Null(element1.Annotation(typeof(string)));

            object[] obj2 = new object[] { 10, 20, 30 };

            element2.AddAnnotation(obj2);
            Assert.Same(obj2, element2.Annotation(typeof(object[])));
            Assert.Equal(element2.Annotation(typeof(object[])), new object[] { 10, 20, 30 });
            element2.RemoveAnnotations(typeof(object[]));
            Assert.Null(element2.Annotation(typeof(object[])));

            // Single annotation; add a second one. Check that duplicates are allowed.
            object obj3 = 10;
            element1.AddAnnotation(obj3);
            Assert.Same(obj3, element1.Annotation(typeof(int)));
            element1.AddAnnotation(1000);
            element1.RemoveAnnotations(typeof(int[]));
            Assert.Null(element1.Annotation(typeof(object[])));

            object obj4 = "world";
            element1.AddAnnotation(obj4);

            Assert.Same(obj3, element1.Annotation(typeof(int)));
            Assert.Same(obj4, element1.Annotation(typeof(string)));

            // Multiple annotations already. Add one on the end.
            object obj5 = 20L;
            element1.AddAnnotation(obj5);

            Assert.Same(obj3, element1.Annotation(typeof(int)));
            Assert.Same(obj4, element1.Annotation(typeof(string)));
            Assert.Same(obj5, element1.Annotation(typeof(long)));

            // Remove one from the middle and then add, which should use the
            // freed slot.
            element1.RemoveAnnotations(typeof(string));
            Assert.Null(element1.Annotation(typeof(string)));

            object obj6 = 30m;
            element1.AddAnnotation(obj6);

            Assert.Same(obj3, element1.Annotation(typeof(int)));
            Assert.Same(obj5, element1.Annotation(typeof(long)));
            Assert.Same(obj6, element1.Annotation(typeof(decimal)));

            // Ensure that duplicates are allowed.           
            element1.AddAnnotation(40m);
            Assert.Null(element1.Annotation(typeof(sbyte)));

            // A couple of additional remove cases.
            element2.AddAnnotation(obj2);
            element2.AddAnnotation(obj3);
            element2.AddAnnotation(obj5);
            element2.AddAnnotation(obj6);

            element2.RemoveAnnotations(typeof(float));
            Assert.Null(element2.Annotation(typeof(float)));
        }

        /// <summary>
        /// Tests removing text content from a container.
        /// </summary>
        [Fact]
        public void ContainerRemoveTextual()
        {
            XElement e1 = XElement.Parse("<a>abcd</a>");
            XElement e2 = new XElement(e1);

            XElement eb = new XElement("b");
            e2.Add(eb);
            eb.Remove();

            Assert.True(XNode.EqualityComparer.Equals(e1, e2));

            // Removing non-text between some text should NOT collapse the text.
            e1.Add(eb);
            e1.Add("efgh");

            Assert.Equal(new XNode[] { new XText("abcd"), eb, new XText("efgh") }, e1.Nodes(), XNode.EqualityComparer);

            eb.Remove();

            Assert.Equal(new XNode[] { new XText("abcd"), new XText("efgh") }, e1.Nodes(), XNode.EqualityComparer);
        }
    }
}
