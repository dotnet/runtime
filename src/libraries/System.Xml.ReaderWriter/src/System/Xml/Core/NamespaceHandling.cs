// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    //
    // NamespaceHandling specifies how should the XmlWriter handle namespaces.
    //  

    [Flags]
    public enum NamespaceHandling
    {
        //
        // Default behavior
        //
        Default = 0x0,

        //
        // Duplicate namespace declarations will be removed
        //
        OmitDuplicates = 0x1,
    }
}
