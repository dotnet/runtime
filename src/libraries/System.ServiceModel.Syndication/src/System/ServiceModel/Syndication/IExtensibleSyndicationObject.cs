// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal interface IExtensibleSyndicationObject
    {
        Dictionary<XmlQualifiedName, string> AttributeExtensions { get; }
        SyndicationElementExtensionCollection ElementExtensions { get; }
    }
}
