// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.ComponentModel;
    using System.Xml.Serialization;
    using System.Text;

    public class XmlSchemaAny : XmlSchemaParticle
    {
        private string _ns;
        private XmlSchemaContentProcessing _processContents = XmlSchemaContentProcessing.None;
        private NamespaceList _namespaceList;

        [XmlAttribute("namespace")]
        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        [XmlAttribute("processContents"), DefaultValue(XmlSchemaContentProcessing.None)]
        public XmlSchemaContentProcessing ProcessContents
        {
            get { return _processContents; }
            set { _processContents = value; }
        }

        [XmlIgnore]
        internal NamespaceList NamespaceList
        {
            get { return _namespaceList; }
        }

        [XmlIgnore]
        internal string ResolvedNamespace
        {
            get
            {
                if (_ns == null || _ns.Length == 0)
                {
                    return "##any";
                }
                return _ns;
            }
        }

        [XmlIgnore]
        internal XmlSchemaContentProcessing ProcessContentsCorrect
        {
            get { return _processContents == XmlSchemaContentProcessing.None ? XmlSchemaContentProcessing.Strict : _processContents; }
        }

        internal override string NameString
        {
            get
            {
                switch (_namespaceList.Type)
                {
                    case NamespaceList.ListType.Any:
                        return "##any:*";

                    case NamespaceList.ListType.Other:
                        return "##other:*";

                    case NamespaceList.ListType.Set:
                        StringBuilder sb = new StringBuilder();
                        int i = 1;
                        foreach (string wildcardNS in _namespaceList.Enumerate)
                        {
                            sb.Append(wildcardNS + ":*");
                            if (i < _namespaceList.Enumerate.Count)
                            {
                                sb.Append(" ");
                            }
                            i++;
                        }
                        return sb.ToString();

                    default:
                        return string.Empty;
                }
            }
        }

        internal void BuildNamespaceList(string targetNamespace)
        {
            if (_ns != null)
            { //If namespace="" default to namespace="##any"
                _namespaceList = new NamespaceList(_ns, targetNamespace);
            }
            else
            {
                _namespaceList = new NamespaceList();
            }
        }

        internal void BuildNamespaceListV1Compat(string targetNamespace)
        {
            if (_ns != null)
            {
                _namespaceList = new NamespaceListV1Compat(_ns, targetNamespace);
            }
            else
            {
                _namespaceList = new NamespaceList(); //This is only ##any, hence base class is sufficient
            }
        }

        internal bool Allows(XmlQualifiedName qname)
        {
            return _namespaceList.Allows(qname.Namespace);
        }
    }
}
