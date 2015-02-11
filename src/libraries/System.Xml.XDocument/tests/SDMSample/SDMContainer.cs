// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_Container : XLinqTestCase
            {
                /// <summary>
                /// Tests the Add methods on Container.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerAdd")]
                public void ContainerAdd()
                {
                    XElement element = new XElement("foo");

                    // Adding null does nothing.
                    element.Add(null);
                    Validate.Count(element.Nodes(), 0);

                    // Add node, attrbute, string, some other value, and an IEnumerable.
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

                    Validate.EnumeratorDeepEquals(
                        element.Nodes(),
                        new XNode[] { comment, new XText(str + other), comment2, comment3 });

                    Validate.EnumeratorAttributes(element.Attributes(), new XAttribute[] { attribute });

                    element.RemoveAll();
                    Validate.Count(element.Nodes(), 0);

                    // Now test params overload.
                    element.Add(comment, attribute, str, other);

                    Validate.EnumeratorDeepEquals(
                        element.Nodes(),
                        new XNode[] { comment, new XText(str + other) });

                    Validate.EnumeratorAttributes(element.Attributes(), new XAttribute[] { attribute });

                    // Not allowed to add a document as a child.
                    XDocument document = new XDocument();
                    try
                    {
                        element.Add(document);
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }
                }

                /// <summary>
                /// Tests the AddAttributes method on Container.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerAddAttributes")]
                public void ContainerAddAttributes()
                {
                    // Not allowed to add attributes in the general case.
                    // The only general case of a container is a document.
                    XDocument document = new XDocument();

                    try
                    {
                        document.Add(new XAttribute("foo", "bar"));
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }

                    // Can add to elements, but no duplicates allowed.
                    XElement e = new XElement("element");
                    XAttribute a1 = new XAttribute("foo", "bar1");
                    XAttribute a2 = new XAttribute("foo", "bar2");
                    e.Add(a1);

                    try
                    {
                        e.Add(a2);
                        Validate.ExpectedThrow(typeof(InvalidOperationException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(InvalidOperationException));
                    }

                    // Can add the same attribute to different parent elements;
                    // it gets copied.
                    XElement e2 = new XElement("element2");
                    e2.Add(a1);

                    if (!object.ReferenceEquals(a1, e.Attribute("foo")))
                    {
                        throw new TestFailedException(
                            "Attribute added to an element was unexpectedly copied");
                    }

                    if (object.ReferenceEquals(a1, e2.Attribute("foo")))
                    {
                        throw new TestFailedException(
                            "Attribute added to a second element was not copied");
                    }
                }

                /// <summary>
                /// Tests the AddFirst methods on Container.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerAddFirst")]
                public void ContainerAddFirst()
                {
                    XElement element = new XElement("foo");

                    // Adding null does nothing.
                    element.AddFirst(null);
                    Validate.Count(element.Nodes(), 0);

                    // Add a sentinal value.
                    XText text = new XText("abcd");
                    element.AddFirst(text);

                    // Add node and string.
                    XComment comment = new XComment("this is a comment");
                    string str = "this is a string";

                    element.AddFirst(comment);
                    element.AddFirst(str);

                    Validate.EnumeratorDeepEquals(element.Nodes(), new XNode[] { new XText(str), comment, text });

                    element.RemoveAll();
                    Validate.Count(element.Nodes(), 0);

                    // Now test params overload.
                    element.AddFirst(text);
                    element.AddFirst(comment, str);

                    Validate.EnumeratorDeepEquals(element.Nodes(), new XNode[] { comment, new XText(str), text });

                    // Can't use to add attributes.
                    XAttribute a = new XAttribute("foo", "bar");
                    try
                    {
                        element.AddFirst(a);
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }
                }

                /// <summary>
                /// Tests the Content/AllContent methods on Container
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerContent")]
                public void ContainerContent()
                {
                    XElement element =
                        new XElement("foo",
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

                    Validate.DeepEquals(obj1, new XComment("my comment"));
                    Validate.DeepEquals(obj2, new XElement("bar", new XText("abcd"), new XElement("inner")));
                    Validate.DeepEquals(obj3, new XText("100"));
                    Validate.IsEqual(b, false);
                }

                /// <summary>
                /// Validate enumeration of container descendents.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerDescendents")]
                public void ContainerDescendents()
                {
                    XComment comment = new XComment("comment");
                    XElement level3 = new XElement("Level3");
                    XElement level2 = new XElement("Level2", level3);
                    XElement level1 = new XElement("Level1", level2, comment);
                    XElement level0 = new XElement("Level1", level1);

                    Validate.EnumeratorDeepEquals(
                        level1.Descendants(),
                        new XElement[] { level2, level3 });

                    Validate.EnumeratorDeepEquals(
                        level0.DescendantNodes(),
                        new XNode[] { level1, level2, level3, comment });

                    Validate.EnumeratorDeepEquals(level0.Descendants(null), new XElement[0]);

                    Validate.EnumeratorDeepEquals(level0.Descendants("Level1"), new XElement[] { level1 });
                }

                /// <summary>
                /// Validate enumeration of container elements.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerElements")]
                public void ContainerElements()
                {
                    XElement level1_1 = new XElement("level1");

                    XElement level1_2 =
                        new XElement("level1",
                            new XElement("level1"),
                            new XElement("level2"));

                    XElement element =
                        new XElement("level0",
                            new XComment("my comment"),
                            level1_1,
                            level1_2
                            );

                    XElement empty = new XElement("empty");

                    // Can't find anything in an empty element
                    Validate.IsNull(empty.Element("foo"));

                    // Can't find element with no name or bogus name.
                    Validate.IsNull(element.Element(null));
                    Validate.IsNull(element.Element("foo"));

                    // Check element by name
                    Validate.IsEqual(element.Element("level1"), level1_1);

                    // Check element sequence -- should not include nested elements.
                    Validate.EnumeratorDeepEquals(element.Elements(), new XElement[] { level1_1, level1_2 });

                    // Check element sequence by name.
                    Validate.EnumeratorDeepEquals(element.Elements(null), new XElement[0]);
                    Validate.EnumeratorDeepEquals(element.Elements("level1"), new XElement[] { level1_1, level1_2 });
                }

                /// <summary>
                /// Validate ReplaceNodes on container.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerReplaceNodes")]
                public void ContainerReplaceNodes()
                {
                    XElement element =
                        new XElement("foo",
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

                    element.ReplaceNodes(
                        comment,
                        attribute,
                        str,
                        other1,
                        new XComment[] { comment2, comment3 });

                    Validate.EnumeratorDeepEquals(
                        element.Nodes(),
                        new XNode[]
                        {
                            comment,
                            new XText(str + XmlConvert.ToString(other1)),
                            comment2,
                            comment3
                        });

                    Validate.Count(element.Attributes(), 2);
                    Validate.AttributeNameAndValue(element.Attribute("att"), "att", "bar");
                    Validate.AttributeNameAndValue(element.Attribute("att2"), "att2", "att-value");
                }

                /// <summary>
                /// Validate the behavior of annotations on Container.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerAnnotations")]
                public void ContainerAnnotations()
                {
                    XElement element1 = new XElement("e1");
                    XElement element2 = new XElement("e2");

                    // Check argument null exception on add.
                    try
                    {
                        element1.AddAnnotation(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Before adding anything, should not be able to get any annotations.
                    Validate.IsNull(element1.Annotation(typeof(object)));
                    element1.RemoveAnnotations(typeof(object));
                    Validate.IsNull(element1.Annotation(typeof(object)));

                    // First annotation: 2 cases, object[] and other.
                    object obj1 = "hello";
                    element1.AddAnnotation(obj1);
                    Validate.IsNull(element1.Annotation(typeof(byte)));
                    Validate.IsReferenceEqual(element1.Annotation(typeof(string)), obj1);
                    element1.RemoveAnnotations(typeof(string));
                    Validate.IsNull(element1.Annotation(typeof(string)));

                    object[] obj2 = new object[] { 10, 20, 30 };

                    element2.AddAnnotation(obj2);
                    Validate.IsReferenceEqual(element2.Annotation(typeof(object[])), obj2);
                    Validate.Enumerator<object>((object[])element2.Annotation(typeof(object[])), new object[] { 10, 20, 30 });
                    element2.RemoveAnnotations(typeof(object[]));
                    Validate.IsNull(element2.Annotation(typeof(object[])));

                    // Single annotation; add a second one. Check that duplicates are allowed.
                    object obj3 = 10;
                    element1.AddAnnotation(obj3);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(int)), obj3);
                    element1.AddAnnotation(1000);
                    element1.RemoveAnnotations(typeof(int[]));
                    Validate.IsNull(element1.Annotation(typeof(object[])));

                    object obj4 = "world";
                    element1.AddAnnotation(obj4);

                    Validate.IsReferenceEqual(element1.Annotation(typeof(int)), obj3);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(string)), obj4);

                    // Multiple annotations already. Add one on the end.
                    object obj5 = 20L;
                    element1.AddAnnotation(obj5);

                    Validate.IsReferenceEqual(element1.Annotation(typeof(int)), obj3);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(string)), obj4);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(long)), obj5);

                    // Remove one from the middle and then add, which should use the
                    // freed slot.
                    element1.RemoveAnnotations(typeof(string));
                    Validate.IsNull(element1.Annotation(typeof(string)));

                    object obj6 = 30m;
                    element1.AddAnnotation(obj6);

                    Validate.IsReferenceEqual(element1.Annotation(typeof(int)), obj3);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(long)), obj5);
                    Validate.IsReferenceEqual(element1.Annotation(typeof(decimal)), obj6);

                    // Ensure that duplicates are allowed.           
                    element1.AddAnnotation(40m);
                    Validate.IsNull(element1.Annotation(typeof(sbyte)));

                    // A couple of additional remove cases.
                    element2.AddAnnotation(obj2);
                    element2.AddAnnotation(obj3);
                    element2.AddAnnotation(obj5);
                    element2.AddAnnotation(obj6);

                    element2.RemoveAnnotations(typeof(float));
                    Validate.IsNull(element2.Annotation(typeof(float)));
                }

                /// <summary>
                /// Tests removing text content from a container.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ContainerRemoveTextual")]
                public void ContainerRemoveTextual()
                {
                    XElement e1 = XElement.Parse("<a>abcd</a>");
                    XElement e2 = new XElement(e1);

                    XElement eb = new XElement("b");
                    e2.Add(eb);
                    eb.Remove();

                    Validate.IsEqual(XNode.EqualityComparer.Equals(e1, e2), true);

                    // Removing non-text between some text should NOT collapse the text.
                    e1.Add(eb);
                    e1.Add("efgh");

                    Validate.EnumeratorDeepEquals(e1.Nodes(), new XNode[] { new XText("abcd"), eb, new XText("efgh") });

                    eb.Remove();

                    Validate.EnumeratorDeepEquals(e1.Nodes(), new XNode[] { new XText("abcd"), new XText("efgh") });
                }
            }
        }
    }
}