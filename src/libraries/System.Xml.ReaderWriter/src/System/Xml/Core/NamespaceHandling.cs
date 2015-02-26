// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    //
    // NamespaceHandling speficies how should the XmlWriter handle namespaces.
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
