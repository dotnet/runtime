//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.Xml;

    interface IExtensibleSyndicationObject
    {
        Dictionary<XmlQualifiedName, string> AttributeExtensions 
        { get; }
        SyndicationElementExtensionCollection ElementExtensions 
        { get; }
    }
}
