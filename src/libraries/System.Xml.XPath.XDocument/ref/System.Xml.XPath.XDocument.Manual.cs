// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

// These types as extension methods support a better contract factoring between XDocument and XPath.

namespace System.Xml.XPath
{
    public static partial class XDocumentExtensions
    {
        public static System.Xml.XPath.IXPathNavigable ToXPathNavigable(this System.Xml.Linq.XNode node) { return default(System.Xml.XPath.IXPathNavigable); }
    }
}
