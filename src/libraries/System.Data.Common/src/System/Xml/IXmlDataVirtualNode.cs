// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO: Enable after System.Private.Xml is annotated
#nullable disable

using System.Data;

namespace System.Xml
{
    internal interface IXmlDataVirtualNode
    {
        bool IsOnNode(XmlNode nodeToCheck);
        bool IsOnColumn(DataColumn col);
        bool IsInUse();
        void OnFoliated(XmlNode foliatedNode);
    }
}
