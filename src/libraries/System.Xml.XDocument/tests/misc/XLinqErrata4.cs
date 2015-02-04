// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using System.Collections;
using System.Collections.Generic;
using XmlCoreTest.Common;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public partial class XmlErrata4 : XLinqTestCase
            {
                // Invalid charcters in names
                //[Variation(Desc = "XName with InValid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XElement" })]
                //[Variation(Desc = "XName with InValid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XElement" })]
                //[Variation(Desc = "XName with InValid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XElement" })]
                //[Variation(Desc = "XName with InValid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XElement" })]
                //[Variation(Desc = "XName with InValid NCName Characters", Params = new object[] { "InValid", "NCNameChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid NCName Characters", Params = new object[] { "InValid", "NCNameChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid NCName Characters", Params = new object[] { "InValid", "NCNameChar", "XElement" })]
                //[Variation(Desc = "XName with InValid NCName Start Characters", Params = new object[] { "InValid", "NCNameStartChar", "XName" })]
                //[Variation(Desc = "XAttribute with InValid NCName Start Characters", Params = new object[] { "InValid", "NCNameStartChar", "XAttribute" })]
                //[Variation(Desc = "XElement with InValid NCName Start Characters", Params = new object[] { "InValid", "NCNameStartChar", "XElement" })]
                // Valid characters in names
                //[Variation(Desc = "XName with Valid NCName Characters", Params = new object[] { "Valid", "NCNameChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid NCName Characters", Params = new object[] { "Valid", "NCNameChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid NCName Characters", Params = new object[] { "Valid", "NCNameChar", "XElement" })]
                //[Variation(Desc = "XName with Valid NCName Start Characters", Params = new object[] { "Valid", "NCNameStartChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid NCName Start Characters", Params = new object[] { "Valid", "NCNameStartChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid NCName Start Characters", Params = new object[] { "Valid", "NCNameStartChar", "XElement" })]
                // This variation runs through about 1000 iterations for each of the above variations
                public void varation1()
                {
                    string testType = Variation.Params[0] as string;
                    string charType = Variation.Params[1] as string;
                    string nodeType = Variation.Params[2] as string;

                    int iterations = 0;
                    foreach (char c in GetRandomCharacters(testType, charType))
                    {
                        string name = GetName(charType, c);
                        if (testType.Equals("Valid"))
                            RunValidTests(nodeType, name);
                        else if (testType.Equals("InValid"))
                            RunInValidTests(nodeType, name);
                        iterations++;
                    }
                    TestLog.WriteLine("{0}, iterations executed", iterations);
                }

                // Get valid(Fifth Edition) surrogate characters but since surrogates are not supported in Fourth Edition Xml we still expect an exception.
                //[Variation(Desc = "XName with Valid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid Name Surrogate Low Characters", Params = new object[] { "InValid", "NameSurrogateLowChar", "XElement" })]
                //[Variation(Desc = "XName with Valid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid Name Surrogate High Characters", Params = new object[] { "InValid", "NameSurrogateHighChar", "XElement" })]
                //[Variation(Desc = "XName with Valid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid NameStart Surrogate Low Characters", Params = new object[] { "InValid", "NameStartSurrogateLowChar", "XElement" })]
                //[Variation(Desc = "XName with Valid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XName" })]
                //[Variation(Desc = "XAttribute with Valid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XAttribute" })]
                //[Variation(Desc = "XElement with Valid NameStart Surrogate High Characters", Params = new object[] { "InValid", "NameStartSurrogateHighChar", "XElement" })]
                public void varation2()
                {
                    string testType = Variation.Params[0] as string;
                    string charType = Variation.Params[1] as string;
                    string nodeType = Variation.Params[2] as string;

                    int iterations = 0;
                    foreach (char c in GetRandomCharacters("Valid", charType))
                    {
                        string name = GetName(charType, c);
                        RunInValidTests(nodeType, name);
                        iterations++;
                    }
                    TestLog.WriteLine("{0}, iterations executed", iterations);
                }

                //[Variation(Desc = "Xml Version Number Change Test")]
                public void varation3()
                {
                    string xml = @"<?xml version='1.9999'?>"
                        + "<!-- an implausibly-versioned document -->"
                        + "<foo/>";
                    try
                    {
                        XDeclaration xDec = new XDeclaration("1.99999", "utf-16", "false");
                        XDocument xDoc = XDocument.Load(XmlReader.Create(new StringReader(xml)));
                        xDoc.Declaration = xDec;
                        xDoc.Save(new MemoryStream());//"Save.Xml");
                    }
                    catch (XmlException e)
                    {
                        TestLog.WriteLine(e.ToString());
                        return;
                    }
                    throw new TestFailedException("Verion number '1.99999' is invalid exception should have been thrown");
                }

                /// <summary>
                /// Returns a set of random characters depending on the input type.
                /// </summary>
                /// <param name="testType">Valid or InValid</param>
                /// <param name="charType">type from CharType class</param>
                /// <returns>IEnumerable of characters</returns>
                public IEnumerable GetRandomCharacters(string testType, string charType)
                {
                    string chars = string.Empty;
                    if (testType.Equals("Valid"))
                        chars = GetValidCharacters(charType);
                    else if (testType.Equals("InValid"))
                        chars = GetInValidCharacters(charType);

                    int count = chars.Length;
                    int step = count < 1000 ? 1 : count / 1000;
                    for (int index = 0; index < count - step; index += step)
                    {
                        Random random = new Random(unchecked((int)(DateTime.Now.Ticks)));
                        int select = random.Next(index, index + step);
                        yield return chars[select];
                    }
                }

                /// <summary>
                /// Returns a string of valid characters
                /// </summary>
                /// <param name="charType">type form CharType class</param>
                /// <returns>string of characters</returns>
                public string GetValidCharacters(string charType)
                {
                    string chars = string.Empty;
                    switch (charType)
                    {
                        case "NCNameStartChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NCNameStartChar);
                            break;
                        case "NCNameChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NCNameChar);
                            break;
                        case "NameStartSurrogateHighChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NameStartSurrogateHighChar);
                            break;
                        case "NameStartSurrogateLowChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NameStartSurrogateLowChar);
                            break;
                        case "NameSurrogateHighChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NameSurrogateHighChar);
                            break;
                        case "NameSurrogateLowChar":
                            chars = UnicodeCharHelper.GetValidCharacters(CharType.NameSurrogateLowChar);
                            break;
                        default:
                            break;
                    }
                    return chars;
                }

                /// <summary>
                /// Returns a string of InValid characters
                /// </summary>
                /// <param name="charType">type form CharType class</param>
                /// <returns>string of characters</returns>
                public string GetInValidCharacters(string charType)
                {
                    string chars = string.Empty;
                    switch (charType)
                    {
                        case "NCNameStartChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NCNameStartChar);
                            break;
                        case "NCNameChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NCNameChar);
                            break;
                        case "NameStartSurrogateHighChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NameStartSurrogateHighChar);
                            break;
                        case "NameStartSurrogateLowChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NameStartSurrogateLowChar);
                            break;
                        case "NameSurrogateHighChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NameSurrogateHighChar);
                            break;
                        case "NameSurrogateLowChar":
                            chars = UnicodeCharHelper.GetInvalidCharacters(CharType.NameSurrogateLowChar);
                            break;
                        default:
                            break;
                    }
                    return chars;
                }


                /// <summary>
                /// Runs test for valid cases
                /// </summary>
                /// <param name="nodeType">XElement/XAttribute</param>
                /// <param name="name">name to be tested</param>
                public void RunValidTests(string nodeType, string name)
                {
                    XDocument xDocument = new XDocument();
                    XElement element = null;
                    try
                    {
                        switch (nodeType)
                        {
                            case "XElement":
                                element = new XElement(name, name);
                                xDocument.Add(element);
                                IEnumerable<XNode> nodeList = xDocument.Nodes();
                                TestLog.Compare(nodeList.Count(), 1, "Failed to create element { " + name + " }");
                                xDocument.RemoveNodes();
                                break;
                            case "XAttribute":
                                element = new XElement(name, name);
                                XAttribute attribute = new XAttribute(name, name);
                                element.Add(attribute);
                                xDocument.Add(element);
                                XAttribute x = element.Attribute(name);
                                TestLog.Compare(x.Name.LocalName, name, "Failed to get the added attribute");
                                xDocument.RemoveNodes();
                                break;
                            case "XName":
                                XName xName = XName.Get(name, name);
                                TestLog.Compare(xName.LocalName.Equals(name), "Invalid LocalName");
                                TestLog.Compare(xName.NamespaceName.Equals(name), "Invalid Namespace Name");
                                TestLog.Compare(xName.Namespace.NamespaceName.Equals(name), "Invalid Namespace Name");
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        TestLog.WriteLine(nodeType + "failed to create with a valid name { " + name + " }");
                        throw new TestFailedException(e.ToString());
                    }
                }

                /// <summary>
                /// Runs test for InValid cases
                /// </summary>
                /// <param name="nodeType">XElement/XAttribute</param>
                /// <param name="name">name to be tested</param>
                public void RunInValidTests(string nodeType, string name)
                {
                    XDocument xDocument = new XDocument();
                    XElement element = null;
                    try
                    {
                        switch (nodeType)
                        {
                            case "XElement":
                                element = new XElement(name, name);
                                xDocument.Add(element);
                                IEnumerable<XNode> nodeList = xDocument.Nodes();
                                break;
                            case "XAttribute":
                                element = new XElement(name, name);
                                XAttribute attribute = new XAttribute(name, name);
                                element.Add(attribute);
                                xDocument.Add(element);
                                XAttribute x = element.Attribute(name);
                                break;
                            case "XName":
                                XName xName = XName.Get(name, name);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (XmlException)
                    {
                        return;
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }

                    // If it gets here then test has failed.
                    throw new TestFailedException(nodeType + "was created with an Invalid name { " + name + " }");
                }

                /// <summary>
                /// returns a name using the character provided in the appropriate postion
                /// </summary>
                /// <param name="charType">type from CharType class</param>
                /// <param name="c">character to be used in the name</param>
                /// <returns>name with the character</returns>
                public string GetName(string charType, char c)
                {
                    string name = string.Empty;
                    switch (charType)
                    {
                        case "NCNameStartChar":
                            name = new string(new char[] { c, 'l', 'e', 'm', 'e', 'n', 't' });
                            break;
                        case "NCNameChar":
                            name = new string(new char[] { 'e', 'l', 'e', 'm', 'e', 'n', c });
                            break;
                        case "NameStartSurrogateHighChar":
                            name = new string(new char[] { c, '\udc00', 'e', 'm', 'e', 'n', 't' });
                            break;
                        case "NameStartSurrogateLowChar":
                            name = new string(new char[] { '\udb7f', c, 'e', 'm', 'e', 'n', 't' });
                            break;
                        case "NameSurrogateHighChar":
                            name = new string(new char[] { 'e', 'l', 'e', 'm', 'e', c, '\udfff' });
                            break;
                        case "NameSurrogateLowChar":
                            name = new string(new char[] { 'e', 'l', 'e', 'm', 'e', '\ud800', c });
                            break;
                        default:
                            break;
                    }
                    return name;
                }
            }
        }
    }
}
