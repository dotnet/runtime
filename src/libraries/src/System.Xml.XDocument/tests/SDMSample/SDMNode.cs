// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace XDocumentTests.SDMSample
{
    public class SDM_Node
    {
        /// <summary>
        /// Tests the Parent property on Node.
        /// </summary>
        [Fact]
        public void NodeParent()
        {
            // Only elements are returned as parents from the Parent property.
            // Documents are not returned.
            XDocument document = new XDocument();

            XNode[] nodes = new XNode[]
            {
                new XComment("comment"), new XElement("element"),
                new XProcessingInstruction("target", "data"),
                new XDocumentType("name", "publicid", "systemid", "internalsubset")
            };

            foreach (XNode node in nodes)
            {
                Assert.Null(node.Parent);
                document.Add(node);
                Assert.Same(document, node.Document);
                // Parent element is null.
                Assert.Null(node.Parent);
                document.RemoveNodes();
            }

            // Now test the cases where an element is the parent.
            nodes = new XNode[]
            {
                new XComment("abcd"),
                new XElement("nested"),
                new XProcessingInstruction("target2", "data2"),
                new XText("text")
            };

            XElement root = new XElement("root");
            document.ReplaceNodes(root);

            foreach (XNode node in nodes)
            {
                Assert.Null(node.Parent);

                root.AddFirst(node);

                Assert.Same(root, node.Parent);

                root.RemoveNodes();
                Assert.Null(node.Parent);
            }
        }

        /// <summary>
        /// Tests the ReadFrom static method on Node.
        /// </summary>
        [Fact]
        public void NodeReadFrom()
        {
            // Null reader not allowed.
            Assert.Throws<ArgumentNullException>(() => XNode.ReadFrom(null));

            // Valid cases: cdata, comment, element
            string[] rawXml = new string[] { "text", "<![CDATA[abcd]]>", "<!-- comment -->", "<y>y</y>" };
            Type[] types = new Type[] { typeof(XText), typeof(XCData), typeof(XComment), typeof(XElement) };

            int count = rawXml.Length;
            for (int i = 0; i < count; i++)
            {
                using (StringReader stringReader = new StringReader("<x>" + rawXml[i] + "</x>"))
                {
                    using (XmlReader reader = XmlReader.Create(stringReader))
                    {
                        reader.Read(); // skip to <x>
                        reader.Read(); // skip over <x> to the meat

                        XNode node = XNode.ReadFrom(reader);

                        // Ensure that the right kind of node got created.
                        Assert.IsType(types[i], node);

                        // Ensure that the value is right.
                        Assert.Equal(rawXml[i], node.ToString(SaveOptions.DisableFormatting));
                    }
                }
            }

            // Also test a case that is not allowed.
            using (StringReader stringReader = new StringReader("<x y='abcd'/>"))
            {
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    reader.Read();
                    reader.MoveToFirstAttribute();

                    Assert.Throws<InvalidOperationException>(() => XNode.ReadFrom(reader));
                }
            }
        }

        /// <summary>
        /// Tests the AddAfterSelf/AddBeforeSelf/Remove method on Node,
        /// when there's no parent.
        /// </summary>
        [Fact]
        public void NodeNoParentAddRemove()
        {
            // Not allowed if parent is null.
            int i = 0;
            while (true)
            {
                XNode node = null;

                switch (i++)
                {
                    case 0:
                        node = new XElement("x");
                        break;
                    case 1:
                        node = new XComment("c");
                        break;
                    case 2:
                        node = new XText("abc");
                        break;
                    case 3:
                        node = new XProcessingInstruction("target", "data");
                        break;
                    default:
                        i = -1;
                        break;
                }

                if (i < 0)
                {
                    break;
                }

                Assert.Throws<InvalidOperationException>(() => node.AddBeforeSelf("foo"));
                Assert.Throws<InvalidOperationException>(() => node.AddAfterSelf("foo"));
                Assert.Throws<InvalidOperationException>(() => node.Remove());
            }
        }

        /// <summary>
        /// Tests AddAfterSelf on Node.
        /// </summary>
        [Fact]
        public void NodeAddAfterSelf()
        {
            XElement parent = new XElement("parent");
            XElement child = new XElement("child");
            parent.Add(child);

            XText sibling1 = new XText("sibling1");
            XElement sibling2 = new XElement("sibling2");
            XComment sibling3 = new XComment("sibling3");

            child.AddAfterSelf(sibling1);

            Assert.Equal(new XNode[] { child, sibling1 }, parent.Nodes(), XNode.EqualityComparer);

            child.AddAfterSelf(sibling2, sibling3);

            Assert.Equal(new XNode[] { child, sibling2, sibling3, sibling1 }, parent.Nodes(), XNode.EqualityComparer);
        }

        /// <summary>
        /// Tests AddBeforeSelf on Node.
        /// </summary>
        [Fact]
        public void NodeAddBeforeSelf()
        {
            XElement parent = new XElement("parent");
            XElement child = new XElement("child");
            parent.Add(child);

            XElement sibling1 = new XElement("sibling1");
            XComment sibling2 = new XComment("sibling2");
            XText sibling3 = new XText("sibling3");

            child.AddBeforeSelf(sibling1);

            Assert.Equal(new XNode[] { sibling1, child }, parent.Nodes(), XNode.EqualityComparer);

            child.AddBeforeSelf(sibling2, sibling3);

            Assert.Equal(new XNode[] { sibling1, sibling2, sibling3, child }, parent.Nodes(), XNode.EqualityComparer);
        }

        /// <summary>
        /// Tests Remove on Node.
        /// </summary>
        [Fact]
        public void NodeRemove()
        {
            XElement parent = new XElement("parent");

            XComment child1 = new XComment("child1");
            XText child2 = new XText("child2");
            XElement child3 = new XElement("child3");

            parent.Add(child1, child2, child3);

            // Sanity check
            Assert.Equal(parent.Nodes(), new XNode[] { child1, child2, child3 }, XNode.EqualityComparer);

            // Remove the text.
            child1.NextNode.Remove();
            Assert.Equal(new XNode[] { child1, child3 }, parent.Nodes(), XNode.EqualityComparer);

            // Remove the XComment.
            child1.Remove();
            Assert.Equal(new XNode[] { child3 }, parent.Nodes(), XNode.EqualityComparer);

            // Remove the XElement.
            child3.Remove();
            Assert.Empty(parent.Nodes());
        }

        /// <summary>
        /// Tests the AllContentBeforeSelf method on Node.
        /// </summary>
        [Fact]
        public void NodeAllContentBeforeSelf()
        {
            XElement parent = new XElement("parent");

            XComment child = new XComment("Self is a comment");

            XComment comment1 = new XComment("Another comment");
            XComment comment2 = new XComment("Yet another comment");
            XElement element1 = new XElement("childelement", new XElement("nested"), new XAttribute("foo", "bar"));
            XElement element2 = new XElement("childelement2", new XElement("nested"), new XAttribute("foo", "bar"));
            XAttribute attribute = new XAttribute("attribute", "value");

            // If no parent, should not be any content before it.
            Assert.Empty(child.NodesBeforeSelf());

            // Add child to parent. Should still be no content before it.
            // Attributes are not content.
            parent.Add(attribute);
            parent.Add(child);
            Assert.Empty(child.NodesBeforeSelf());

            // Add more children and validate.
            parent.Add(comment1);
            parent.Add(element1);

            Assert.Empty(child.NodesBeforeSelf());

            parent.AddFirst(element2);
            parent.AddFirst(comment2);

            Assert.Equal(new XNode[] { comment2, element2 }, child.NodesBeforeSelf());
        }

        /// <summary>
        /// Tests the AllContentAfterSelf method on Node.
        /// </summary>
        [Fact]
        public void NodeAllContentAfterSelf()
        {
            XElement parent = new XElement("parent");

            XComment child = new XComment("Self is a comment");

            XComment comment1 = new XComment("Another comment");
            XComment comment2 = new XComment("Yet another comment");
            XElement element1 = new XElement("childelement", new XElement("nested"), new XAttribute("foo", "bar"));
            XElement element2 = new XElement("childelement2", new XElement("nested"), new XAttribute("foo", "bar"));
            XAttribute attribute = new XAttribute("attribute", "value");

            // If no parent, should not be any content after it.
            Assert.Empty(child.NodesAfterSelf());

            // Add child to parent. Should still be no content after it.
            // Attributes are not content.
            parent.Add(child);
            parent.Add(attribute);
            Assert.Empty(child.NodesAfterSelf());

            // Add more children and validate.
            parent.AddFirst(comment1);
            parent.AddFirst(element1);

            Assert.Empty(child.NodesAfterSelf());

            parent.Add(element2);
            parent.Add(comment2);

            Assert.Equal(child.NodesAfterSelf(), new XNode[] { element2, comment2 });
        }

        /// <summary>
        /// Tests the ContentBeforeSelf method on Node.
        /// </summary>
        [Fact]
        public void NodeContentBeforeSelf()
        {
            XElement parent = new XElement("parent");

            XComment child = new XComment("Self is a comment");

            XComment comment1 = new XComment("Another comment");
            XComment comment2 = new XComment("Yet another comment");
            XElement element1 = new XElement("childelement", new XElement("nested"), new XAttribute("foo", "bar"));
            XElement element2 = new XElement("childelement2", new XElement("nested"), new XAttribute("foo", "bar"));
            XAttribute attribute = new XAttribute("attribute", "value");

            // If no parent, should not be any content before it.
            Assert.Empty(child.NodesBeforeSelf());

            // Add some content, including the child, and validate.
            parent.Add(attribute);
            parent.Add(comment1);
            parent.Add(element1);

            parent.Add(child);

            parent.Add(comment2);
            parent.Add(element2);

            Assert.Equal(new XNode[] { comment1, element1 }, child.NodesBeforeSelf());
        }

        /// <summary>
        /// Tests the ContentAfterSelf method on Node.
        /// </summary>
        [Fact]
        public void NodeContentAfterSelf()
        {
            XElement parent = new XElement("parent");

            XComment child = new XComment("Self is a comment");

            XComment comment1 = new XComment("Another comment");
            XComment comment2 = new XComment("Yet another comment");
            XElement element1 = new XElement("childelement", new XElement("nested"), new XAttribute("foo", "bar"));
            XElement element2 = new XElement("childelement2", new XElement("nested"), new XAttribute("foo", "bar"));
            XAttribute attribute = new XAttribute("attribute", "value");

            // If no parent, should not be any content after it.
            Assert.Empty(child.NodesAfterSelf());

            // Add some content, including the child, and validate.
            parent.Add(attribute);
            parent.Add(comment1);
            parent.Add(element1);

            parent.Add(child);

            parent.Add(comment2);
            parent.Add(element2);

            Assert.Equal(child.NodesAfterSelf(), new XNode[] { comment2, element2 });
        }

        /// <summary>
        /// Tests the ElementsBeforeSelf methods on Node.
        /// </summary>
        [Fact]
        public void NodeElementsBeforeSelf()
        {
            XElement parent = new XElement("parent");

            XElement child1a = new XElement("child1", new XElement("nested"));
            XElement child1b = new XElement("child1", new XElement("nested"));
            XElement child2a = new XElement("child2", new XElement("nested"));
            XElement child2b = new XElement("child2", new XElement("nested"));

            XComment comment = new XComment("this is a comment");

            // If no parent, should not be any elements before it.
            Assert.Empty(comment.ElementsBeforeSelf());

            parent.Add(child1a);
            parent.Add(child1b);
            parent.Add(child2a);
            parent.Add(comment);
            parent.Add(child2b);

            Assert.Equal(new XElement[] { child1a, child1b, child2a }, comment.ElementsBeforeSelf());

            Assert.Equal(comment.ElementsBeforeSelf("child1"), new XElement[] { child1a, child1b });

            Assert.Equal(new XElement[] { child1a, child1b, child2a }, child2b.ElementsBeforeSelf());

            Assert.Equal(new XElement[] { child2a }, child2b.ElementsBeforeSelf("child2"));
        }

        /// <summary>
        /// Tests the ElementsAfterSelf methods on Node.
        /// </summary>
        [Fact]
        public void NodeElementsAfterSelf()
        {
            XElement parent = new XElement("parent");

            XElement child1a = new XElement("child1", new XElement("nested"));
            XElement child1b = new XElement("child1", new XElement("nested"));
            XElement child2a = new XElement("child2", new XElement("nested"));
            XElement child2b = new XElement("child2", new XElement("nested"));

            XComment comment = new XComment("this is a comment");

            // If no parent, should not be any elements before it.
            Assert.Empty(comment.ElementsAfterSelf());

            parent.Add(child1a);
            parent.Add(comment);
            parent.Add(child1b);
            parent.Add(child2a);
            parent.Add(child2b);

            Assert.Equal(new XElement[] { child1b, child2a, child2b }, comment.ElementsAfterSelf());

            Assert.Equal(new XElement[] { child1b }, comment.ElementsAfterSelf("child1"));

            Assert.Equal(new XElement[] { child1b }, child1a.ElementsAfterSelf("child1"));

            Assert.Empty(child2b.ElementsAfterSelf());
        }

        /// <summary>
        /// Tests the Document property on Node.
        /// </summary>
        [Fact]
        public void NodeDocument()
        {
            XDocument document = new XDocument();

            XNode[] topLevelNodes = new XNode[]
            {
                new XComment("comment"),
                new XElement("element"),
                new XProcessingInstruction("target", "data")
            };

            XNode[] nestedNodes = new XNode[]
            {
                new XText("abcd"),
                new XElement("nested"),
                new XProcessingInstruction("target2", "data2")
            };

            // Test top-level cases.
            foreach (XNode node in topLevelNodes)
            {
                Assert.Null(node.Document);
                document.Add(node);
                Assert.Same(document, node.Document);
                document.RemoveNodes();
            }

            // Test nested cases.
            XElement root = new XElement("root");
            document.Add(root);

            foreach (XNode node in nestedNodes)
            {
                Assert.Null(node.Document);
                root.Add(node);
                Assert.Same(document, root.Document);
                root.RemoveNodes();
            }
        }
    }
}
