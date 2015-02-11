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
            public partial class SDM_Element : XLinqTestCase
            {
                /// <summary>
                /// Validate behavior of XElement simple creation.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateElementSimple")]
                public void CreateElementSimple()
                {
                    const string ElementName = "Element";

                    XElement element;

                    // Test the constructor that takes only a name.
                    element = new XElement(ElementName);
                    Validate.String(element.Name.ToString(), ElementName);

                    try
                    {
                        element = new XElement((XName)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Validate behavior of XElement creation with content supplied.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateElementWithContent")]
                public void CreateElementWithContent()
                {
                    // Test the constructor that takes a name and some content.
                    XElement level2Element = new XElement("Level2", "TextValue");
                    XAttribute attribute = new XAttribute("Attribute", "AttributeValue");
                    XCData cdata = new XCData("abcdefgh");
                    string someValue = "text";

                    XElement element = new XElement("Level1", level2Element, cdata, someValue, attribute);

                    Validate.ElementName(element, "Level1");

                    Validate.EnumeratorDeepEquals(
                        element.Nodes(),
                        new XNode[] { level2Element, cdata, new XText(someValue) });

                    Validate.EnumeratorAttributes(element.Attributes(), new XAttribute[1] { attribute });
                }

                /// <summary>
                /// Validate behavior of XElement creation with copy constructor.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateElementCopy")]
                public void CreateElementCopy()
                {
                    // With attributes
                    XElement level2Element = new XElement("Level2", "TextValue");
                    XAttribute attribute = new XAttribute("Attribute", "AttributeValue");
                    XCData cdata = new XCData("abcdefgh");
                    string someValue = "text";

                    XElement element = new XElement("Level1", level2Element, cdata, someValue, attribute);

                    XElement elementCopy = new XElement(element);

                    Validate.ElementName(element, "Level1");

                    Validate.EnumeratorDeepEquals(
                        elementCopy.Nodes(),
                        new XNode[] { level2Element, cdata, new XText(someValue) });

                    Validate.EnumeratorAttributes(elementCopy.Attributes(), new XAttribute[1] { attribute });

                    // Without attributes
                    element = new XElement("Level1", level2Element, cdata, someValue);
                    elementCopy = new XElement(element);

                    Validate.ElementName(element, "Level1");

                    Validate.EnumeratorDeepEquals(
                        elementCopy.Nodes(),
                        new XNode[] { level2Element, cdata, new XText(someValue) });

                    Validate.EnumeratorAttributes(elementCopy.Attributes(), new XAttribute[0]);

                    // Hsh codes of equal elements shoukd be equal.
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(element), XNode.EqualityComparer.GetHashCode(elementCopy));

                    // Null element is not allowed.
                    try
                    {
                        XElement e = new XElement((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Validate behavior of XElement creation from an XmlReader.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "CreateElementFromReader")]
                public void CreateElementFromReader()
                {
                    string xml = "<Level1 a1='1' a2='2'><Level2><![CDATA[12345678]]>text</Level2></Level1>";
                    string xml2 = "<Level1 />";
                    string xml3 = "<x><?xml version='1.0' encoding='utf-8'?></x>";

                    // With attributes
                    TextReader textReader = new StringReader(xml);
                    XmlReader xmlReader = XmlReader.Create(textReader);
                    xmlReader.Read();

                    XElement element = (XElement)XNode.ReadFrom(xmlReader);

                    Validate.ElementName(element, "Level1");
                    Validate.ElementNames(element.Elements(), new string[] { "Level2" });

                    Validate.AttributeNames(element.Attributes(), new string[] { "a1", "a2" });
                    Validate.AttributeValues(element.Attributes(), new string[] { "1", "2" });

                    Validate.String(element.Element("Level2").Value, "12345678text");

                    xmlReader.Dispose();
                    textReader.Dispose();

                    // Without attributes
                    textReader = new StringReader(xml2);
                    xmlReader = XmlReader.Create(textReader);
                    xmlReader.Read();

                    element = (XElement)XNode.ReadFrom(xmlReader);

                    Validate.ElementName(element, "Level1");
                    Validate.ElementNames(element.Elements(), new string[0]);
                    Validate.AttributeNames(element.Attributes(), new string[0]);

                    Validate.String(element.Value, string.Empty);

                    xmlReader.Dispose();
                    textReader.Dispose();

                    // XmlReader in start state results in exception
                    textReader = new StringReader(xml);
                    xmlReader = XmlReader.Create(textReader);

                    try
                    {
                        element = (XElement)XNode.ReadFrom(xmlReader);
                        Validate.ExpectedThrow(typeof(InvalidOperationException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(InvalidOperationException));
                    }

                    xmlReader.Dispose();
                    textReader.Dispose();

                    // XmlReader not on an element results in exception.
                    textReader = new StringReader(xml);
                    xmlReader = XmlReader.Create(textReader);
                    xmlReader.Read();
                    xmlReader.MoveToAttribute("a1");

                    try
                    {
                        element = (XElement)XNode.ReadFrom(xmlReader);
                        Validate.ExpectedThrow(typeof(InvalidOperationException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(InvalidOperationException));
                    }

                    xmlReader.Dispose();
                    textReader.Dispose();

                    // Illegal xml triggers exception that is bubbled out.
                    textReader = new StringReader(xml3);
                    xmlReader = XmlReader.Create(textReader);
                    xmlReader.Read();

                    try
                    {
                        element = (XElement)XNode.ReadFrom(xmlReader);
                        Validate.ExpectedThrow(typeof(XmlException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(XmlException));
                    }

                    xmlReader.Dispose();
                    textReader.Dispose();
                }

                /// <summary>
                /// Validate behavior of XElement EmptySequence method.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementEmptyElementSequence")]
                public void ElementEmptyElementSequence()
                {
                    Validate.Enumerator(XElement.EmptySequence, new XElement[0]);
                    Validate.Enumerator(XElement.EmptySequence, new XElement[0]);
                }

                /// <summary>
                /// Validate behavior of XElement HasAttributes/HasElements properties.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementHasAttributesAndElements")]
                public void ElementHasAttributesAndElements()
                {
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", new XAttribute("a", "value"));
                    XElement e3 = new XElement("x", new XElement("y"));
                    XElement e4 = new XElement("x", new XCData("cdata-value"));

                    Validate.IsEqual(e1.HasAttributes, false);
                    Validate.IsEqual(e2.HasAttributes, true);

                    Validate.IsEqual(e1.HasElements, false);
                    Validate.IsEqual(e2.HasElements, false);
                    Validate.IsEqual(e3.HasElements, true);
                    Validate.IsEqual(e4.HasElements, false);
                }

                /// <summary>
                /// Validate behavior of the IsEmpty property.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementIsEmpty")]
                public void ElementIsEmpty()
                {
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", 10);
                    XElement e3 = new XElement("x", string.Empty);

                    Validate.IsEqual(e1.IsEmpty, true);
                    Validate.IsEqual(e2.IsEmpty, false);
                    Validate.IsEqual(e3.IsEmpty, false);
                }

                /// <summary>
                /// Validate behavior of the Value property on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementValue")]
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

                    Validate.String(e1.Value, string.Empty);
                    Validate.String(e2.Value, "value");
                    Validate.String(e3.Value, "100200");
                    Validate.String(e4.Value, "100value200");
                    Validate.String(e5.Value, string.Empty);
                    Validate.String(e6.Value, "15");
                    Validate.String(e7.Value, "inner1fooinner2");
                    Validate.String(e8.Value, "text1innertext2");
                    Validate.String(e9.Value, "text1abcdy");
                    Validate.String(e10.Value, string.Empty);

                    try
                    {
                        e1.Value = null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    e1.Value = string.Empty;
                    e2.Value = "not-empty";

                    Validate.String(e1.Value, string.Empty);
                    Validate.String(e2.Value, "not-empty");
                }

                /// <summary>
                /// Validates the explicit string conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToString")]
                public void ElementExplicitToString()
                {
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", string.Empty);
                    XElement e3 = new XElement("x", "value");

                    string s0 = (string)((XElement)null);
                    string s1 = (string)e1;
                    string s2 = (string)e2;
                    string s3 = (string)e3;

                    Validate.IsNull(s0);
                    Validate.String(s1, string.Empty);
                    Validate.String(s2, string.Empty);
                    Validate.String(s3, "value");
                }

                /// <summary>
                /// Validates the explicit boolean conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToBoolean")]
                public void ElementExplicitToBoolean()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        bool b = (bool)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "true");
                    XElement e4 = new XElement("x", "false");
                    XElement e5 = new XElement("x", "0");
                    XElement e6 = new XElement("x", "1");

                    try
                    {
                        bool b1 = (bool)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        bool b2 = (bool)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((bool)e3, true);
                    Validate.IsEqual((bool)e4, false);
                    Validate.IsEqual((bool)e5, false);
                    Validate.IsEqual((bool)e6, true);
                }

                /// <summary>
                /// Validates the explicit int32 conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToInt32")]
                public void ElementExplicitToInt32()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        int i = (int)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "2147483648");
                    XElement e4 = new XElement("x", "5");

                    try
                    {
                        int i1 = (int)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        int i2 = (int)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        int i3 = (int)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((int)e4, 5);
                }

                /// <summary>
                /// Validates the explicit uint32 conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToUInt32")]
                public void ElementExplicitToUInt32()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        uint i = (uint)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "4294967296");
                    XElement e4 = new XElement("x", "5");

                    try
                    {
                        uint i1 = (uint)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        uint i2 = (uint)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        uint i3 = (uint)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((uint)e4, 5u);
                }

                /// <summary>
                /// Validates the explicit int64 conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToInt64")]
                public void ElementExplicitToInt64()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        long i = (long)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "18446744073709551616");
                    XElement e4 = new XElement("x", "5");

                    try
                    {
                        long i1 = (long)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        long i2 = (long)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        long i3 = (long)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((long)e4, 5L);
                }

                /// <summary>
                /// Validates the explicit uint64 conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToUInt64")]
                public void ElementExplicitToUInt64()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        ulong i = (ulong)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "18446744073709551616");
                    XElement e4 = new XElement("x", "5");

                    try
                    {
                        ulong i1 = (ulong)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        ulong i2 = (ulong)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        ulong i3 = (ulong)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((ulong)e4, 5UL);
                }

                /// <summary>
                /// Validates the explicit float conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToFloat")]
                public void ElementExplicitToFloat()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        float f = (float)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "5e+500");
                    XElement e4 = new XElement("x", "5.0");

                    try
                    {
                        float f1 = (float)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        float f2 = (float)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        float i3 = (float)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((float)e4, 5.0f);
                }

                /// <summary>
                /// Validates the explicit double conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToDouble")]
                public void ElementExplicitToDouble()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        double f = (double)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "5e+5000");
                    XElement e4 = new XElement("x", "5.0");

                    try
                    {
                        double f1 = (double)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        double f2 = (double)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        double f3 = (double)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((double)e4, 5.0);
                }

                /// <summary>
                /// Validates the explicit decimal conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToDecimal")]
                public void ElementExplicitToDecimal()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        decimal d = (decimal)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "111111111111111111111111111111111111111111111111");
                    XElement e4 = new XElement("x", "5.0");

                    try
                    {
                        decimal d1 = (decimal)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        decimal d2 = (decimal)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        decimal d3 = (decimal)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((decimal)e4, 5m);
                }

                /// <summary>
                /// Validates the explicit DateTime conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToDateTime")]
                public void ElementExplicitToDateTime()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        DateTime d = (DateTime)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "1968-01-07");

                    try
                    {
                        DateTime d1 = (DateTime)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        DateTime d2 = (DateTime)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((DateTime)e3, new DateTime(1968, 1, 7));
                }

                /// <summary>
                /// Validates the explicit TimeSpan conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToTimeSpan")]
                public void ElementExplicitToTimeSpan()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        TimeSpan d = (TimeSpan)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", "PT1H2M3S");

                    try
                    {
                        TimeSpan d1 = (TimeSpan)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        TimeSpan d2 = (TimeSpan)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((TimeSpan)e3, new TimeSpan(1, 2, 3));
                }

                /// <summary>
                /// Validates the explicit guid conversion operator on XElement.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToGuId")]
                public void ElementExplicitToGuid()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        Guid g = (Guid)((XElement)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    string guid = "2b67e9fb-97ad-4258-8590-8bc8c2d32df5";

                    // Test various values.
                    XElement e1 = new XElement("x");
                    XElement e2 = new XElement("x", "bogus");
                    XElement e3 = new XElement("x", guid);

                    try
                    {
                        Guid g1 = (Guid)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        Guid g2 = (Guid)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((Guid)e3, new Guid(guid));
                }

                /// <summary>
                /// Validates the explicit conversion operators on XElement
                /// for nullable value types.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "ElementExplicitToNullables")]
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

                    Validate.IsEqual(b.Value, true);
                    Validate.IsEqual(i.Value, 5);
                    Validate.IsEqual(u.Value, 5u);
                    Validate.IsEqual(l.Value, 5L);
                    Validate.IsEqual(ul.Value, 5uL);
                    Validate.IsEqual(f.Value, 5.0f);
                    Validate.IsEqual(n.Value, 5.0);
                    Validate.IsEqual(d.Value, 5.0m);
                    Validate.IsEqual(dt.Value, new DateTime(1968, 1, 7));
                    Validate.IsEqual(ts.Value, new TimeSpan(1, 2, 3));
                    Validate.IsEqual(g.Value, new Guid(guid));

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

                    Validate.IsNull(b);
                    Validate.IsNull(i);
                    Validate.IsNull(u);
                    Validate.IsNull(l);
                    Validate.IsNull(ul);
                    Validate.IsNull(f);
                    Validate.IsNull(n);
                    Validate.IsNull(d);
                    Validate.IsNull(dt);
                    Validate.IsNull(ts);
                    Validate.IsNull(g);
                }

                /// <summary>
                /// Validate enumeration of element ancestors.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementAncestors")]
                public void ElementAncestors()
                {
                    XElement level3 = new XElement("Level3");
                    XElement level2 = new XElement("Level2", level3);
                    XElement level1 = new XElement("Level1", level2);
                    XElement level0 = new XElement("Level1", level1);

                    Validate.EnumeratorDeepEquals(level3.Ancestors(), new XElement[] { level2, level1, level0 });

                    Validate.EnumeratorDeepEquals(level3.Ancestors("Level1"), new XElement[] { level1, level0 });

                    Validate.EnumeratorDeepEquals(level3.Ancestors(null), new XElement[0]);

                    Validate.EnumeratorDeepEquals(level3.AncestorsAndSelf(), new XElement[] { level3, level2, level1, level0 });

                    Validate.EnumeratorDeepEquals(level3.AncestorsAndSelf("Level3"), new XElement[] { level3 });

                    Validate.EnumeratorDeepEquals(level3.AncestorsAndSelf(null), new XElement[0]);
                }

                /// <summary>
                /// Validate enumeration of element descendents.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementDescendents")]
                public void ElementDescendents()
                {
                    XComment comment = new XComment("comment");
                    XElement level3 = new XElement("Level3");
                    XElement level2 = new XElement("Level2", level3);
                    XElement level1 = new XElement("Level1", level2, comment);
                    XElement level0 = new XElement("Level1", level1);

                    Validate.EnumeratorDeepEquals(
                        level1.DescendantsAndSelf(),
                        new XElement[] { level1, level2, level3 });

                    Validate.EnumeratorDeepEquals(
                        level0.DescendantNodesAndSelf(),
                        new XNode[] { level0, level1, level2, level3, comment });

                    Validate.EnumeratorDeepEquals(
                        level0.DescendantsAndSelf(null),
                        new XElement[0]);

                    Validate.EnumeratorDeepEquals(
                        level0.DescendantsAndSelf("Level1"),
                        new XElement[] { level0, level1 });
                }

                /// <summary>
                /// Validate enumeration of element attributes.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementAttributes")]
                public void ElementAttributes()
                {
                    XElement e1 = new XElement("x");

                    XElement e2 = new XElement("x",
                        new XAttribute("a1", "1"),
                        new XAttribute("a2", "2"),
                        new XAttribute("a3", "3"),
                        new XAttribute("a4", "4"),
                        new XAttribute("a5", "5"));

                    XElement e3 = new XElement("x",
                        new XAttribute("a1", "1"),
                        new XAttribute("a2", "2"),
                        new XAttribute("a3", "3"));

                    Validate.IsNull(e1.Attribute("foo"));
                    Validate.IsNull(e2.Attribute("foo"));

                    Validate.AttributeNameAndValue(e2.Attribute("a3"), "a3", "3");

                    Validate.AttributeNames(e2.Attributes(), new string[] { "a1", "a2", "a3", "a4", "a5" });
                    Validate.AttributeValues(e2.Attributes(), new string[] { "1", "2", "3", "4", "5" });

                    Validate.AttributeNames(e2.Attributes("a1"), new string[] { "a1" });
                    Validate.AttributeValues(e2.Attributes("a5"), new string[] { "5" });

                    Validate.EnumeratorAttributes(e2.Attributes(null), new XAttribute[0]);

                    e2.RemoveAttributes();
                    Validate.EnumeratorAttributes(e2.Attributes(), new XAttribute[0]);

                    // Removal of non-existant attribute
                    e1.SetAttributeValue("foo", null);
                    Validate.Count(e1.Attributes(), 0);

                    // Add of non-existant attribute
                    e1.SetAttributeValue("foo", "foo-value");
                    Validate.AttributeNameAndValue(e1.Attribute("foo"), "foo", "foo-value");

                    // Overwriting of existing attribute
                    e1.SetAttributeValue("foo", "noo-value");
                    Validate.AttributeNameAndValue(e1.Attribute("foo"), "foo", "noo-value");

                    // Effective removal of existing attribute
                    e1.SetAttributeValue("foo", null);
                    Validate.Count(e1.Attributes(), 0);

                    // These 3 are in a specific order to exercise the attribute removal code.
                    e3.SetAttributeValue("a2", null);
                    Validate.Count(e3.Attributes(), 2);

                    e3.SetAttributeValue("a3", null);
                    Validate.Count(e3.Attributes(), 1);

                    e3.SetAttributeValue("a1", null);
                    Validate.Count(e3.Attributes(), 0);
                }

                /// <summary>
                /// Validates remove methods on elements.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementRemove")]
                public void ElementRemove()
                {
                    XElement e =
                        new XElement("x",
                            new XAttribute("a1", 1),
                            new XAttribute("a2", 2),
                            new XText("abcd"),
                            10,
                            new XElement("y",
                                new XComment("comment")),
                            new XElement("z"));

                    Validate.Count(e.DescendantNodesAndSelf(), 5);
                    Validate.Count(e.Attributes(), 2);

                    e.RemoveAll();
                    Validate.Count(e.DescendantNodesAndSelf(), 1);
                    Validate.Count(e.Attributes(), 0);

                    // Removing all from an already empty one.
                    e.RemoveAll();
                    Validate.Count(e.DescendantNodesAndSelf(), 1);
                    Validate.Count(e.Attributes(), 0);
                }

                /// <summary>
                /// Validate enumeration of the SetElementValue method on element/
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementSetElementValue")]
                public void ElementSetElementValue()
                {
                    XElement e1 = new XElement("x");

                    // Removal of non-existant element
                    e1.SetElementValue("foo", null);
                    Validate.Count(e1.Elements(), 0);

                    // Add of non-existant element
                    e1.SetElementValue("foo", "foo-value");
                    Validate.EnumeratorDeepEquals(
                        e1.Elements(),
                        new XElement[] { new XElement("foo", "foo-value") });

                    // Overwriting of existing element
                    e1.SetElementValue("foo", "noo-value");
                    Validate.EnumeratorDeepEquals(
                        e1.Elements(),
                        new XElement[] { new XElement("foo", "noo-value") });

                    // Effective removal of existing element
                    e1.SetElementValue("foo", null);
                    Validate.Count(e1.Elements(), 0);
                }

                /// <summary>
                /// Tests XElement.GetDefaultNamespace().
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementGetDefaultNamespace")]
                public void ElementGetDefaultNamespace()
                {
                    XNamespace ns = XNamespace.Get("http://test");
                    XElement e = new XElement(ns + "foo");

                    XNamespace n = e.GetDefaultNamespace();
                    Validate.IsEqual(n, XNamespace.None);

                    e.SetAttributeValue("xmlns", ns);
                    n = e.GetDefaultNamespace();
                    Validate.IsEqual(n, ns);
                }

                /// <summary>
                /// Tests XElement.GetNamespaceOfPrefix().
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementGetNamespaceOfPrefix")]
                public void ElementGetNamespaceOfPrefix()
                {
                    XNamespace ns = XNamespace.Get("http://test");
                    XElement e = new XElement(ns + "foo");

                    try
                    {
                        e.GetNamespaceOfPrefix(null);
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        e.GetNamespaceOfPrefix(string.Empty);
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }

                    XNamespace n = e.GetNamespaceOfPrefix("xmlns");
                    Validate.String(n.NamespaceName, "http://www.w3.org/2000/xmlns/");

                    n = e.GetNamespaceOfPrefix("xml");
                    Validate.String(n.NamespaceName, "http://www.w3.org/XML/1998/namespace");

                    n = e.GetNamespaceOfPrefix("myns");
                    Validate.IsNull(n);

                    XDocument doc = new XDocument(e);
                    e.SetAttributeValue("{http://www.w3.org/2000/xmlns/}myns", ns);
                    n = e.GetNamespaceOfPrefix("myns");

                    Validate.IsEqual(n, ns);
                }

                /// <summary>
                /// Tests XElement.GetPrefixOfNamespace().
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementGetPrefixOfNamespace")]
                public void ElementGetPrefixOfNamespace()
                {
                    try
                    {
                        new XElement("foo").GetPrefixOfNamespace(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    XNamespace ns = XNamespace.Get("http://test");
                    XElement e = new XElement(ns + "foo");

                    string prefix = e.GetPrefixOfNamespace(ns);
                    Validate.IsNull(prefix);

                    prefix = e.GetPrefixOfNamespace(XNamespace.Xmlns);
                    Validate.String(prefix, "xmlns");

                    prefix = e.GetPrefixOfNamespace(XNamespace.Xml);
                    Validate.String(prefix, "xml");

                    XElement parent = new XElement("parent", e);
                    parent.SetAttributeValue("{http://www.w3.org/2000/xmlns/}myns", ns);
                    prefix = e.GetPrefixOfNamespace(ns);
                    Validate.String(prefix, "myns");

                    e = XElement.Parse("<foo:element xmlns:foo='http://xxx'></foo:element>");
                    prefix = e.GetPrefixOfNamespace("http://xxx");
                    Validate.String(prefix, "foo");

                    e = XElement.Parse("<foo:element xmlns:foo='http://foo' xmlns:bar='http://bar'><bar:element /></foo:element>");
                    prefix = e.GetPrefixOfNamespace("http://foo");
                    Validate.String(prefix, "foo");
                    prefix = e.Element(XName.Get("{http://bar}element")).GetPrefixOfNamespace("http://foo");
                    Validate.String(prefix, "foo");
                    prefix = e.Element(XName.Get("{http://bar}element")).GetPrefixOfNamespace("http://bar");
                    Validate.String(prefix, "bar");
                }

                /// <summary>
                /// Tests cases where we're exporting unqualified elements that have xmlns attributes.
                /// In this specific scenario we expect XmlExceptions because the element itself
                /// is written to an XmlWriter with the empty namespace, and then when the attribute
                /// is written to the XmlWriter an exception occurs because the xmlns attribute
                /// would cause a retroactive change to the namespace of the already-written element.
                /// That is not allowed -- the element must be qualified.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementWithXmlnsAttribute")]
                public void ElementWithXmlnsAttribute()
                {
                    // And with just xmlns local name
                    XElement element = new XElement(
                        "MyElement",
                        new XAttribute("xmlns", "http://tempuri/test"));

                    try
                    {
                        element.ToString();
                        Validate.ExpectedThrow(typeof(XmlException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(XmlException));
                    }

                    // A qualified element name works.
                    element = new XElement(
                        "{http://tempuri/test}MyElement",
                        new XAttribute("xmlns", "http://tempuri/test"));

                    string s = element.ToString();
                    Validate.String(s, "<MyElement xmlns=\"http://tempuri/test\" />");
                }

                /// <summary>
                /// Tests the Equals methods on XElement.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementEquality")]
                public void ElementEquality()
                {
                    XElement e1 = XElement.Parse("<x/>");
                    XElement e2 = XElement.Parse("<x/>");
                    XElement e3 = XElement.Parse("<x a='a'/>");
                    XElement e4 = XElement.Parse("<x>x</x>");
                    XElement e5 = XElement.Parse("<y/>");

                    // Internal method.
                    bool b1 = XNode.DeepEquals(e1, e1);
                    bool b2 = XNode.DeepEquals(e1, e2);
                    bool b3 = XNode.DeepEquals(e1, e3);
                    bool b4 = XNode.DeepEquals(e1, e4);
                    bool b5 = XNode.DeepEquals(e1, e5);

                    Validate.IsEqual(b1, true);
                    Validate.IsEqual(b2, true);
                    Validate.IsEqual(b3, false);
                    Validate.IsEqual(b4, false);
                    Validate.IsEqual(b5, false);

                    // object.Equals override
                    b1 = e1.Equals(e1);
                    b2 = e1.Equals(e2);
                    b3 = e1.Equals(e3);
                    b4 = e1.Equals(e4);
                    b5 = e1.Equals(e5);

                    bool bx = e1.Equals(null);
                    bool by = e1.Equals("foo");

                    Validate.IsEqual(b1, true);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, false);
                    Validate.IsEqual(b4, false);
                    Validate.IsEqual(b5, false);
                    Validate.IsEqual(bx, false);
                    Validate.IsEqual(by, false);

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
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(e1b), hash);

                    hash = XNode.EqualityComparer.GetHashCode(e2a);
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(e2b), hash);

                    hash = XNode.EqualityComparer.GetHashCode(e3a);
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(e3b), hash);

                    hash = XNode.EqualityComparer.GetHashCode(e4a);
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(e4b), hash);

                    hash = XNode.EqualityComparer.GetHashCode(e5a);
                    Validate.IsEqual(XNode.EqualityComparer.GetHashCode(e5b), hash);

                    // Attribute comparison
                    e1 = XElement.Parse("<x a='a' />");
                    e2 = XElement.Parse("<x b='b' />");
                    e3 = XElement.Parse("<x a='a' b='b' />");
                    e4 = XElement.Parse("<x b='b' a='a' />");
                    e5 = XElement.Parse("<x a='b' />");

                    b1 = XNode.DeepEquals(e1, e2);
                    b2 = XNode.DeepEquals(e1, e3);
                    b3 = XNode.DeepEquals(e1, e4);
                    b4 = XNode.DeepEquals(e1, e5);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, false);
                    Validate.IsEqual(b4, false);

                    b1 = XNode.DeepEquals(e2, e3);
                    b2 = XNode.DeepEquals(e2, e4);
                    b3 = XNode.DeepEquals(e2, e5);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, false);

                    b1 = XNode.DeepEquals(e3, e4);
                    b2 = XNode.DeepEquals(e3, e5);

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, false);

                    b1 = XNode.DeepEquals(e4, e5);
                    Validate.IsEqual(b1, false);
                }

                /// <summary>
                /// Tests that an element appended as a child element during iteration of its new 
                /// parent's content is returned in iteration.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "ElementAppendedChildIsIterated")]
                public void ElementAppendedChildIsIterated()
                {
                    XElement parent = new XElement("element",
                        new XElement("child1"),
                        new XElement("child2"));

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
                                throw new TestFailedException(string.Format(
                                    "Uexpected element '{0}'", child.Name));
                        }
                    }

                    if (!b1 || !b2 || !b3 || !b4)
                    {
                        throw new TestFailedException(
                            "Appended child elements not included in parent iteration");
                    }
                }
            }
        }
    }
}
