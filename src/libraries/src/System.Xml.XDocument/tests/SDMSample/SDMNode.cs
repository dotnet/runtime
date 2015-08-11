// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_Node : XLinqTestCase
            {
                /// <summary>
                /// Tests the Parent property on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeParent")]
                public void NodeParent()
                {
                    // Only elements are returned as parents from the Parent property.
                    // Documents are not returned.
                    XDocument document = new XDocument();

                    XNode[] nodes = new XNode[]
                        {
                            new XComment("comment"),
                            new XElement("element"),
                            new XProcessingInstruction("target", "data"),
                            new XDocumentType("name", "publicid", "systemid", "internalsubset")
                        };

                    foreach (XNode node in nodes)
                    {
                        Validate.IsNull(node.Parent);
                        document.Add(node);
                        Validate.IsReferenceEqual(document, node.Document);
                        // Parent element is null.
                        Validate.IsNull(node.Parent);
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
                        Validate.IsNull(node.Parent);

                        root.AddFirst(node);

                        Validate.IsReferenceEqual(node.Parent, root);

                        root.RemoveNodes();
                        Validate.IsNull(node.Parent);
                    }
                }

                /// <summary>
                /// Tests the ReadFrom static method on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeReadFrom")]
                public void NodeReadFrom()
                {
                    // Null reader not allowed.
                    try
                    {
                        XNode.ReadFrom(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

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
                                reader.Read();  // skip to <x>
                                reader.Read();  // skip over <x> to the meat

                                XNode node = XNode.ReadFrom(reader);

                                // Ensure that the right kind of node got created.
                                Validate.Type(node, types[i]);

                                // Ensure that the value is right.
                                Validate.IsEqual(rawXml[i], node.ToString(SaveOptions.DisableFormatting));
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

                            try
                            {
                                XNode node = XNode.ReadFrom(reader);
                                Validate.ExpectedThrow(typeof(InvalidOperationException));
                            }
                            catch (Exception ex)
                            {
                                Validate.Catch(ex, typeof(InvalidOperationException));
                            }
                        }
                    }
                }

                /// <summary>
                /// Tests the AddAfterSelf/AddBeforeSelf/Remove method on Node,
                /// when there's no parent.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeNoParentAddRemove")]
                public void NodeNoParentAddRemove()
                {
                    // Not allowed if parent is null.
                    int i = 0;
                    while (true)
                    {
                        XNode node = null;

                        switch (i++)
                        {
                            case 0: node = new XElement("x"); break;
                            case 1: node = new XComment("c"); break;
                            case 2: node = new XText("abc"); break;
                            case 3: node = new XProcessingInstruction("target", "data"); break;
                            default: i = -1; break;
                        }

                        if (i < 0)
                        {
                            break;
                        }

                        try
                        {
                            node.AddBeforeSelf("foo");
                            Validate.ExpectedThrow(typeof(InvalidOperationException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(InvalidOperationException));
                        }

                        try
                        {
                            node.AddAfterSelf("foo");
                            Validate.ExpectedThrow(typeof(InvalidOperationException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(InvalidOperationException));
                        }

                        try
                        {
                            node.Remove();
                            Validate.ExpectedThrow(typeof(InvalidOperationException));
                        }
                        catch (Exception ex)
                        {
                            Validate.Catch(ex, typeof(InvalidOperationException));
                        }
                    }
                }

                /// <summary>
                /// Tests AddAfterSelf on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeAddAfterSelf")]
                public void NodeAddAfterSelf()
                {
                    XElement parent = new XElement("parent");
                    XElement child = new XElement("child");
                    parent.Add(child);

                    XText sibling1 = new XText("sibling1");
                    XElement sibling2 = new XElement("sibling2");
                    XComment sibling3 = new XComment("sibling3");

                    child.AddAfterSelf(sibling1);

                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { child, sibling1 });

                    child.AddAfterSelf(sibling2, sibling3);

                    Validate.EnumeratorDeepEquals(
                        parent.Nodes(),
                        new XNode[] { child, sibling2, sibling3, sibling1 });
                }

                /// <summary>
                /// Tests AddBeforeSelf on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeAddBeforeSelf")]
                public void NodeAddBeforeSelf()
                {
                    XElement parent = new XElement("parent");
                    XElement child = new XElement("child");
                    parent.Add(child);

                    XElement sibling1 = new XElement("sibling1");
                    XComment sibling2 = new XComment("sibling2");
                    XText sibling3 = new XText("sibling3");

                    child.AddBeforeSelf(sibling1);

                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { sibling1, child });

                    child.AddBeforeSelf(sibling2, sibling3);

                    Validate.EnumeratorDeepEquals(
                        parent.Nodes(),
                        new XNode[] { sibling1, sibling2, sibling3, child });
                }

                /// <summary>
                /// Tests Remove on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeRemove")]
                public void NodeRemove()
                {
                    XElement parent = new XElement("parent");

                    XComment child1 = new XComment("child1");
                    XText child2 = new XText("child2");
                    XElement child3 = new XElement("child3");

                    parent.Add(child1, child2, child3);

                    // Sanity check
                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { child1, child2, child3 });

                    // Remove the text.
                    child1.NextNode.Remove();
                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { child1, child3 });

                    // Remove the XComment.
                    child1.Remove();
                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { child3 });

                    // Remove the XElement.
                    child3.Remove();
                    Validate.EnumeratorDeepEquals(parent.Nodes(), new XNode[] { });
                }

                /// <summary>
                /// Tests the AllContentBeforeSelf method on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeAllContentBeforeSelf")]
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
                    Validate.Enumerator<XNode>(child.NodesBeforeSelf(), new XNode[0]);

                    // Add child to parent. Should still be no content before it.
                    // Attributes are not content.
                    parent.Add(attribute);
                    parent.Add(child);
                    Validate.Enumerator<XNode>(child.NodesBeforeSelf(), new XNode[0]);

                    // Add more children and validate.
                    parent.Add(comment1);
                    parent.Add(element1);

                    Validate.Enumerator<XNode>(child.NodesBeforeSelf(), new XNode[0]);

                    parent.AddFirst(element2);
                    parent.AddFirst(comment2);

                    Validate.Enumerator<XNode>(child.NodesBeforeSelf(), new XNode[] { comment2, element2 });
                }

                /// <summary>
                /// Tests the AllContentAfterSelf method on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeAllContentAfterSelf")]
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
                    Validate.Enumerator<XNode>(child.NodesAfterSelf(), new XNode[0]);

                    // Add child to parent. Should still be no content after it.
                    // Attributes are not content.
                    parent.Add(child);
                    parent.Add(attribute);
                    Validate.Enumerator<XNode>(child.NodesAfterSelf(), new XNode[0]);

                    // Add more children and validate.
                    parent.AddFirst(comment1);
                    parent.AddFirst(element1);

                    Validate.Enumerator<XNode>(child.NodesAfterSelf(), new XNode[0]);

                    parent.Add(element2);
                    parent.Add(comment2);

                    Validate.Enumerator<XNode>(child.NodesAfterSelf(), new XNode[] { element2, comment2 });
                }

                /// <summary>
                /// Tests the ContentBeforeSelf method on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeContentBeforeSelf")]
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
                    Validate.Enumerator(child.NodesBeforeSelf(), new XNode[0]);

                    // Add some content, including the child, and validate.
                    parent.Add(attribute);
                    parent.Add(comment1);
                    parent.Add(element1);

                    parent.Add(child);

                    parent.Add(comment2);
                    parent.Add(element2);

                    Validate.Enumerator(
                        child.NodesBeforeSelf(),
                        new XNode[] { comment1, element1 });
                }

                /// <summary>
                /// Tests the ContentAfterSelf method on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeContentAfterSelf")]
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
                    Validate.Enumerator(child.NodesAfterSelf(), new XNode[0]);

                    // Add some content, including the child, and validate.
                    parent.Add(attribute);
                    parent.Add(comment1);
                    parent.Add(element1);

                    parent.Add(child);

                    parent.Add(comment2);
                    parent.Add(element2);

                    Validate.Enumerator(child.NodesAfterSelf(), new XNode[] { comment2, element2 });
                }

                /// <summary>
                /// Tests the ElementsBeforeSelf methods on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeElementsBeforeSelf")]
                public void NodeElementsBeforeSelf()
                {
                    XElement parent = new XElement("parent");

                    XElement child1a = new XElement("child1", new XElement("nested"));
                    XElement child1b = new XElement("child1", new XElement("nested"));
                    XElement child2a = new XElement("child2", new XElement("nested"));
                    XElement child2b = new XElement("child2", new XElement("nested"));

                    XComment comment = new XComment("this is a comment");

                    // If no parent, should not be any elements before it.
                    Validate.Enumerator(comment.ElementsBeforeSelf(), new XElement[0]);

                    parent.Add(child1a);
                    parent.Add(child1b);
                    parent.Add(child2a);
                    parent.Add(comment);
                    parent.Add(child2b);

                    Validate.Enumerator(
                        comment.ElementsBeforeSelf(),
                        new XElement[] { child1a, child1b, child2a });

                    Validate.Enumerator(
                        comment.ElementsBeforeSelf("child1"),
                        new XElement[] { child1a, child1b });

                    Validate.Enumerator(
                        child2b.ElementsBeforeSelf(),
                        new XElement[] { child1a, child1b, child2a });

                    Validate.Enumerator(
                        child2b.ElementsBeforeSelf("child2"),
                        new XElement[] { child2a });
                }

                /// <summary>
                /// Tests the ElementsAfterSelf methods on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeElementsAfterSelf")]
                public void NodeElementsAfterSelf()
                {
                    XElement parent = new XElement("parent");

                    XElement child1a = new XElement("child1", new XElement("nested"));
                    XElement child1b = new XElement("child1", new XElement("nested"));
                    XElement child2a = new XElement("child2", new XElement("nested"));
                    XElement child2b = new XElement("child2", new XElement("nested"));

                    XComment comment = new XComment("this is a comment");

                    // If no parent, should not be any elements before it.
                    Validate.Enumerator(comment.ElementsAfterSelf(), new XElement[0]);

                    parent.Add(child1a);
                    parent.Add(comment);
                    parent.Add(child1b);
                    parent.Add(child2a);
                    parent.Add(child2b);

                    Validate.Enumerator(
                        comment.ElementsAfterSelf(),
                        new XElement[] { child1b, child2a, child2b });

                    Validate.Enumerator(
                        comment.ElementsAfterSelf("child1"),
                        new XElement[] { child1b });

                    Validate.Enumerator(
                        child1a.ElementsAfterSelf("child1"),
                        new XElement[] { child1b });

                    Validate.Enumerator(child2b.ElementsAfterSelf(), new XElement[0]);
                }

                /// <summary>
                /// Tests the Document property on Node.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "NodeDocument")]
                public void NodeDocument()
                {
                    XDocument document = new XDocument();

                    XNode[] topLevelNodes = new XNode[]
                        {
                            new XComment("comment"),
                            new XElement("element"),
                            new XProcessingInstruction("target", "data"),
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
                        Validate.IsNull(node.Document);
                        document.Add(node);
                        Validate.IsReferenceEqual(document, node.Document);
                        document.RemoveNodes();
                    }

                    // Test nested cases.
                    XElement root = new XElement("root");
                    document.Add(root);

                    foreach (XNode node in nestedNodes)
                    {
                        Validate.IsNull(node.Document);
                        root.Add(node);
                        Validate.IsReferenceEqual(document, root.Document);
                        root.RemoveNodes();
                    }
                }

                /// <summary>
                /// Gets the full path of the RuntimeTestXml directory, allowing for the
                /// possibility of being run from the VS build (bin\debug, etc).
                /// </summary>
                /// <returns></returns>
                internal string GetTestXmlDirectory()
                {
                    const string TestXmlDirectoryName = "RuntimeTestXml";
                    string baseDir = Path.Combine(XLinqTestCase.RootPath, "TestData", "XLinq");
                    string dir = Path.Combine(baseDir, TestXmlDirectoryName);

                    return Path.Combine(baseDir, "..", "..", TestXmlDirectoryName);
                }

                /// <summary>
                /// Gets the filenames of all files in the RuntimeTestXml directory.
                /// We use all files we find in a directory with a known name and location.
                /// </summary>
                /// <returns></returns>
                internal string[] GetTestXmlFilenames()
                {
                    string[] filenames = new string[] { };
                    return filenames;
                }

                /// <summary>
                /// Gets the filenames of all files in the RuntimeTestXml directory.
                /// We use all files we find in a directory with a known name and location.
                /// </summary>
                /// <returns></returns>
                internal string GetTestXmlFilename(string filename)
                {
                    string directory = GetTestXmlDirectory();
                    string fullName = Path.Combine(directory, filename);

                    return fullName;
                }
            }
        }
    }
}