// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public class XmlSchemaAnyAttribute : XmlSchemaAnnotated
    {
        private string _ns;
        private XmlSchemaContentProcessing _processContents = XmlSchemaContentProcessing.None;
        private NamespaceList _namespaceList;

        [XmlAttribute("namespace")]
        public string Namespace
        {
            get { return _ns ?? NamespaceList.ToString(); }
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
        internal XmlSchemaContentProcessing ProcessContentsCorrect
        {
            get { return _processContents == XmlSchemaContentProcessing.None ? XmlSchemaContentProcessing.Strict : _processContents; }
        }

        internal void BuildNamespaceList(string targetNamespace)
        {
            if (_ns != null)
            {
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

        internal static bool IsSubset(XmlSchemaAnyAttribute sub, XmlSchemaAnyAttribute super)
        {
            return NamespaceList.IsSubset(sub.NamespaceList, super.NamespaceList);
        }

        internal static XmlSchemaAnyAttribute Intersection(XmlSchemaAnyAttribute o1, XmlSchemaAnyAttribute o2, bool v1Compat)
        {
            NamespaceList nsl = NamespaceList.Intersection(o1.NamespaceList, o2.NamespaceList, v1Compat);
            if (nsl != null)
            {
                XmlSchemaAnyAttribute anyAttribute = new XmlSchemaAnyAttribute();
                anyAttribute._namespaceList = nsl;
                anyAttribute.ProcessContents = o1.ProcessContents;
                anyAttribute.Annotation = o1.Annotation;
                return anyAttribute;
            }
            else
            {
                // not expressible
                return null;
            }
        }

        internal static XmlSchemaAnyAttribute Union(XmlSchemaAnyAttribute o1, XmlSchemaAnyAttribute o2, bool v1Compat)
        {
            NamespaceList nsl = NamespaceList.Union(o1.NamespaceList, o2.NamespaceList, v1Compat);
            if (nsl != null)
            {
                XmlSchemaAnyAttribute anyAttribute = new XmlSchemaAnyAttribute();
                anyAttribute._namespaceList = nsl;
                anyAttribute._processContents = o1._processContents;
                anyAttribute.Annotation = o1.Annotation;
                return anyAttribute;
            }
            else
            {
                // not expressible
                return null;
            }
        }
    }
}
