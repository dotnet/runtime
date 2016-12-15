// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;
    using System.Collections;
    using System.IO;
    using System.Diagnostics;
    using System.Globalization;
    using System.ComponentModel;
    using System.Collections.Specialized;
    using System.Security.Permissions;

    public abstract class DirectoryRequest : DirectoryOperation
    {
        internal DirectoryControlCollection directoryControlCollection = null;

        internal DirectoryRequest()
        {
            Utility.CheckOSVersion();

            directoryControlCollection = new DirectoryControlCollection();
        }

        public string RequestId
        {
            get
            {
                return directoryRequestID;
            }
            set
            {
                directoryRequestID = value;
            }
        }

        public DirectoryControlCollection Controls
        {
            get
            {
                return directoryControlCollection;
            }
        }

        //
        // Internal
        //

        // Returns a XmlElement representing this object
        // in DSML v2 format
        internal XmlElement ToXmlNodeHelper(XmlDocument doc)
        {
            return ToXmlNode(doc);
        }

        // Overloaded to implement the operation-specific portion of transforming
        // an object into its DSML v2 XML representation.
        protected abstract XmlElement ToXmlNode(XmlDocument doc);

        // Produces a XmlElement containing all the attributes/elements common
        // to DSML v2 requests
        internal XmlElement CreateRequestElement(XmlDocument doc,
                                                  string requestName,
                                                  bool includeDistinguishedName,
                                                  string distinguishedName)
        {
            // Create the element to represent the request
            XmlElement elem = doc.CreateElement(requestName, DsmlConstants.DsmlUri);

            if (includeDistinguishedName)
            {
                XmlAttribute attrDn = doc.CreateAttribute("dn", null);
                attrDn.InnerText = distinguishedName;
                elem.Attributes.Append(attrDn);
            }

            // Attach the requestID to the request
            if (directoryRequestID != null)
            {
                XmlAttribute attrReqID = doc.CreateAttribute("requestID", null);
                attrReqID.InnerText = directoryRequestID;
                elem.Attributes.Append(attrReqID);
            }

            // Attach the controls to the request
            // (controls are always the first child elements)
            if (directoryControlCollection != null)
            {
                foreach (DirectoryControl control in directoryControlCollection)
                {
                    XmlElement elControl = control.ToXmlNode(doc);
                    elem.AppendChild(elControl);
                }
            }

            return elem;
        }
    }

    public class DeleteRequest : DirectoryRequest
    {
        //
        // Public
        //

        public DeleteRequest() { }

        public DeleteRequest(string distinguishedName)
        {
            _dn = distinguishedName;
        }

        // Member properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "delRequest", true, _dn);

            return elem;
        }
    }

    public class AddRequest : DirectoryRequest
    {
        //
        // Public
        //

        public AddRequest()
        {
            _attributeList = new DirectoryAttributeCollection();
        }

        public AddRequest(string distinguishedName, params DirectoryAttribute[] attributes) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            if (attributes != null)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    _attributeList.Add(attributes[i]);
                }
            }
        }

        public AddRequest(string distinguishedName, string objectClass) : this()
        {
            // parameter validation
            if (objectClass == null)
                throw new ArgumentNullException("objectClass");

            // Store off the distinguished name
            _dn = distinguishedName;

            // Store off the objectClass in an object class attribute
            DirectoryAttribute objClassAttr = new DirectoryAttribute();

            objClassAttr.Name = "objectClass";
            objClassAttr.Add(objectClass);
            _attributeList.Add(objClassAttr);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttributeCollection Attributes
        {
            get
            {
                return _attributeList;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttributeCollection _attributeList;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "addRequest", true, _dn);

            // Add in the attributes
            if (_attributeList != null)
            {
                foreach (DirectoryAttribute attr in _attributeList)
                {
                    XmlElement elAttr = ((DirectoryAttribute)attr).ToXmlNode(doc, "attr");
                    elem.AppendChild(elAttr);
                }
            }

            return elem;
        }
    }

    public class ModifyRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ModifyRequest()
        {
            _attributeModificationList = new DirectoryAttributeModificationCollection();
        }

        public ModifyRequest(string distinguishedName, params DirectoryAttributeModification[] modifications) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            // Store off the initial list of modifications
            _attributeModificationList.AddRange(modifications);
        }

        public ModifyRequest(string distinguishedName, DirectoryAttributeOperation operation, string attributeName, params object[] values) : this()
        {
            // Store off the distinguished name
            _dn = distinguishedName;

            // validate the attributeName
            if (attributeName == null)
                throw new ArgumentNullException("attributeName");

            DirectoryAttributeModification mod = new DirectoryAttributeModification();
            mod.Operation = operation;
            mod.Name = attributeName;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                    mod.Add(values[i]);
            }

            _attributeModificationList.Add(mod);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttributeModificationCollection Modifications
        {
            get
            {
                return _attributeModificationList;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttributeModificationCollection _attributeModificationList;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "modifyRequest", true, _dn);

            // Add in the attributes

            if (_attributeModificationList != null)
            {
                foreach (DirectoryAttributeModification attr in _attributeModificationList)
                {
                    XmlElement elAttr = attr.ToXmlNode(doc);
                    elem.AppendChild(elAttr);
                }
            }

            return elem;
        }
    }

    public class CompareRequest : DirectoryRequest
    {
        //
        // Public
        //
        public CompareRequest() { }

        public CompareRequest(string distinguishedName, string attributeName, string value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, string attributeName, byte[] value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, string attributeName, Uri value)
        {
            CompareRequestHelper(distinguishedName, attributeName, value);
        }

        public CompareRequest(string distinguishedName, DirectoryAttribute assertion)
        {
            if (assertion == null)
                throw new ArgumentNullException("assertion");

            if (assertion.Count != 1)
                throw new ArgumentException(Res.GetString(Res.WrongNumValuesCompare));

            CompareRequestHelper(distinguishedName, assertion.Name, assertion[0]);
        }

        private void CompareRequestHelper(string distinguishedName, string attributeName, object value)
        {
            // parameter validation
            if (attributeName == null)
                throw new ArgumentNullException("attributeName");

            if (value == null)
                throw new ArgumentNullException("value");

            // store off the DN
            _dn = distinguishedName;

            // store off the attribute name and value            
            _attribute.Name = attributeName;
            _attribute.Add(value);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public DirectoryAttribute Assertion
        {
            get
            {
                return _attribute;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private DirectoryAttribute _attribute = new DirectoryAttribute();

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "compareRequest", true, _dn);

            // add in the attribute
            if (_attribute.Count != 1)
                throw new ArgumentException(Res.GetString(Res.WrongNumValuesCompare));

            XmlElement elAttr = _attribute.ToXmlNode(doc, "assertion");
            elem.AppendChild(elAttr);

            return elem;
        }
    }

    public class ModifyDNRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ModifyDNRequest() { }

        public ModifyDNRequest(string distinguishedName,
                                string newParentDistinguishedName,
                                string newName)
        {
            // store off the DN
            _dn = distinguishedName;

            _newSuperior = newParentDistinguishedName;
            _newRDN = newName;
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public string NewParentDistinguishedName
        {
            get
            {
                return _newSuperior;
            }

            set
            {
                _newSuperior = value;
            }
        }

        public string NewName
        {
            get
            {
                return _newRDN;
            }

            set
            {
                _newRDN = value;
            }
        }

        public bool DeleteOldRdn
        {
            get
            {
                return _deleteOldRDN;
            }

            set
            {
                _deleteOldRDN = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn;
        private string _newSuperior;
        private string _newRDN;
        private bool _deleteOldRDN = true;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "modDNRequest", true, _dn);

            // "newrdn" attribute (required)
            XmlAttribute attrNewRDN = doc.CreateAttribute("newrdn", null);
            attrNewRDN.InnerText = _newRDN;
            elem.Attributes.Append(attrNewRDN);

            // "deleteoldrdn" attribute (optional, but we'll always include it)
            XmlAttribute attrDeleteOldRDN = doc.CreateAttribute("deleteoldrdn", null);
            attrDeleteOldRDN.InnerText = _deleteOldRDN ? "true" : "false";
            elem.Attributes.Append(attrDeleteOldRDN);

            // "newSuperior" attribute (optional)
            if (_newSuperior != null)
            {
                XmlAttribute attrNewSuperior = doc.CreateAttribute("newSuperior", null);
                attrNewSuperior.InnerText = _newSuperior;
                elem.Attributes.Append(attrNewSuperior);
            }

            return elem;
        }
    }

    /// <summary>
    /// The representation of a <extendedRequest>
    /// </summary>
    public class ExtendedRequest : DirectoryRequest
    {
        //
        // Public
        //
        public ExtendedRequest() { }

        public ExtendedRequest(string requestName)
        {
            _requestName = requestName;
        }

        public ExtendedRequest(string requestName, byte[] requestValue) : this(requestName)
        {
            _requestValue = requestValue;
        }

        // Properties
        public string RequestName
        {
            get
            {
                return _requestName;
            }

            set
            {
                _requestName = value;
            }
        }

        public byte[] RequestValue
        {
            get
            {
                if (_requestValue == null)
                    return new byte[0];
                else
                {
                    byte[] tempValue = new byte[_requestValue.Length];
                    for (int i = 0; i < _requestValue.Length; i++)
                        tempValue[i] = _requestValue[i];

                    return tempValue;
                }
            }

            set
            {
                _requestValue = value;
            }
        }

        //
        // Private/protected
        //

        private string _requestName;
        private byte[] _requestValue = null;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "extendedRequest", false, null);

            // <requestName>
            XmlElement elemName = doc.CreateElement("requestName", DsmlConstants.DsmlUri);
            elemName.InnerText = _requestName;
            elem.AppendChild(elemName);

            // <requestValue> (optional)
            if (_requestValue != null)
            {
                XmlElement elemValue = doc.CreateElement("requestValue", DsmlConstants.DsmlUri);
                elemValue.InnerText = System.Convert.ToBase64String(_requestValue);

                // attach the "xsi:type = xsd:base64Binary" attribute
                XmlAttribute attrXsiType = doc.CreateAttribute("xsi:type", DsmlConstants.XsiUri);
                attrXsiType.InnerText = "xsd:base64Binary";
                elemValue.Attributes.Append(attrXsiType);

                elem.AppendChild(elemValue);
            }

            return elem;
        }
    }

    public class SearchRequest : DirectoryRequest
    {
        //
        // Public
        //
        public SearchRequest()
        {
            _directoryAttributes = new StringCollection();
        }

        public SearchRequest(string distinguishedName,
                             XmlDocument filter,
                             SearchScope searchScope,
                             params string[] attributeList) : this()
        {
            _dn = distinguishedName;

            if (attributeList != null)
            {
                for (int i = 0; i < attributeList.Length; i++)
                    _directoryAttributes.Add(attributeList[i]);
            }

            // validate the scope parameter
            Scope = searchScope;

            Filter = filter;
        }

        public SearchRequest(string distinguishedName,
                             string ldapFilter,
                             SearchScope searchScope,
                             params string[] attributeList) : this()
        {
            _dn = distinguishedName;

            if (attributeList != null)
            {
                for (int i = 0; i < attributeList.Length; i++)
                    _directoryAttributes.Add(attributeList[i]);
            }

            Scope = searchScope;

            Filter = ldapFilter;
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return _dn;
            }

            set
            {
                _dn = value;
            }
        }

        public StringCollection Attributes
        {
            get
            {
                return _directoryAttributes;
            }
        }

        public object Filter
        {
            get
            {
                return _directoryFilter;
            }

            set
            {
                // do we need to validate the filter here?
                if ((value is string) || (value is XmlDocument) || (value == null))
                    _directoryFilter = value;
                else
                    throw new ArgumentException(Res.GetString(Res.ValidFilterType), "value");
            }
        }

        public SearchScope Scope
        {
            get
            {
                return _directoryScope;
            }

            set
            {
                if (value < SearchScope.Base || value > SearchScope.Subtree)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SearchScope));

                _directoryScope = value;
            }
        }

        public DereferenceAlias Aliases
        {
            get
            {
                return _directoryRefAlias;
            }

            set
            {
                if (value < DereferenceAlias.Never || value > DereferenceAlias.Always)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DereferenceAlias));

                _directoryRefAlias = value;
            }
        }

        public int SizeLimit
        {
            get
            {
                return _directorySizeLimit;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeSizeLimit), "value");
                }

                _directorySizeLimit = value;
            }
        }

        public TimeSpan TimeLimit
        {
            get
            {
                return _directoryTimeLimit;
            }

            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if (value.TotalSeconds > Int32.MaxValue)
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");

                _directoryTimeLimit = value;
            }
        }

        public bool TypesOnly
        {
            get
            {
                return _directoryTypesOnly;
            }

            set
            {
                _directoryTypesOnly = value;
            }
        }

        //
        // Private/protected
        //

        private string _dn = null;
        private StringCollection _directoryAttributes = new StringCollection();
        private object _directoryFilter = null;
        private SearchScope _directoryScope = SearchScope.Subtree;
        private DereferenceAlias _directoryRefAlias = DereferenceAlias.Never;
        private int _directorySizeLimit = 0;
        private TimeSpan _directoryTimeLimit = new TimeSpan(0);
        private bool _directoryTypesOnly = false;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "searchRequest", true, _dn);

            // attach the "scope" attribute (required)
            XmlAttribute attrScope = doc.CreateAttribute("scope", null);

            switch (_directoryScope)
            {
                case SearchScope.Subtree:
                    attrScope.InnerText = "wholeSubtree";
                    break;

                case SearchScope.OneLevel:
                    attrScope.InnerText = "singleLevel";
                    break;

                case SearchScope.Base:
                    attrScope.InnerText = "baseObject";
                    break;

                default:
                    Debug.Assert(false, "Unknown DsmlSearchScope type");
                    break;
            }

            elem.Attributes.Append(attrScope);

            // attach the "derefAliases" attribute (required)
            XmlAttribute attrDerefAliases = doc.CreateAttribute("derefAliases", null);

            switch (_directoryRefAlias)
            {
                case DereferenceAlias.Never:
                    attrDerefAliases.InnerText = "neverDerefAliases";
                    break;

                case DereferenceAlias.InSearching:
                    attrDerefAliases.InnerText = "derefInSearching";
                    break;

                case DereferenceAlias.FindingBaseObject:
                    attrDerefAliases.InnerText = "derefFindingBaseObj";
                    break;

                case DereferenceAlias.Always:
                    attrDerefAliases.InnerText = "derefAlways";
                    break;

                default:
                    Debug.Assert(false, "Unknown DsmlDereferenceAlias type");
                    break;
            }

            elem.Attributes.Append(attrDerefAliases);

            // attach the "sizeLimit" attribute (optional)
            XmlAttribute attrSizeLimit = doc.CreateAttribute("sizeLimit", null);
            attrSizeLimit.InnerText = _directorySizeLimit.ToString(CultureInfo.InvariantCulture);
            elem.Attributes.Append(attrSizeLimit);

            // attach the "timeLimit" attribute (optional)
            XmlAttribute attrTimeLimit = doc.CreateAttribute("timeLimit", null);
            attrTimeLimit.InnerText = (_directoryTimeLimit.Ticks / TimeSpan.TicksPerSecond).ToString(CultureInfo.InvariantCulture);
            elem.Attributes.Append(attrTimeLimit);

            // attach the "typesOnly" attribute (optional, defaults to false)
            XmlAttribute attrTypesOnly = doc.CreateAttribute("typesOnly", null);
            attrTypesOnly.InnerText = _directoryTypesOnly ? "true" : "false";
            elem.Attributes.Append(attrTypesOnly);

            // add in the <filter> element (required)
            XmlElement elemFilter = doc.CreateElement("filter", DsmlConstants.DsmlUri);

            if (Filter != null)
            {
                StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

                try
                {
                    if (Filter is XmlDocument)
                    {
                        if (((XmlDocument)Filter).NamespaceURI.Length == 0)
                        {
                            // namespaceURI is not explicitly specified
                            CopyFilter((XmlDocument)Filter, xmlWriter);
                            elemFilter.InnerXml = stringWriter.ToString();
                        }
                        else
                        {
                            elemFilter.InnerXml = ((XmlDocument)Filter).OuterXml;
                        }
                    }
                    else if (Filter is string)
                    {
                        //
                        // Search Filter
                        // We make use of the code from DSDE, which requires an intermediary
                        // trip through the ADFilter representation.  Although ADFilter is unnecessary
                        // for our purposes, this enables us to use the same exact code as
                        // DSDE, without having to maintain a second copy of it.
                        //

                        // AD filter currently does not support filter without paranthesis, so adding it explicitly
                        string tempFilter = (string)Filter;
                        if (!tempFilter.StartsWith("(", StringComparison.Ordinal) && !tempFilter.EndsWith(")", StringComparison.Ordinal))
                        {
                            tempFilter = tempFilter.Insert(0, "(");
                            tempFilter = String.Concat(tempFilter, ")");
                        }

                        // Convert LDAP filter string to ADFilter representation
                        ADFilter adfilter = FilterParser.ParseFilterString(tempFilter);

                        if (adfilter == null)
                        {
                            // The LDAP filter string didn't parse correctly
                            throw new ArgumentException(Res.GetString(Res.BadSearchLDAPFilter));
                        }

                        // Convert ADFilter representation to a DSML filter string
                        //   Ideally, we'd skip the intemediary string, but the DSDE conversion
                        //   routines expect a XmlWriter, and the only XmlWriter available
                        //   is the XmlTextWriter, which produces text.                    
                        DSMLFilterWriter filterwriter = new DSMLFilterWriter();

                        filterwriter.WriteFilter(adfilter, false, xmlWriter, DsmlConstants.DsmlUri);

                        elemFilter.InnerXml = stringWriter.ToString();
                    }
                    else
                        Debug.Assert(false, "Unknown filter type");
                }
                finally
                {
                    // close this stream and the underlying stream.
                    xmlWriter.Close();
                }
            }
            else
            {
                // default filter: (objectclass=*)
                elemFilter.InnerXml = DsmlConstants.DefaultSearchFilter;
            }

            elem.AppendChild(elemFilter);

            // add in the <attributes> element (optional)
            if (_directoryAttributes != null && _directoryAttributes.Count != 0)
            {
                // create and attach the <attributes> element
                XmlElement elemAttributes = doc.CreateElement("attributes", DsmlConstants.DsmlUri);
                elem.AppendChild(elemAttributes);

                // create and attach the <attribute> elements under the <attributes> element
                foreach (string attrName in _directoryAttributes)
                {
                    // DsmlAttribute objects know how to persist themself in the right
                    // XML format, so we'll make use of that here rather than
                    // duplicating the code
                    DirectoryAttribute attr = new DirectoryAttribute();
                    attr.Name = attrName;
                    XmlElement elemAttr = attr.ToXmlNode(doc, "attribute");
                    elemAttributes.AppendChild(elemAttr);
                }
            }

            return elem;
        }

        private void CopyFilter(XmlNode node, XmlTextWriter writer)
        {
            for (XmlNode n = node.FirstChild; n != null; n = n.NextSibling)
                if (n != null)
                    CopyXmlTree(n, writer);
        }

        private void CopyXmlTree(XmlNode node, XmlTextWriter writer)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(node.LocalName, DsmlConstants.DsmlUri);
                    foreach (XmlAttribute att in node.Attributes)
                    {
                        writer.WriteAttributeString(att.LocalName, att.Value);
                    }

                    for (XmlNode n = node.FirstChild; n != null; n = n.NextSibling)
                    {
                        CopyXmlTree(n, writer);
                    }
                    writer.WriteEndElement();

                    break;
                default:
                    writer.WriteRaw(node.OuterXml);
                    break;
            }
        }
    }
}

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;

    public class DsmlAuthRequest : DirectoryRequest
    {
        private string _directoryPrincipal = "";

        public DsmlAuthRequest() { }

        public DsmlAuthRequest(string principal)
        {
            _directoryPrincipal = principal;
        }

        public string Principal
        {
            get
            {
                return _directoryPrincipal;
            }
            set
            {
                _directoryPrincipal = value;
            }
        }

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "authRequest", false, null);

            XmlAttribute attrPrincipal = doc.CreateAttribute("principal", null);
            attrPrincipal.InnerText = Principal;
            elem.Attributes.Append(attrPrincipal);

            return elem;
        }
    }
}
