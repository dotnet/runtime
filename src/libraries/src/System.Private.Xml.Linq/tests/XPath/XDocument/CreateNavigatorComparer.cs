// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests
{
    public class CreateNavigatorComparer : ICreateNavigator
    {
        private ICreateNavigator _xmlDocNavCreator = new CreateNavigatorFromXmlDocument();
        private ICreateNavigator _xDocNavCreator = new CreateNavigatorFromXDocument();
        public XPathNavigator CreateNavigatorFromFile(string fileName)
        {
            var nav1 = _xmlDocNavCreator.CreateNavigatorFromFile(fileName);
            var nav2 = _xDocNavCreator.CreateNavigatorFromFile(fileName);
            return new System.Xml.XPath.XDocument.Tests.XDocument.NavigatorComparer(nav1, nav2);
        }

        public XPathNavigator CreateNavigator(string xml)
        {
            var nav1 = _xmlDocNavCreator.CreateNavigator(xml);
            var nav2 = _xDocNavCreator.CreateNavigator(xml);
            return new System.Xml.XPath.XDocument.Tests.XDocument.NavigatorComparer(nav1, nav2);
        }
    }
}
