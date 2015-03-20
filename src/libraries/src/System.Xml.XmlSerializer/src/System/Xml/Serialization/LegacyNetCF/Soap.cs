// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

namespace System.Xml.Serialization.LegacyNetCF
{
    /// <summary>
    /// Contains constants frequently used in SOAP serialization per the spec.
    /// </summary>
    internal static class Soap
    {
        internal const string Namespace = "http://schemas.xmlsoap.org/soap/envelope/";
        internal const string Encoding = "http://schemas.xmlsoap.org/soap/encoding/";
        internal const string SoapencPrefix = "soapenc";
        internal const string Action = "SOAPAction";
        internal const string Envelope = "Envelope";
        internal const string Body = "Body";
        internal const string Header = "Header";
        internal const string MustUnderstand = "mustUnderstand";
        internal const string Actor = "actor";
        internal const string ArrayType = "Array";
        internal const string TempUri = "http://tempuri.org/";

        internal const string SoapPrefix = "soap";
        internal const string XmlPrefix = "xml";

        internal const string Xsd = "xsd";
        internal const string XsdUrl = "http://www.w3.org/2001/XMLSchema";
        internal const string Xsi = "xsi";
        internal const string XsiUrl = "http://www.w3.org/2001/XMLSchema-instance";
        internal const string UrtTypesNS = "http://microsoft.com/wsdl/types/";
        internal const string Xmlns = "http://www.w3.org/2000/xmlns/";
        internal const string TnsPrefix = "tns";
        internal const string TypesPrefix = "types";

        internal const string ServerCode = "Server";
        internal const string VersionMismatchCode = "VersionMismatch";
        internal const string MustUnderstandCode = "MustUnderstand";
        internal const string ClientCode = "Client";

        internal const string FaultDetail = "detail";
        internal const string Fault = "Fault";
        internal const string FaultCode = "faultcode";
        internal const string FaultString = "faultstring";
        internal const string FaultActor = "faultactor";

        internal const string EncodingStyle = "encodingStyle";

        internal const string Lang = "lang";
        internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    }

    /// <summary>
    /// Contains constants frequently used in SOAP serialization per the spec.
    /// </summary>
    internal static class Soap12
    {
        internal const string Namespace = "http://www.w3.org/2003/05/soap-envelope";
        internal const string Encoding = "http://www.w3.org/2003/05/soap-encoding";
        internal const string RpcNamespace = "http://www.w3.org/2003/05/soap-rpc";
        internal const string UpgradeNamespace = Namespace;
        internal const string Upgrade = "Upgrade";
        internal const string UpgradeEnvelope = "SupportedEnvelope";
        internal const string UpgradeEnvelopeQname = "qname";
        internal const string Role = "role";
        internal const string Relay = "relay";
        internal const string FaultCode = "Code";
        internal const string FaultReason = "Reason";
        internal const string FaultReasonText = "Text";
        internal const string FaultRole = "Role";
        internal const string FaultNode = "Node";
        internal const string FaultCodeValue = "Value";
        internal const string FaultSubcode = "Subcode";
        internal const string FaultDetail = "Detail";
        internal const string FaultText = "Text";

        internal static readonly string VersionMismatchCode = "VersionMismatch";
        internal static readonly string MustUnderstandCode = "MustUnderstand";
        internal static readonly string DataEncodingUnknownCode = "DataEncodingUnknown";
        internal static readonly string SenderCode = "Sender";
        internal static readonly string ReceiverCode = "Receiver";

        internal static readonly string RpcProcedureNotPresentSubcode = "ProcedureNotPresent";
        internal static readonly string RpcBadArgumentsSubcode = "BadArguments";

        internal static readonly string EncodingMissingIDFaultSubcode = "MissingID";
        internal static readonly string EncodingUntypedValueFaultSubcode = "UntypedValue";
    }
}
