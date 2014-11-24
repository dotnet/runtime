// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal abstract class BaseAxisQuery : Query
    {
        internal Query qyInput;
        bool nameTest;
        string name;
        string prefix;
        string nsUri;
        XPathNodeType typeTest;

        // these two things are the state of this class
        // that need to be reset whenever the context changes.
        protected XPathNavigator currentNode;
        protected int position;

        protected BaseAxisQuery(Query qyInput)
        {
            this.name = string.Empty;
            this.prefix = string.Empty;
            this.nsUri = string.Empty;
            this.qyInput = qyInput;
        }
        protected BaseAxisQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
        {
            Debug.Assert(qyInput != null);
            this.qyInput = qyInput;
            this.name = name;
            this.prefix = prefix;
            this.typeTest = typeTest;
            this.nameTest = prefix.Length != 0 || name.Length != 0;
            this.nsUri = string.Empty;
        }
        protected BaseAxisQuery(BaseAxisQuery other) : base(other)
        {
            this.qyInput = Clone(other.qyInput);
            this.name = other.name;
            this.prefix = other.prefix;
            this.nsUri = other.nsUri;
            this.typeTest = other.typeTest;
            this.nameTest = other.nameTest;
            this.position = other.position;
            this.currentNode = other.currentNode;
        }

        public override void Reset()
        {
            position = 0;
            currentNode = null; // After this current will not point to context node from Evaluate() call
                                // But this is ok, because there is no public Reset() on XPathNodeIterator
            qyInput.Reset();
        }

        public override void SetXsltContext(XsltContext context)
        {
            Debug.Assert(context != null);
            nsUri = context.LookupNamespace(prefix);
            qyInput.SetXsltContext(context);
        }

        protected string Name { get { return name; } }
        protected string Prefix { get { return prefix; } }
        protected string Namespace { get { return nsUri; } }
        protected bool NameTest { get { return nameTest; } }
        protected XPathNodeType TypeTest { get { return typeTest; } }

        public override int CurrentPosition { get { return position; } }
        public override XPathNavigator Current { get { return currentNode; } }

        public virtual bool matches(XPathNavigator e)
        {
            if (
                TypeTest == e.NodeType ||
                TypeTest == XPathNodeType.All ||
                TypeTest == XPathNodeType.Text && (e.NodeType == XPathNodeType.Whitespace || e.NodeType == XPathNodeType.SignificantWhitespace)
            )
            {
                if (NameTest)
                {
                    if (name.Equals(e.LocalName) || name.Length == 0)
                    {
                        if (nsUri.Equals(e.NamespaceURI))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            ResetCount();
            Reset();
            qyInput.Evaluate(nodeIterator);
            AssertQuery(qyInput);
            return this;
        }

        public override double XsltDefaultPriority
        {
            get
            {
                if (qyInput.GetType() != typeof(ContextQuery))
                {
                    return 0.5;   // a/b a[b] id('s')/a
                }
                Debug.Assert(this is AttributeQuery || this is ChildrenQuery);
                if (name.Length != 0)
                {
                    return 0; // p:foo, foo, processing-instruction("foo")
                }
                if (prefix.Length != 0)
                {
                    return -0.25; // p:*
                }
                return -0.5; // *, text(), node()
            }
        }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            if (NameTest)
            {
                w.WriteAttributeString("name", Prefix.Length != 0 ? Prefix + ':' + Name : Name);
            }
            if (TypeTest != XPathNodeType.Element)
            {
                w.WriteAttributeString("nodeType", TypeTest.ToString());
            }
            qyInput.PrintQuery(w);
            w.WriteEndElement();
        }
    }
}
