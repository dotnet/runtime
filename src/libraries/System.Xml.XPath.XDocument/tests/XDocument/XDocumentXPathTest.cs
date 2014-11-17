// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XPathTests.Common;

namespace XPathTests.Common
{
    public static partial class Utils
    {
        private readonly static ICreateNavigator _navigatorCreator = new CreateNavigatorComparer();
        public readonly static string ResourceFilesPath = "System.Xml.XPath.XDocument.Tests.TestData.";
    }
}
