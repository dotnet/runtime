// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    // DtdProcessing enumerations speficies how will an XmlReader handle DTDs in the XML document.
    //
    // Prohibit     The XmlReader will throw an exception when it finds a '<!DOCTYPE' markup.
    // Ignore       The DTD will be ignored. Any reference to a general entity in the XML document 
    //              will cause an exception (except for the predefined entities &lt; &gt; &amp; &quot; and &apos;).
    //              The DocumentType node will not be reported.
    // Parse        The DTD will be parsed and fully processed (entities expanded, default attributes added etc.)
    //
    public enum DtdProcessing
    {
        Prohibit,
        Ignore,
        Parse
    }
}
