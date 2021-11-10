// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    // Specifies the type of validation to perform in XmlValidatingReader or in XmlReaderSettings.
    public enum ValidationType
    {
        // No validation will be performed.
        None,

        // In XmlValidatingReader ValidationType.Auto does the following:
        // 1) If there is no DTD or schema, it will parse the XML without validation.
        // 2) If there is a DTD defined in a <!DOCTYPE ...> declaration, it will load the DTD and
        //    process the DTD declarations such that default attributes and general entities will
        //    be made available. General entities are only loaded and parsed if they are used (expanded).
        // 3) If there is no <!DOCTYPE ...> declaration but there is an XSD "schemaLocation" attribute,
        //    it will load and process those XSD schemas and it will return any default attributes defined in those schemas.
        // 4) If there is no <!DOCTYPE ...&> declaration and no XSD "schemaLocation" attribute but there are some namespaces
        //    using the MSXML "x-schema:" URN prefix, it will load and process those schemas and it will return any default
        //    attributes defined in those schemas.
        [Obsolete("ValidationType.Auto has been deprecated. Use DTD or Schema instead.")]
        Auto,

        // Validate according to DTD.
        DTD,

        // Validate according to XDR.
        [Obsolete("XDR Validation through XmlValidatingReader has been deprecated and is not supported.")]
        XDR,

        // Validate according to W3C XSD schemas, including inline schemas. An error is returned if both XDR and XSD schemas
        // are referenced from the same document.
        Schema
    }
}
