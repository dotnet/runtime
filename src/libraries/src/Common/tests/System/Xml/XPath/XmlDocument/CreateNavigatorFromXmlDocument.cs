// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests
{
    public class CreateNavigatorFromXmlDocument : ICreateNavigator
    {
        public XPathNavigator CreateNavigatorFromFile(string fileName)
        {
            var stream = FileHelper.CreateStreamFromFile(fileName);
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.Load(stream);
            return xmlDocument.CreateNavigator();
        }

        public XPathNavigator CreateNavigator(string xml)
        {
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);
            return xmlDocument.CreateNavigator();
        }
    }
}
