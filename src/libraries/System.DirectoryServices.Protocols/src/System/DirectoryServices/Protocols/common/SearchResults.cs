//------------------------------------------------------------------------------
// <copyright file="SearchResults.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
    using System;
    using System.Net;
    using System.Xml;
    using System.IO;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    
    public class SearchResultReference 
    {
        private XmlNode dsmlNode = null;
        private XmlNamespaceManager dsmlNS = null;
        private bool dsmlRequest = false;

        private Uri[] resultReferences = null;
        private DirectoryControl[] resultControls = null;
        
        internal SearchResultReference(XmlNode node)
        {
            Debug.Assert(node != null);
        
            dsmlNode = node;

            dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();            

            dsmlRequest = true;
        }

        internal SearchResultReference(Uri[] uris)
        {
            resultReferences = uris;
        }

        public Uri[] Reference {
            get {
                if(dsmlRequest && (resultReferences == null))
                    resultReferences = UriHelper();
                
                if(resultReferences == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempUri = new Uri[resultReferences.Length];
                    for(int i = 0; i < resultReferences.Length; i++)
                    {
                        tempUri[i] = new Uri(resultReferences[i].AbsoluteUri);                            
                    }
                    return tempUri;
                }                
            }
        }

        public DirectoryControl[] Controls
        {
            get {
                DirectoryControl[] controls = null;
                if(dsmlRequest && resultControls == null)
                {
                    resultControls = ControlsHelper();                    
                }
                
                if(resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[resultControls.Length];
                    for(int i = 0; i < resultControls.Length; i++)
                    {                        
                        controls[i] = new DirectoryControl(resultControls[i].Type, resultControls[i].GetValue(), resultControls[i].IsCritical, resultControls[i].ServerSide);
                    }                        
                }    

                DirectoryControl.TransformControls(controls);                

                return controls;
                
            }
        }

        private Uri[] UriHelper()
        {
            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:ref", dsmlNS);

            if (nodeList.Count == 0)
            {
                // the server returned no controls
                return new Uri[0];
            }

            Uri[] references = new Uri[nodeList.Count];
            int index = 0;

            foreach (XmlNode node in nodeList)
            {
                Debug.Assert(node is XmlElement);
            
                references[index] = new Uri((string)node.InnerText);
                index++;
            }

            return references;
        }

        private DirectoryControl[] ControlsHelper()
        {
            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:control", dsmlNS);

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
    }

    public class SearchResultReferenceCollection :ReadOnlyCollectionBase
    {
        internal SearchResultReferenceCollection ()
        {            
        }

        public SearchResultReference this[int index] {
            get {
                return (SearchResultReference) InnerList[index];                                                 
            }
        }

        internal int Add(SearchResultReference reference)
        {
            return InnerList.Add(reference);
        }

        public bool Contains(SearchResultReference value) {
            return InnerList.Contains(value);
        }  

        public int IndexOf(SearchResultReference value) {
            return InnerList.IndexOf(value);
        } 

        public void CopyTo(SearchResultReference[] values, int index) {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
        
        
    }

    public class SearchResultEntry {
        
        private XmlNode dsmlNode = null;
        private XmlNamespaceManager dsmlNS = null;
        private bool dsmlRequest = false;
        
        string distinguishedName = null;
        SearchResultAttributeCollection attributes = new SearchResultAttributeCollection();
        private DirectoryControl[] resultControls = null;
        
        internal SearchResultEntry(XmlNode node)
        {
            Debug.Assert(node != null);
        
            dsmlNode = node;

            dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();              

            dsmlRequest = true;
        }        

        internal SearchResultEntry(string dn, SearchResultAttributeCollection attrs)
        {
            distinguishedName = dn;
            attributes = attrs;
        }

        internal SearchResultEntry(string dn)
        {
            distinguishedName = dn;
        }

        public string DistinguishedName {
            get {
                if(dsmlRequest && distinguishedName == null)
                    distinguishedName = DNHelper("@dsml:dn", "@dn");

                return distinguishedName;
            }
        }

        public SearchResultAttributeCollection Attributes {
            get {
                if(dsmlRequest && (attributes.Count == 0))
                    attributes = AttributesHelper();

                return attributes;
            }
        }

        public DirectoryControl[] Controls
        {
            get {
                DirectoryControl[] controls = null;
                if(dsmlRequest && (resultControls == null))
                {
                    resultControls = ControlsHelper();                    
                }

                if(resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[resultControls.Length];
                    for(int i = 0; i < resultControls.Length; i++)
                    {                        
                        controls[i] = new DirectoryControl(resultControls[i].Type, resultControls[i].GetValue(), resultControls[i].IsCritical, resultControls[i].ServerSide);
                    }                        
                } 

                DirectoryControl.TransformControls(controls);                

                return controls;

                
            }
        }


        private string DNHelper(string primaryXPath, string secondaryXPath)
        {
            XmlAttribute attrDN = (XmlAttribute) dsmlNode.SelectSingleNode(primaryXPath, dsmlNS);

            if (attrDN == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrDN = (XmlAttribute) dsmlNode.SelectSingleNode(secondaryXPath, dsmlNS);

                if (attrDN == null)
                {
                    // the element doesn't have a associated dn
                    throw new DsmlInvalidDocumentException(Res.GetString(Res.MissingSearchResultEntryDN));
                }

                return attrDN.Value;
            }
            else
            {
                return attrDN.Value;
            }
        }

        private SearchResultAttributeCollection AttributesHelper()
        {
            SearchResultAttributeCollection attributes = new SearchResultAttributeCollection();

            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:attr", dsmlNS);

            if (nodeList.Count != 0)
            {
                foreach (XmlNode node in nodeList)
                {
                    Debug.Assert(node is XmlElement);

                    DirectoryAttribute attribute = new DirectoryAttribute((XmlElement) node);
                    attributes.Add(attribute.Name, attribute);            
                }
            }

            return attributes;
        }

        private DirectoryControl[] ControlsHelper()
        {
            XmlNodeList nodeList = dsmlNode.SelectNodes("dsml:control", dsmlNS);

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
        
        
    }

    public class SearchResultEntryCollection :ReadOnlyCollectionBase {
        
        internal SearchResultEntryCollection ()
        {            
        }

        public SearchResultEntry this[int index] {
            get {
                return (SearchResultEntry) InnerList[index];                                                 
            }
        }

        internal int Add(SearchResultEntry entry)
        {
            return InnerList.Add(entry);
        }

        public bool Contains(SearchResultEntry value) {
            return InnerList.Contains(value);
        }  

        public int IndexOf(SearchResultEntry value) {
            return InnerList.IndexOf(value);
        } 

        public void CopyTo(SearchResultEntry[] values, int index) {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }

    }
}

