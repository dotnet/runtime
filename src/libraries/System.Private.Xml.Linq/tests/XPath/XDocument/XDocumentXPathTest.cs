// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;

namespace XPathTests.Common
{
    public static partial class Utils
    {
        private static readonly ICreateNavigator _navigatorCreator = new CreateNavigatorComparer();
        public static readonly string ResourceFilesPath = "System.Xml.XPath.XDocument.Tests.TestData.";
    }
}
