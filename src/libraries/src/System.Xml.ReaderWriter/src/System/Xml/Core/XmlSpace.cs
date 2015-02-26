// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    // An enumeration for the xml:space scope used in XmlReader and XmlWriter.
    public enum XmlSpace
    {
        // xml:space scope has not been specified.
        None = 0,

        // The xml:space scope is "default".
        Default = 1,

        // The xml:space scope is "preserve".
        Preserve = 2
    }
}
