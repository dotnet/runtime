// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    // Specifies how entities are handled in XmlTextReader and XmlValidatingReader.
    public enum EntityHandling
    {
        // Expand all entities. This is the default in XmlValidatingReader. No nodes with NodeType EntityReference will be returned. 
        // The entity text is expanded in place of the entity references.
        ExpandEntities = 1,

        // Expand character entities only and return general entities as nodes (NodeType=XmlNodeType.EntityReference, Name=the name of the entity).
        // Default in XmlTextReader. You must call ResolveEntity to see what the general entity expands to.
        ExpandCharEntities = 2,
    }
}
