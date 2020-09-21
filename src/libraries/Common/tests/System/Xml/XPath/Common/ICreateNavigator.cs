// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace XPathTests.Common
{
    public interface ICreateNavigator
    {
        XPathNavigator CreateNavigatorFromFile(string fileName);
        XPathNavigator CreateNavigator(string xml);
    }
}
