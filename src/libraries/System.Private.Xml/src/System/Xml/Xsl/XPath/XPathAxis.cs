// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml.Xsl.XPath
{
    // Order is important - we use them as an index in QilAxis & AxisMask arrays
    internal enum XPathAxis
    {
        Unknown = 0,
        Ancestor,
        AncestorOrSelf,
        Attribute,
        Child,
        Descendant,
        DescendantOrSelf,
        Following,
        FollowingSibling,
        Namespace,
        Parent,
        Preceding,
        PrecedingSibling,
        Self,
        Root,
    }
}
