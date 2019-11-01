// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl
{
    [Flags]
    internal enum XslFlags
    {
        None = 0x0000,

        // XPath types flags. These flags indicate what type the result of the expression may have.
        String = 0x0001,
        Number = 0x0002,
        Boolean = 0x0004,
        Node = 0x0008,
        Nodeset = 0x0010,
        Rtf = 0x0020,
        TypeFilter = AnyType,
        AnyType = XslFlags.String | XslFlags.Number | XslFlags.Boolean | XslFlags.Node | XslFlags.Nodeset | XslFlags.Rtf,

        // Focus flags. These flags indicate which of the three focus values (context item, context position,
        // context size) are required for calculation of the expression.
        Current = 0x0100,
        Position = 0x0200,
        Last = 0x0400,
        FocusFilter = FullFocus,
        FullFocus = XslFlags.Current | XslFlags.Position | XslFlags.Last,

        // Indicates that the expression contains at least one of xsl:call-template, xsl:apply-templates,
        // xsl:apply-imports, [xsl:]use-attribute-sets. Needed for default values of xsl:param's.
        HasCalls = 0x1000,

        // Used for xsl:param's only. Indicates that at least one caller does not pass value for this param,
        // so its default value will be used.
        MayBeDefault = 0x2000,

        // Indicates that expression may produce side effects
        // This flag is on for xsl:message and for calls to extension functions.
        SideEffects = 0x4000,

        // Indicates that the corresponding graph vertex has been already visited in flag propagation process.
        Stop = 0x8000,
    }
}
