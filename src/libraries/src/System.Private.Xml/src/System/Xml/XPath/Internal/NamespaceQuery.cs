// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class NamespaceQuery : BaseAxisQuery
    {
        private bool _onNamespace;

        public NamespaceQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type) : base(qyParent, Name, Prefix, Type) { }
        private NamespaceQuery(NamespaceQuery other) : base(other)
        {
            _onNamespace = other._onNamespace;
        }

        public override void Reset()
        {
            _onNamespace = false;
            base.Reset();
        }

        public override XPathNavigator Advance()
        {
            while (true)
            {
                if (!_onNamespace)
                {
                    currentNode = qyInput.Advance();
                    if (currentNode == null)
                    {
                        return null;
                    }
                    position = 0;
                    currentNode = currentNode.Clone();
                    _onNamespace = currentNode.MoveToFirstNamespace();
                }
                else
                {
                    _onNamespace = currentNode.MoveToNextNamespace();
                }

                if (_onNamespace)
                {
                    if (matches(currentNode))
                    {
                        position++;
                        return currentNode;
                    }
                }
            } // while
        } // Advance

        public override bool matches(XPathNavigator e)
        {
            Debug.Assert(e.NodeType == XPathNodeType.Namespace);
            if (e.Value.Length == 0)
            {
                Debug.Assert(e.LocalName.Length == 0, "Only xmlns='' can have empty string as a value");
                // Namespace axes never returns xmlns='', 
                // because it's not a NS declaration but rather undeclaration.
                return false;
            }
            if (NameTest)
            {
                return Name.Equals(e.LocalName);
            }
            else
            {
                return true;
            }
        }

        public override XPathNodeIterator Clone() { return new NamespaceQuery(this); }
    }
}
