// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    internal class DsmlConstants
    {
        private DsmlConstants() { }

        // XML namespace URIs
        public const string DsmlUri = "urn:oasis:names:tc:DSML:2:0:core";
        public const string XsiUri = "http://www.w3.org/2001/XMLSchema-instance";
        public const string XsdUri = "http://www.w3.org/2001/XMLSchema";
        public const string SoapUri = "http://schemas.xmlsoap.org/soap/envelope/";
        public const string ADSessionUri = "urn:schema-microsoft-com:activedirectory:dsmlv2";

        // default search filter
        public const string DefaultSearchFilter = "<present name='objectClass' xmlns=\"" + DsmlUri + "\"/>";

        // HTTP method
        public const string HttpPostMethod = "POST";

        // SOAP elements
        public const string SOAPEnvelopeBegin = "<se:Envelope xmlns:se=\"" + SoapUri + "\">";
        public const string SOAPEnvelopeEnd = "</se:Envelope>";
        public const string SOAPBodyBegin = "<se:Body xmlns=\"" + DsmlUri + "\">";
        public const string SOAPBodyEnd = "</se:Body>";
        public const string SOAPHeaderBegin = "<se:Header>";
        public const string SOAPHeaderEnd = "</se:Header>";

        public const string SOAPSession1 = "<ad:Session xmlns:ad=\"" + ADSessionUri + "\" ad:SessionID=\"";
        public const string SOAPSession2 = "\" se:mustUnderstand=\"1\"/>";

        public const string SOAPBeginSession = "<ad:BeginSession xmlns:ad=\"" + ADSessionUri + "\" se:mustUnderstand=\"1\"/>";

        public const string SOAPEndSession1 = "<ad:EndSession xmlns:ad=\"" + ADSessionUri + "\" ad:SessionID=\"";
        public const string SOAPEndSession2 = "\" se:mustUnderstand=\"1\"/>";

        // DSML v2 Response Elements
        public const string DsmlErrorResponse = "errorResponse";
        public const string DsmlSearchResponse = "searchResponse";
        public const string DsmlModifyResponse = "modifyResponse";
        public const string DsmlAddResponse = "addResponse";
        public const string DsmlDelResponse = "delResponse";
        public const string DsmlModDNResponse = "modDNResponse";
        public const string DsmlCompareResponse = "compareResponse";
        public const string DsmlExtendedResponse = "extendedResponse";
        public const string DsmlAuthResponse = "authResponse";

        public const string AttrTypePrefixedName = "xsi:type";
        public const string AttrBinaryTypePrefixedValue = "xsd:base64Binary";
        public const string AttrDsmlAttrName = "name";
        public const string ElementDsmlAttrValue = "value";
        public const string ElementSearchReqFilter = "filter";

        public const string ElementSearchReqFilterAnd = "and";
        public const string ElementSearchReqFilterOr = "or";
        public const string ElementSearchReqFilterNot = "not";
        public const string ElementSearchReqFilterSubstr = "substrings";
        public const string ElementSearchReqFilterEqual = "equalityMatch";
        public const string ElementSearchReqFilterGrteq = "greaterOrEqual";
        public const string ElementSearchReqFilterLesseq = "lessOrEqual";
        public const string ElementSearchReqFilterApprox = "approxMatch";
        public const string ElementSearchReqFilterPresent = "present";
        public const string ElementSearchReqFilterExtenmatch = "extensibleMatch";
        public const string ElementSearchReqFilterExtenmatchValue = "value";

        public const string AttrSearchReqFilterPresentName = "name";
        public const string AttrSearchReqFilterExtenmatchName = "name";
        public const string AttrSearchReqFilterExtenmatchMatchrule = "matchingRule";
        public const string AttrSearchReqFilterExtenmatchDnattr = "dnAttributes";

        public const string AttrSearchReqFilterSubstrName = "name";
        public const string ElementSearchReqFilterSubstrInit = "initial";
        public const string ElementSearchReqFilterSubstrAny = "any";
        public const string ElementSearchReqFilterSubstrFinal = "final";
    }
}
