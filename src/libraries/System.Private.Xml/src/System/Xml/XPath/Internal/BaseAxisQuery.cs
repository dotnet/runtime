// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal abstract class BaseAxisQuery : Query
    {
        internal Query qyInput;
        private bool _nameTest;
        private string _name;
        private string _prefix;
        private string _nsUri;
        private XPathNodeType _typeTest;

        // these two things are the state of this class
        // that need to be reset whenever the context changes.
        protected XPathNavigator currentNode;
        protected int position;

        protected BaseAxisQuery(Query qyInput)
        {
            _name = string.Empty;
            _prefix = string.Empty;
            _nsUri = string.Empty;
            this.qyInput = qyInput;
        }
        protected BaseAxisQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
        {
            Debug.Assert(qyInput != null);
            this.qyInput = qyInput;
            _name = name;
            _prefix = prefix;
            _typeTest = typeTest;
            _nameTest = prefix.Length != 0 || name.Length != 0;
            _nsUri = string.Empty;
        }
        protected BaseAxisQuery(BaseAxisQuery other) : base(other)
        {
            this.qyInput = Clone(other.qyInput);
            _name = other._name;
            _prefix = other._prefix;
            _nsUri = other._nsUri;
            _typeTest = other._typeTest;
            _nameTest = other._nameTest;
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
            _nsUri = context.LookupNamespace(_prefix);
            qyInput.SetXsltContext(context);
        }

        protected string Name { get { return _name; } }
        protected string Namespace { get { return _nsUri; } }
        protected bool NameTest { get { return _nameTest; } }
        protected XPathNodeType TypeTest { get { return _typeTest; } }

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
                    if (_name.Equals(e.LocalName) || _name.Length == 0)
                    {
                        if (_nsUri.Equals(e.NamespaceURI))
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
                if (_name.Length != 0)
                {
                    return 0; // p:foo, foo, processing-instruction("foo")
                }
                if (_prefix.Length != 0)
                {
                    return -0.25; // p:*
                }
                return -0.5; // *, text(), node()
            }
        }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
    }
}
