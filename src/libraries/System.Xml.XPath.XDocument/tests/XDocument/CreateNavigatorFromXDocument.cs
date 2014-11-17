// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using XPathTests.Common;

namespace XPathTests
{
    public class CreateNavigatorFromXDocument : ICreateNavigator
    {
        public XPathNavigator CreateNavigatorFromFile(string fileName)
        {
            Stream stream = FileHelper.CreateStreamFromFile(fileName);
            XDocument xDocument = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            return xDocument.CreateNavigator();
        }

        public XPathNavigator CreateNavigator(string xml)
        {
            TextReader sr = new StringReader(xml);
            XDocument xDocument = XDocument.Load(sr, LoadOptions.PreserveWhitespace);
            return xDocument.CreateNavigator();
        }
    }
}
