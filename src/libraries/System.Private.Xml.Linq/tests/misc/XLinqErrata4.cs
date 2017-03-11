// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using XmlCoreTest.Common;

namespace System.Xml.Linq.Tests
{
    public class XmlErrata4
    {
        // Invalid characters in names
        [InlineData("InValid", "NameSurrogateLowChar", "XName")] // XName with InValid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateLowChar", "XAttribute")] // XAttribute with InValid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateLowChar", "XElement")] // XElement with InValid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XName")] // XName with InValid Name Surrogate High Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XAttribute")] // XAttribute with InValid Name Surrogate High Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XElement")] // XElement with InValid Name Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XName")] // XName with InValid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XAttribute")] // XAttribute with InValid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XElement")] // XElement with InValid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XName")] // XName with InValid NameStart Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XAttribute")] // XAttribute with InValid NameStart Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XElement")] // XElement with InValid NameStart Surrogate High Characters
        [InlineData("InValid", "NCNameChar", "XName")] // XName with InValid NCName Characters
        [InlineData("InValid", "NCNameChar", "XAttribute")] // XAttribute with InValid NCName Characters
        [InlineData("InValid", "NCNameChar", "XElement")] // XElement with InValid NCName Characters
        [InlineData("InValid", "NCNameStartChar", "XName")] // XName with InValid NCName Start Characters
        [InlineData("InValid", "NCNameStartChar", "XAttribute")] // XAttribute with InValid NCName Start Characters
        [InlineData("InValid", "NCNameStartChar", "XElement")] // XElement with InValid NCName Start Characters
        // Valid characters in names
        [InlineData("Valid", "NCNameChar", "XName")] // XName with Valid NCName Characters
        [InlineData("Valid", "NCNameChar", "XAttribute")] // XAttribute with Valid NCName Characters
        [InlineData("Valid", "NCNameChar", "XElement")] // XElement with Valid NCName Characters
        [InlineData("Valid", "NCNameStartChar", "XName")] // XName with Valid NCName Start Characters
        [InlineData("Valid", "NCNameStartChar", "XAttribute")] // XAttribute with Valid NCName Start Characters
        [InlineData("Valid", "NCNameStartChar", "XElement")] // XElement with Valid NCName Start Characters
        // This variation runs through about 1000 iterations for each of the above variations
        [Theory]
        public void varation1(string testType, string charType, string nodeType)
        {
            int iterations = 0;
            foreach (char c in GetRandomCharacters(testType, charType))
            {
                string name = GetName(charType, c);
                if (testType.Equals("Valid")) RunValidTests(nodeType, name);
                else if (testType.Equals("InValid")) RunInValidTests(nodeType, name);
                iterations++;
            }
        }

        // Get valid(Fifth Edition) surrogate characters but since surrogates are not supported in Fourth Edition Xml we still expect an exception.
        [InlineData("InValid", "NameSurrogateLowChar", "XName")] // XName with Valid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateLowChar", "XAttribute")] // XAttribute with Valid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateLowChar", "XElement")] // XElement with Valid Name Surrogate Low Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XName")] // XName with Valid Name Surrogate High Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XAttribute")] // XAttribute with Valid Name Surrogate High Characters
        [InlineData("InValid", "NameSurrogateHighChar", "XElement")] // XElement with Valid Name Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XName")] // XName with Valid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XAttribute")] // XAttribute with Valid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateLowChar", "XElement")] // XElement with Valid NameStart Surrogate Low Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XName")] // XName with Valid NameStart Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XAttribute")] // XAttribute with Valid NameStart Surrogate High Characters
        [InlineData("InValid", "NameStartSurrogateHighChar", "XElement")] // XElement with Valid NameStart Surrogate High Characters
        [Theory]
        public void varation2(string testType, string charType, string nodeType)
        {
            int iterations = 0;
            foreach (char c in GetRandomCharacters("Valid", charType))
            {
                string name = GetName(charType, c);
                RunInValidTests(nodeType, name);
                iterations++;
            }
        }

        [Fact]
        public void varation3()
        {
            string xml = @"<?xml version='1.9999'?>" + "<!-- an implausibly-versioned document -->" + "<foo/>";

            Assert.Throws<XmlException>(() => XDocument.Load(XmlReader.Create(new StringReader(xml))));
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
            if (testType.Equals("Valid")) chars = GetValidCharacters(charType);
            else if (testType.Equals("InValid")) chars = GetInValidCharacters(charType);

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
            switch (nodeType)
            {
                case "XElement":
                    element = new XElement(name, name);
                    xDocument.Add(element);
                    IEnumerable<XNode> nodeList = xDocument.Nodes();
                    Assert.True(nodeList.Count() == 1, "Failed to create element { " + name + " }");
                    xDocument.RemoveNodes();
                    break;
                case "XAttribute":
                    element = new XElement(name, name);
                    XAttribute attribute = new XAttribute(name, name);
                    element.Add(attribute);
                    xDocument.Add(element);
                    XAttribute x = element.Attribute(name);
                    Assert.Equal(name, x.Name.LocalName);
                    xDocument.RemoveNodes();
                    break;
                case "XName":
                    XName xName = XName.Get(name, name);
                    Assert.Equal(name, xName.LocalName);
                    Assert.Equal(name, xName.NamespaceName);
                    Assert.Equal(name, xName.Namespace.NamespaceName);
                    break;
                default:
                    break;
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

            Assert.True(false, "Expected exception not thrown");
        }

        /// <summary>
        /// returns a name using the character provided in the appropriate position
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
