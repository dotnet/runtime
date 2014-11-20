// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace XPathTests.Common
{
    public interface ICreateNavigator
    {
        XPathNavigator CreateNavigatorFromFile(string fileName);
        XPathNavigator CreateNavigator(string xml);
    }
}
