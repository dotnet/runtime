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

    public class SearchResultReference
    {
        private XmlNode _dsmlNode = null;
        private XmlNamespaceManager _dsmlNS = null;
        private bool _dsmlRequest = false;

        private Uri[] _resultReferences = null;
        private DirectoryControl[] _resultControls = null;

        internal SearchResultReference(XmlNode node)
        {
            Debug.Assert(node != null);

            _dsmlNode = node;

            _dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();

            _dsmlRequest = true;
        }

        internal SearchResultReference(Uri[] uris)
        {
            _resultReferences = uris;
        }

        public Uri[] Reference
        {
            get
            {
                if (_dsmlRequest && (_resultReferences == null))
                    _resultReferences = UriHelper();

                if (_resultReferences == null)
                    return new Uri[0];
                else
                {
                    Uri[] tempUri = new Uri[_resultReferences.Length];
                    for (int i = 0; i < _resultReferences.Length; i++)
                    {
                        tempUri[i] = new Uri(_resultReferences[i].AbsoluteUri);
                    }
                    return tempUri;
                }
            }
        }

        public DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;
                if (_dsmlRequest && _resultControls == null)
                {
                    _resultControls = ControlsHelper();
                }

                if (_resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[_resultControls.Length];
                    for (int i = 0; i < _resultControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }

        private Uri[] UriHelper()
        {
            XmlNodeList nodeList = _dsmlNode.SelectNodes("dsml:ref", _dsmlNS);

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
            XmlNodeList nodeList = _dsmlNode.SelectNodes("dsml:control", _dsmlNS);

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

    public class SearchResultReferenceCollection : ReadOnlyCollectionBase
    {
        internal SearchResultReferenceCollection()
        {
        }

        public SearchResultReference this[int index]
        {
            get
            {
                return (SearchResultReference)InnerList[index];
            }
        }

        internal int Add(SearchResultReference reference)
        {
            return InnerList.Add(reference);
        }

        public bool Contains(SearchResultReference value)
        {
            return InnerList.Contains(value);
        }

        public int IndexOf(SearchResultReference value)
        {
            return InnerList.IndexOf(value);
        }

        public void CopyTo(SearchResultReference[] values, int index)
        {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
    }

    public class SearchResultEntry
    {
        private XmlNode _dsmlNode = null;
        private XmlNamespaceManager _dsmlNS = null;
        private bool _dsmlRequest = false;

        private string _distinguishedName = null;
        private SearchResultAttributeCollection _attributes = new SearchResultAttributeCollection();
        private DirectoryControl[] _resultControls = null;

        internal SearchResultEntry(XmlNode node)
        {
            Debug.Assert(node != null);

            _dsmlNode = node;

            _dsmlNS = NamespaceUtils.GetDsmlNamespaceManager();

            _dsmlRequest = true;
        }

        internal SearchResultEntry(string dn, SearchResultAttributeCollection attrs)
        {
            _distinguishedName = dn;
            _attributes = attrs;
        }

        internal SearchResultEntry(string dn)
        {
            _distinguishedName = dn;
        }

        public string DistinguishedName
        {
            get
            {
                if (_dsmlRequest && _distinguishedName == null)
                    _distinguishedName = DNHelper("@dsml:dn", "@dn");

                return _distinguishedName;
            }
        }

        public SearchResultAttributeCollection Attributes
        {
            get
            {
                if (_dsmlRequest && (_attributes.Count == 0))
                    _attributes = AttributesHelper();

                return _attributes;
            }
        }

        public DirectoryControl[] Controls
        {
            get
            {
                DirectoryControl[] controls = null;
                if (_dsmlRequest && (_resultControls == null))
                {
                    _resultControls = ControlsHelper();
                }

                if (_resultControls == null)
                    return new DirectoryControl[0];
                else
                {
                    controls = new DirectoryControl[_resultControls.Length];
                    for (int i = 0; i < _resultControls.Length; i++)
                    {
                        controls[i] = new DirectoryControl(_resultControls[i].Type, _resultControls[i].GetValue(), _resultControls[i].IsCritical, _resultControls[i].ServerSide);
                    }
                }

                DirectoryControl.TransformControls(controls);

                return controls;
            }
        }

        private string DNHelper(string primaryXPath, string secondaryXPath)
        {
            XmlAttribute attrDN = (XmlAttribute)_dsmlNode.SelectSingleNode(primaryXPath, _dsmlNS);

            if (attrDN == null)
            {
                // try it without the namespace qualifier, in case the sender omitted it
                attrDN = (XmlAttribute)_dsmlNode.SelectSingleNode(secondaryXPath, _dsmlNS);

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

            XmlNodeList nodeList = _dsmlNode.SelectNodes("dsml:attr", _dsmlNS);

            if (nodeList.Count != 0)
            {
                foreach (XmlNode node in nodeList)
                {
                    Debug.Assert(node is XmlElement);

                    DirectoryAttribute attribute = new DirectoryAttribute((XmlElement)node);
                    attributes.Add(attribute.Name, attribute);
                }
            }

            return attributes;
        }

        private DirectoryControl[] ControlsHelper()
        {
            XmlNodeList nodeList = _dsmlNode.SelectNodes("dsml:control", _dsmlNS);

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

    public class SearchResultEntryCollection : ReadOnlyCollectionBase
    {
        internal SearchResultEntryCollection()
        {
        }

        public SearchResultEntry this[int index]
        {
            get
            {
                return (SearchResultEntry)InnerList[index];
            }
        }

        internal int Add(SearchResultEntry entry)
        {
            return InnerList.Add(entry);
        }

        public bool Contains(SearchResultEntry value)
        {
            return InnerList.Contains(value);
        }

        public int IndexOf(SearchResultEntry value)
        {
            return InnerList.IndexOf(value);
        }

        public void CopyTo(SearchResultEntry[] values, int index)
        {
            InnerList.CopyTo(values, index);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
    }
}

