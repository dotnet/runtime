// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Xml;
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    public abstract class DirectoryResponse : DirectoryOperation
    {
        internal XmlNode dsmlNode = null;
        internal XmlNamespaceManager dsmlNS = null;
        internal bool dsmlRequest = false;

        internal string dn = null;
        internal DirectoryControl[] directoryControls = null;
        internal ResultCode result = (ResultCode)(-1);
        internal string directoryMessage = null;
        internal Uri[] directoryReferral = null;

        private string _requestID = null;

        internal DirectoryResponse(XmlNode node)
        {
            Debug.Assert(node != null);

            dsmlNode = node;

            dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();

            dsmlRequest = true;
        }

        internal DirectoryResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral)
        {
            this.dn = dn;
            this.directoryControls = controls;
            this.result = result;
            this.directoryMessage = message;
            this.directoryReferral = referral;
        }

        public string RequestId
        {
            get
            {
                // this is a dsml request
                if (dsmlRequest && (_requestID == null))
                {
                    // Locate the requestID attribute
                    XmlAttribute attrReqID = (XmlAttribute)dsmlNode.SelectSingleNode("@dsml:requestID", dsmlNS);

                    if (attrReqID == null)
                    {
                        // try it without the namespace qualifier, in case the sender omitted it
                        attrReqID = (XmlAttribute)dsmlNode.SelectSingleNode("@requestID", dsmlNS);
                    }

                    if (attrReqID != null)
                    {
                        _requestID = attrReqID.Value;
                    }
                }

                return _requestID;
            }
        }

        public virtual string MatchedDN
        {
            get
            {
                if (dsmlRequest && (dn == null))
                {
                    dn = MatchedDNHelper("@dsml:matchedDN", "@matchedDN");
                }

                return dn;
            }
        }

        public virtual DirectoryControl[] Controls
        {
            get
            {
                if (dsmlRequest && (directoryControls == null))
                {
                    directoryControls = ControlsHelper("dsml:control");
                }

                if (directoryControls == null)
                    return new DirectoryControl[0];
                else
                {
                    DirectoryControl[] tempControls = new DirectoryControl[directoryControls.Length];
                    for (int i = 0; i < directoryControls.Length; i++)
                        tempControls[i] = new DirectoryControl(directoryControls[i].Type, directoryControls[i].GetValue(), directoryControls[i].IsCritical, directoryControls[i].ServerSide);

                    DirectoryControl.TransformControls(tempControls);

                    return tempControls;
                }
            }
        }

        public virtual ResultCode ResultCode
        {
            get
            {
                if (dsmlRequest && ((int)result == -1))
                {
                    result = ResultCodeHelper("dsml:resultCode/@dsml:code", "dsml:resultCode/@code");
                }

                return result;
            }
        }

        public virtual string ErrorMessage
        {
            get
            {
                if (dsmlRequest && (directoryMessage == null))
                {
                    directoryMessage = ErrorMessageHelper("dsml:errorMessage");
                }

                return directoryMessage;
            }
        }

        public virtual Uri[] Referral
        {
            get
            {
                if (dsmlRequest && (directoryReferral == null))
                {
                    directoryReferral = ReferralHelper("dsml:referral");
                }

                if (directoryReferral == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempReferral = new Uri[directoryReferral.Length];
                    for (int i = 0; i < directoryReferral.Length; i++)
                    {
                        tempReferral[i] = new Uri(directoryReferral[i].AbsoluteUri);
                    }
                    return tempReferral;
                }
            }
        }

        //
        // Private/protected
        //

        // Methods used to implement the above properties
        internal string MatchedDNHelper(string primaryXPath, string secondaryXPath)
        {
            // Locate the matchedDN attribute
            XmlAttribute attrMatchedDN = (XmlAttribute)dsmlNode.SelectSingleNode(primaryXPath, dsmlNS);

            if (attrMatchedDN == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrMatchedDN = (XmlAttribute)dsmlNode.SelectSingleNode(secondaryXPath, dsmlNS);

                if (attrMatchedDN == null)
                {
                    // the element doesn't have a associated dn
                    return null;
                }

                return attrMatchedDN.Value;
            }
            else
            {
                return attrMatchedDN.Value;
            }
        }

        internal DirectoryControl[] ControlsHelper(string primaryXPath)
        {
            // Get the set of control nodes
            XmlNodeList nodeList = dsmlNode.SelectNodes(primaryXPath, dsmlNS);

            if (nodeList.Count == 0)
            {
                // the server returned no controls
                return new DirectoryControl[0];
            }

            // Build the DirectoryControl array
            DirectoryControl[] controls = new DirectoryControl[nodeList.Count];
            int index = 0;

            foreach (XmlNode node in nodeList)
            {
                Debug.Assert(node is XmlElement);

                controls[index] = new DirectoryControl((XmlElement)node);
                index++;
            }

            return controls;
        }

        internal ResultCode ResultCodeHelper(string primaryXPath, string secondaryXPath)
        {
            // Retrieve the result code attribute
            XmlAttribute attrResultCode = (XmlAttribute)dsmlNode.SelectSingleNode(primaryXPath, dsmlNS);

            if (attrResultCode == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrResultCode = (XmlAttribute)dsmlNode.SelectSingleNode(secondaryXPath, dsmlNS);

                if (attrResultCode == null)
                {
                    // the resultCode is not optional
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingOperationResponseResultCode));
                }
            }

            // Validate the result code
            string resCodeString = attrResultCode.Value;
            int resCodeInt;

            try
            {
                resCodeInt = int.Parse(resCodeString, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                throw new DsmlInvalidDocumentException(Res.GetString(Res.BadOperationResponseResultCode, resCodeString));
            }
            catch (OverflowException)
            {
                throw new DsmlInvalidDocumentException(Res.GetString(Res.BadOperationResponseResultCode, resCodeString));
            }

            if (!Utility.IsResultCode((ResultCode)resCodeInt))
            {
                throw new DsmlInvalidDocumentException(Res.GetString(Res.BadOperationResponseResultCode, resCodeString));
            }

            // Transform the result code into an LDAPResultCode
            ResultCode resCode = (ResultCode)resCodeInt;

            return resCode;
        }

        internal string ErrorMessageHelper(string primaryXPath)
        {
            XmlElement elMessage = (XmlElement)dsmlNode.SelectSingleNode(primaryXPath, dsmlNS);

            if (elMessage != null)
            {
                return elMessage.InnerText;
            }
            else
            {
                // server didn't return a errorMessage
                return null;
            }
        }

        internal Uri[] ReferralHelper(string primaryXPath)
        {
            // Get the set of referral nodes
            XmlNodeList nodeList = dsmlNode.SelectNodes(primaryXPath, dsmlNS);

            if (nodeList.Count == 0)
            {
                // the server returned no referrals
                return new Uri[0];
            }

            // Build the Uri array
            Uri[] uris = new Uri[nodeList.Count];
            int index = 0;

            foreach (XmlNode node in nodeList)
            {
                uris[index] = new Uri(node.InnerText);
                index++;
            }

            return uris;
        }
    }

    public class DeleteResponse : DirectoryResponse
    {
        internal DeleteResponse(XmlNode node) : base(node) { }
        internal DeleteResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The AddResponse class for representing <addResponse>
    /// </summary>
    public class AddResponse : DirectoryResponse
    {
        internal AddResponse(XmlNode node) : base(node) { }
        internal AddResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ModifyResponse class for representing <modifyResponse>
    /// </summary>
    public class ModifyResponse : DirectoryResponse
    {
        internal ModifyResponse(XmlNode node) : base(node) { }
        internal ModifyResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ModifyDnResponse class for representing <modDNResponse>
    /// </summary>
    public class ModifyDNResponse : DirectoryResponse
    {
        internal ModifyDNResponse(XmlNode node) : base(node) { }
        internal ModifyDNResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The CompareResponse class for representing <compareResponse>
    /// </summary>
    public class CompareResponse : DirectoryResponse
    {
        internal CompareResponse(XmlNode node) : base(node) { }
        internal CompareResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }
    }

    /// <summary>
    /// The ExtendedResponse class for representing <extendedResponse>
    /// </summary>
    public class ExtendedResponse : DirectoryResponse
    {
        internal string name = null;
        internal byte[] value = null;

        internal ExtendedResponse(XmlNode node) : base(node) { }
        internal ExtendedResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        //
        // Public
        //

        // Properties
        public string ResponseName
        {
            get
            {
                if (dsmlRequest && (name == null))
                {
                    XmlElement elRespName = (XmlElement)dsmlNode.SelectSingleNode("dsml:responseName", dsmlNS);

                    if (elRespName != null)
                    {
                        name = elRespName.InnerText;
                    }
                }

                return name;
            }
        }

        public byte[] ResponseValue
        {
            get
            {
                if (dsmlRequest && (value == null))
                {
                    XmlElement elRespValue = (XmlElement)dsmlNode.SelectSingleNode("dsml:response", dsmlNS);

                    if (elRespValue != null)
                    {
                        // server returns a response value
                        string base64EncodedValue = elRespValue.InnerText;

                        try
                        {
                            value = System.Convert.FromBase64String(base64EncodedValue);
                        }
                        catch (FormatException)
                        {
                            // server returned invalid base64
                            throw new DsmlInvalidDocumentException(Res.GetString(Res.BadBase64Value));
                        }
                    }
                }

                if (value == null)
                    return new byte[0];
                else
                {
                    byte[] tmpValue = new byte[value.Length];
                    for (int i = 0; i < value.Length; i++)
                    {
                        tmpValue[i] = value[i];
                    }
                    return tmpValue;
                }
            }
        }
    }

    public class SearchResponse : DirectoryResponse
    {
        private SearchResultReferenceCollection _referenceCollection = new SearchResultReferenceCollection();
        private SearchResultEntryCollection _entryCollection = new SearchResultEntryCollection();
        internal bool searchDone = false;

        internal SearchResponse(XmlNode node) : base(node) { }
        internal SearchResponse(string dn, DirectoryControl[] controls, ResultCode result, string message, Uri[] referral) : base(dn, controls, result, message, referral) { }

        public override string MatchedDN
        {
            get
            {
                if (dsmlRequest && (dn == null))
                {
                    dn = MatchedDNHelper("dsml:searchResultDone/@dsml:matchedDN",
                                           "dsml:searchResultDone/@matchedDN");
                }

                return dn;
            }
        }

        public override DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;
                if (dsmlRequest && (directoryControls == null))
                {
                    directoryControls = ControlsHelper("dsml:searchResultDone/dsml:control");
                }

                if (directoryControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[directoryControls.Length];
                    for (int i = 0; i < directoryControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(directoryControls[i].Type, directoryControls[i].GetValue(), directoryControls[i].IsCritical, directoryControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }

        public override ResultCode ResultCode
        {
            get
            {
                if (dsmlRequest && ((int)result == -1))
                {
                    result = ResultCodeHelper("dsml:searchResultDone/dsml:resultCode/@dsml:code",
                                            "dsml:searchResultDone/dsml:resultCode/@code");
                }

                return result;
            }
        }

        public override string ErrorMessage
        {
            get
            {
                if (dsmlRequest && (directoryMessage == null))
                {
                    directoryMessage = ErrorMessageHelper("dsml:searchResultDone/dsml:errorMessage");
                }

                return directoryMessage;
            }
        }

        public override Uri[] Referral
        {
            get
            {
                if (dsmlRequest && (directoryReferral == null))
                {
                    directoryReferral = ReferralHelper("dsml:searchResultDone/dsml:referral");
                }

                if (directoryReferral == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempReferral = new Uri[directoryReferral.Length];
                    for (int i = 0; i < directoryReferral.Length; i++)
                    {
                        tempReferral[i] = new Uri(directoryReferral[i].AbsoluteUri);
                    }
                    return tempReferral;
                }
            }
        }

        public SearchResultReferenceCollection References
        {
            get
            {
                if (dsmlRequest && (_referenceCollection.Count == 0))
                {
                    _referenceCollection = ReferenceHelper();
                }

                return _referenceCollection;
            }
        }

        public SearchResultEntryCollection Entries
        {
            get
            {
                if (dsmlRequest && (_entryCollection.Count == 0))
                {
                    _entryCollection = EntryHelper();
                }

                return _entryCollection;
            }
        }

        internal void SetReferences(SearchResultReferenceCollection col)
        {
            _referenceCollection = col;
        }

        internal void SetEntries(SearchResultEntryCollection col)
        {
            _entryCollection = col;
        }

        private SearchResultReferenceCollection ReferenceHelper()
        {
            SearchResultReferenceCollection refCollection = new SearchResultReferenceCollection();

            // Get the set of control nodes
            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:searchResultReference", dsmlNS);

            if (nodeList.Count != 0)
            {
                foreach (XmlNode node in nodeList)
                {
                    Debug.Assert(node is XmlElement);

                    SearchResultReference attribute = new SearchResultReference((XmlElement)node);
                    refCollection.Add(attribute);
                }
            }

            return refCollection;
        }

        private SearchResultEntryCollection EntryHelper()
        {
            SearchResultEntryCollection resultCollection = new SearchResultEntryCollection();

            // Get the set of control nodes
            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:searchResultEntry", dsmlNS);

            if (nodeList.Count != 0)
            {
                foreach (XmlNode node in nodeList)
                {
                    Debug.Assert(node is XmlElement);

                    SearchResultEntry attribute = new SearchResultEntry((XmlElement)node);
                    resultCollection.Add(attribute);
                }
            }

            return resultCollection;
        }
    }
}

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Xml;
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    public class DsmlErrorResponse : DirectoryResponse
    {
        private string _message = null;
        private string _detail = null;
        private ErrorResponseCategory _category = (ErrorResponseCategory)(-1);

        internal DsmlErrorResponse(XmlNode node) : base(node) { }

        //
        // Public
        //

        // Properties
        public string Message
        {
            get
            {
                if (_message == null)
                {
                    XmlElement elMessage = (XmlElement)dsmlNode.SelectSingleNode("dsml:message", dsmlNS);

                    if (elMessage != null)
                    {
                        _message = elMessage.InnerText;
                    }
                }

                return _message;
            }
        }

        public string Detail
        {
            get
            {
                if (_detail == null)
                {
                    XmlElement elDetail = (XmlElement)dsmlNode.SelectSingleNode("dsml:detail", dsmlNS);

                    if (elDetail != null)
                    {
                        _detail = elDetail.InnerXml;
                    }
                }

                return _detail;
            }
        }

        public ErrorResponseCategory Type
        {
            get
            {
                if ((int)_category == -1)
                {
                    XmlAttribute attrType = (XmlAttribute)dsmlNode.SelectSingleNode("@dsml:type", dsmlNS);

                    if (attrType == null)
                    {
                        // try it without the namespace qualifier, in case the sender omitted it
                        attrType = (XmlAttribute)dsmlNode.SelectSingleNode("@type", dsmlNS);
                    }

                    // verify we got a "type" attribute
                    if (attrType == null)
                    {
                        // the "type" attribute is not optional
                        throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingErrorResponseType));
                    }

                    // map the "type" attribute to a ErrorResponseCategory                    
                    switch (attrType.Value)
                    {
                        case "notAttempted":
                            _category = ErrorResponseCategory.NotAttempted;
                            break;

                        case "couldNotConnect":
                            _category = ErrorResponseCategory.CouldNotConnect;
                            break;

                        case "connectionClosed":
                            _category = ErrorResponseCategory.ConnectionClosed;
                            break;

                        case "malformedRequest":
                            _category = ErrorResponseCategory.MalformedRequest;
                            break;

                        case "gatewayInternalError":
                            _category = ErrorResponseCategory.GatewayInternalError;
                            break;

                        case "authenticationFailed":
                            _category = ErrorResponseCategory.AuthenticationFailed;
                            break;

                        case "unresolvableURI":
                            _category = ErrorResponseCategory.UnresolvableUri;
                            break;

                        case "other":
                            _category = ErrorResponseCategory.Other;
                            break;

                        default:
                            throw new DsmlInvalidDocumentException(Res.GetString(Res.ErrorResponseInvalidValue, attrType.Value));
                    }
                }

                return _category;
            }
        }

        public override string MatchedDN
        {
            get
            {
                throw new NotSupportedException(Res.GetString(Res.NotSupportOnDsmlErrRes));
            }
        }

        public override DirectoryControl[] Controls
        {
            get
            {
                throw new NotSupportedException(Res.GetString(Res.NotSupportOnDsmlErrRes));
            }
        }

        public override ResultCode ResultCode
        {
            get
            {
                throw new NotSupportedException(Res.GetString(Res.NotSupportOnDsmlErrRes));
            }
        }

        public override string ErrorMessage
        {
            get
            {
                throw new NotSupportedException(Res.GetString(Res.NotSupportOnDsmlErrRes));
            }
        }

        public override Uri[] Referral
        {
            get
            {
                throw new NotSupportedException(Res.GetString(Res.NotSupportOnDsmlErrRes));
            }
        }
    }

    public class DsmlAuthResponse : DirectoryResponse
    {
        internal DsmlAuthResponse(XmlNode node) : base(node) { }
    }
}

