// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System.Collections.Generic;
    using System.Xml;

    internal interface IExtensibleSyndicationObject
    {
        Dictionary<XmlQualifiedName, string> AttributeExtensions
        { get; }
        SyndicationElementExtensionCollection ElementExtensions
        { get; }
    }
}
