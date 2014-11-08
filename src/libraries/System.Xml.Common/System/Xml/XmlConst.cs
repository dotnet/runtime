// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    /// <summary>
    /// This class defines a set of common strings for sharing across multiple source files.
    /// </summary>
    internal static class XmlConst
    {
        #region Reserved namespaces
        internal const string ReservedNsXml = "http://www.w3.org/XML/1998/namespace";
        internal const string ReservedNsXmlNs = "http://www.w3.org/2000/xmlns/";
        internal const string ReservedNsDataType = "urn:schemas-microsoft-com:datatypes";
        internal const string ReservedNsDataTypeAlias = "uuid:C2F41010-65B3-11D1-A29F-00AA00C14882";
        internal const string ReservedNsDataTypeOld = "urn:uuid:C2F41010-65B3-11D1-A29F-00AA00C14882/";
        internal const string ReservedNsMsxsl = "urn:schemas-microsoft-com:xslt";
        internal const string ReservedNsXdr = "urn:schemas-microsoft-com:xml-data";
        internal const string ReservedNsXslDebug = "urn:schemas-microsoft-com:xslt-debug";
        internal const string ReservedNsXdrAlias = "uuid:BDC6E3F0-6DA3-11D1-A2A3-00AA00C14882";
        internal const string ReservedNsWdXsl = "http://www.w3.org/TR/WD-xsl";
        internal const string ReservedNsXs = "http://www.w3.org/2001/XMLSchema";
        internal const string ReservedNsXsd = "http://www.w3.org/2001/XMLSchema-datatypes";
        internal const string ReservedNsXsi = "http://www.w3.org/2001/XMLSchema-instance";
        internal const string ReservedNsXslt = "http://www.w3.org/1999/XSL/Transform";
        internal const string ReservedNsExsltCommon = "http://exslt.org/common";
        internal const string ReservedNsExsltDates = "http://exslt.org/dates-and-times";
        internal const string ReservedNsExsltMath = "http://exslt.org/math";
        internal const string ReservedNsExsltRegExps = "http://exslt.org/regular-expressions";
        internal const string ReservedNsExsltSets = "http://exslt.org/sets";
        internal const string ReservedNsExsltStrings = "http://exslt.org/strings";
        internal const string ReservedNsXQueryFunc = "http://www.w3.org/2003/11/xpath-functions";
        internal const string ReservedNsXQueryDataType = "http://www.w3.org/2003/11/xpath-datatypes";
        internal const string ReservedNsCollationBase = "http://collations.microsoft.com";
        internal const string ReservedNsCollCodePoint = "http://www.w3.org/2004/10/xpath-functions/collation/codepoint";
        internal const string ReservedNsXsltInternal = "http://schemas.microsoft.com/framework/2003/xml/xslt/internal";
        #endregion

        #region Local namespaces
        public const string NsXml = "xml";
        public const string NsXmlNs = "xmlns";
        #endregion

        #region Attributes
        public const string AttrLang = "lang";
        public const string AttrSpace = "space";
        public const string AttrSpaceValueDefault = "default";
        public const string AttrSpaceValuePreserve = "preserve";
        public const string AttrXmlLang = NsXml + ":" + AttrLang;
        #endregion

        public const string XmlDeclarationTag = "xml";
    }
}
