//------------------------------------------------------------------------------
// <copyright file="DirectoryRequest" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
    
namespace System.DirectoryServices.Protocols {

    using System;
    using System.Xml;
    using System.Collections;    
    using System.IO;
    using System.Diagnostics;
    using System.Globalization;
    using System.ComponentModel;    
    using System.Collections.Specialized;    
    using System.Security.Permissions;
    
    public abstract class DirectoryRequest  :DirectoryOperation {        
        internal DirectoryControlCollection directoryControlCollection = null;
        
        internal DirectoryRequest()  
        {
            Utility.CheckOSVersion();
            
            directoryControlCollection = new DirectoryControlCollection();
        }        

        public string RequestId {
            get {
                return directoryRequestID;
            }
            set {
                directoryRequestID = value;
            }
        }        
        
        public DirectoryControlCollection Controls
        {
            get {               
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

        public DeleteRequest() {}

        public DeleteRequest(string distinguishedName)
        {        
            dn= distinguishedName;
        }

        // Member properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }


        //
        // Private/protected
        //

        private string dn;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {

            XmlElement elem = CreateRequestElement(doc, "delRequest", true, dn);

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
            attributeList = new DirectoryAttributeCollection();
        }

        public AddRequest(string distinguishedName, params DirectoryAttribute[] attributes) : this()
        {
            // Store off the distinguished name
            dn = distinguishedName;

            if(attributes != null)
            {
                for(int i = 0; i < attributes.Length; i++)
                {
                    attributeList.Add(attributes[i]);
                }
            }
        }

        public AddRequest(string distinguishedName, string objectClass) : this()
        {
            // parameter validation
            if (objectClass == null)
                throw new ArgumentNullException("objectClass");
        
            // Store off the distinguished name
            dn = distinguishedName;

            // Store off the objectClass in an object class attribute
            DirectoryAttribute objClassAttr = new DirectoryAttribute();
            
            objClassAttr.Name = "objectClass";
            objClassAttr.Add(objectClass);
            attributeList.Add(objClassAttr);
        }

        // Properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }

        public DirectoryAttributeCollection Attributes
        {
            get
            {
                return attributeList;
            }            
        }


        //
        // Private/protected
        //

        private string dn;
        private DirectoryAttributeCollection attributeList;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {

            XmlElement elem = CreateRequestElement(doc, "addRequest", true, dn);

            // Add in the attributes
            if(attributeList != null)
            {
                foreach (DirectoryAttribute attr in attributeList)
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
            attributeModificationList = new DirectoryAttributeModificationCollection();
        }

        public ModifyRequest(string distinguishedName, params DirectoryAttributeModification[] modifications) : this()
        {
            // Store off the distinguished name
            dn = distinguishedName;

            // Store off the initial list of modifications
            attributeModificationList.AddRange(modifications);
        }

        public ModifyRequest(string distinguishedName, DirectoryAttributeOperation operation, string attributeName, params object[] values) :this()
        {
            // Store off the distinguished name
            dn = distinguishedName;

            // validate the attributeName
            if(attributeName == null)
                throw new ArgumentNullException("attributeName");            

            DirectoryAttributeModification mod = new DirectoryAttributeModification();
            mod.Operation = operation;
            mod.Name = attributeName;
            if(values != null)
            {
                for(int i = 0; i < values.Length; i++)
                    mod.Add(values[i]);
            }

            attributeModificationList.Add(mod);
            
        }


        // Properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }

        public DirectoryAttributeModificationCollection Modifications
        {
            get
            {
                return attributeModificationList;
            }           
        }

        
        //
        // Private/protected
        //
        
        private string dn;
        private DirectoryAttributeModificationCollection attributeModificationList;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "modifyRequest", true, dn);

            // Add in the attributes

            if(attributeModificationList != null)
            {
                foreach (DirectoryAttributeModification attr in attributeModificationList)
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
        public CompareRequest() {}

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
            if(assertion == null)
                throw new ArgumentNullException("assertion");

            if(assertion.Count != 1)
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
            dn = distinguishedName;

            
            // store off the attribute name and value            
            attribute.Name = attributeName;
            attribute.Add(value);
        }




        // Properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }

        public DirectoryAttribute Assertion
        {
            get
            {
                return attribute;
            }            
        }

        
        //
        // Private/protected
        //
        
        private string dn;
        private DirectoryAttribute attribute= new DirectoryAttribute();

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "compareRequest", true, dn);

            // add in the attribute
            if (attribute.Count != 1)
                throw new ArgumentException(Res.GetString(Res.WrongNumValuesCompare));
            
            XmlElement elAttr = attribute.ToXmlNode(doc, "assertion");
            elem.AppendChild(elAttr);
                
            return elem;
        }
    }

    public class ModifyDNRequest  : DirectoryRequest
    {
        //
        // Public
        //
        public ModifyDNRequest () {}

        public ModifyDNRequest (string distinguishedName,
                                string newParentDistinguishedName,
                                string newName)
        {

            // store off the DN
            dn = distinguishedName;

            newSuperior = newParentDistinguishedName;
            newRDN = newName;
        }


        // Properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }

        public string NewParentDistinguishedName
        {
            get
            {
                return newSuperior;
            }

            set
            {
                newSuperior = value;
            }
        }

        public string NewName
        {
            get
            {
                return newRDN;
            }

            set
            {
                newRDN = value;
            }
        }

        public bool DeleteOldRdn
        {
            get
            {
                return deleteOldRDN;
            }

            set
            {
                deleteOldRDN = value;
            }
        }        

        
        //
        // Private/protected
        //
        
        private string dn;
        private string newSuperior;
        private string newRDN;
        private bool deleteOldRDN = true;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "modDNRequest", true, dn);

            // "newrdn" attribute (required)
            XmlAttribute attrNewRDN = doc.CreateAttribute("newrdn", null);
            attrNewRDN.InnerText = newRDN;
            elem.Attributes.Append(attrNewRDN);

            // "deleteoldrdn" attribute (optional, but we'll always include it)
            XmlAttribute attrDeleteOldRDN = doc.CreateAttribute("deleteoldrdn", null);
            attrDeleteOldRDN.InnerText = deleteOldRDN ? "true" : "false";
            elem.Attributes.Append(attrDeleteOldRDN);

			
            // "newSuperior" attribute (optional)
            if (newSuperior != null)
            {
                XmlAttribute attrNewSuperior = doc.CreateAttribute("newSuperior", null);
                attrNewSuperior.InnerText = newSuperior;
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
        public ExtendedRequest() {}

        public ExtendedRequest(string requestName)
        {
            this.requestName = requestName;
        }

        public ExtendedRequest(string requestName, byte[] requestValue) : this(requestName)
        {
            this.requestValue = requestValue;
        }


        // Properties
        public string RequestName
        {
            get
            {
                return this.requestName;
            }

            set
            {
                this.requestName = value;
            }
        }

        public byte[] RequestValue
        {
            get
            {
                if(this.requestValue == null)
                    return new byte[0];
                else
                {
                    byte[] tempValue = new byte[this.requestValue.Length];
                    for(int i = 0; i < this.requestValue.Length; i++)
                        tempValue[i] = this.requestValue[i];

                    return tempValue;
                }
            }

            set
            {
                this.requestValue = value;
            }
        }

        
        //
        // Private/protected
        //
        
        private string requestName;
        private byte[] requestValue = null;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "extendedRequest", false, null);

            // <requestName>
            XmlElement elemName = doc.CreateElement("requestName", DsmlConstants.DsmlUri);
            elemName.InnerText = requestName;
            elem.AppendChild(elemName);
            

            // <requestValue> (optional)
            if (requestValue != null)
            {
                XmlElement elemValue = doc.CreateElement("requestValue", DsmlConstants.DsmlUri);
                elemValue.InnerText = System.Convert.ToBase64String(requestValue);

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
            directoryAttributes = new StringCollection();
        }

        public SearchRequest(string distinguishedName,
                             XmlDocument filter,
                             SearchScope searchScope,
                             params string[] attributeList) :this()
        {
            dn = distinguishedName;
            
            if(attributeList != null)
            {                
                for(int i = 0; i < attributeList.Length; i++)
                    directoryAttributes.Add(attributeList[i]);
            }

            // validate the scope parameter
            Scope = searchScope;

            Filter = filter;
        }

        public SearchRequest(string distinguishedName,
                             string ldapFilter,                             
                             SearchScope searchScope,
                             params string[] attributeList) :this()
        {
            dn = distinguishedName;

            if(attributeList != null)
            {                
                for(int i = 0; i < attributeList.Length; i++)
                    directoryAttributes.Add(attributeList[i]);
            }

            Scope = searchScope;

            Filter = ldapFilter;
            
        }


        // Properties
        public string DistinguishedName
        {
            get
            {
                return dn;
            }

            set
            {
                dn = value;
            }
        }

        public StringCollection Attributes
        {
            get
            {
                return directoryAttributes;
            }            
        }

        public object Filter
        {
            get
            {
                return directoryFilter;
            }

            set
            {
                // do we need to validate the filter here?
                if((value is string) || (value is XmlDocument) || (value == null))
                    directoryFilter = value;
                else
                    throw new ArgumentException(Res.GetString(Res.ValidFilterType), "value");
            }
        }

        public SearchScope Scope
        {
            get
            {
                return directoryScope;
            }

            set
            {
                if (value < SearchScope.Base || value > SearchScope.Subtree) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SearchScope));
            
                directoryScope = value;
            }
        }    

        public DereferenceAlias Aliases
        {
            get
            {
                return directoryRefAlias;
            }

            set
            {
                if (value < DereferenceAlias.Never || value > DereferenceAlias.Always) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DereferenceAlias));
            
                directoryRefAlias = value;
            }
        }     

        public int SizeLimit
        {
            get
            {
                return directorySizeLimit;
            }

            set
            {         
                if (value < 0)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeSizeLimit), "value");
                }
            
                directorySizeLimit = value;
            }
        }     

        public TimeSpan TimeLimit
        {
            get
            {
                return directoryTimeLimit;
            }

            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                // prevent integer overflow
                if(value.TotalSeconds > Int32.MaxValue)
                    throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
            
                directoryTimeLimit = value;
            }
        }      

        public bool TypesOnly
        {
            get
            {
                return directoryTypesOnly;
            }

            set
            {
                directoryTypesOnly = value;
            }
        }          

        
        //
        // Private/protected
        //

        private string dn = null;
        private StringCollection directoryAttributes = new StringCollection();
        private object directoryFilter = null;
        private SearchScope directoryScope = SearchScope.Subtree;
        private DereferenceAlias directoryRefAlias = DereferenceAlias.Never;
        private int directorySizeLimit = 0;
        private TimeSpan directoryTimeLimit = new TimeSpan(0);
        private bool directoryTypesOnly = false;

        protected override XmlElement ToXmlNode(XmlDocument doc)
        {
            XmlElement elem = CreateRequestElement(doc, "searchRequest", true, dn);            


            // attach the "scope" attribute (required)
            XmlAttribute attrScope = doc.CreateAttribute("scope", null);

            switch (directoryScope)
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

            switch (directoryRefAlias)
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
            attrSizeLimit.InnerText = directorySizeLimit.ToString(CultureInfo.InvariantCulture);
            elem.Attributes.Append(attrSizeLimit);            

            // attach the "timeLimit" attribute (optional)
            XmlAttribute attrTimeLimit = doc.CreateAttribute("timeLimit", null);
            attrTimeLimit.InnerText = (directoryTimeLimit.Ticks / TimeSpan.TicksPerSecond).ToString(CultureInfo.InvariantCulture);
            elem.Attributes.Append(attrTimeLimit);            
            
            // attach the "typesOnly" attribute (optional, defaults to false)
            XmlAttribute attrTypesOnly = doc.CreateAttribute("typesOnly", null);
            attrTypesOnly.InnerText = directoryTypesOnly ? "true" : "false";
            elem.Attributes.Append(attrTypesOnly);            

            // add in the <filter> element (required)
            XmlElement elemFilter = doc.CreateElement("filter", DsmlConstants.DsmlUri);

            if (Filter != null)
            {
                StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);

                try
                {
                    
                    if(Filter is XmlDocument)
                    {
                        if(((XmlDocument)Filter).NamespaceURI.Length == 0)
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
                    else if(Filter is string)
                    {
                        //
                        // Search Filter
                        // We make use of the code from DSDE, which requires an intermediary
                        // trip through the ADFilter representation.  Although ADFilter is unnecessary
                        // for our purposes, this enables us to use the same exact code as
                        // DSDE, without having to maintain a second copy of it.
                        //

                        // AD filter currently does not support filter without paranthesis, so adding it explicitly
                        string tempFilter = (string) Filter;
                        if(!tempFilter.StartsWith("(", StringComparison.Ordinal) && !tempFilter.EndsWith(")", StringComparison.Ordinal))
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
            if (directoryAttributes != null && directoryAttributes.Count != 0)
            {
                // create and attach the <attributes> element
                XmlElement elemAttributes = doc.CreateElement("attributes", DsmlConstants.DsmlUri);
                elem.AppendChild(elemAttributes);

                // create and attach the <attribute> elements under the <attributes> element
                foreach (string attrName in directoryAttributes)
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
            for ( XmlNode n = node.FirstChild; n != null; n = n.NextSibling ) 
                if(n != null)
                    CopyXmlTree(n, writer);
        }

        private void CopyXmlTree(XmlNode node, XmlTextWriter writer)
        {
            switch(node.NodeType)
            {
                case XmlNodeType.Element:                    
                    writer.WriteStartElement(node.LocalName, DsmlConstants.DsmlUri);                    
                    foreach(XmlAttribute att in node.Attributes)
                    {                        
                        writer.WriteAttributeString(att.LocalName, att.Value);
                    }

                    for ( XmlNode n = node.FirstChild; n != null; n = n.NextSibling ) 
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

namespace System.DirectoryServices.Protocols {
    
    using System;
    using System.Xml;
    
    public class DsmlAuthRequest :DirectoryRequest {
        private string directoryPrincipal = "";

        public DsmlAuthRequest() {}

        public DsmlAuthRequest(string principal)
        {
            this.directoryPrincipal = principal;
        }

        public string Principal {
            get {
                return directoryPrincipal;
            }
            set {
                directoryPrincipal = value;
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
