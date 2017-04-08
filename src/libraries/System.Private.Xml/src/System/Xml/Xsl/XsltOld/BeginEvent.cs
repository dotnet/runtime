// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal class BeginEvent : Event
    {
        private XPathNodeType _nodeType;
        private string _namespaceUri;
        private string _name;
        private string _prefix;
        private bool _empty;
        private object _htmlProps;
#if DEBUG
        private bool _replaceNSAliasesDone;
#endif

        public BeginEvent(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            Debug.Assert(input != null);
            Debug.Assert(input.NodeType != XPathNodeType.Namespace);
            _nodeType = input.NodeType;
            _namespaceUri = input.NamespaceURI;
            _name = input.LocalName;
            _prefix = input.Prefix;
            _empty = input.IsEmptyTag;
            if (_nodeType == XPathNodeType.Element)
            {
                _htmlProps = HtmlElementProps.GetProps(_name);
            }
            else if (_nodeType == XPathNodeType.Attribute)
            {
                _htmlProps = HtmlAttributeProps.GetProps(_name);
            }
        }

        public override void ReplaceNamespaceAlias(Compiler compiler)
        {
#if DEBUG
            Debug.Assert(!_replaceNSAliasesDone, "Second attempt to replace NS aliases!. This bad.");
            _replaceNSAliasesDone = true;
#endif
            if (_nodeType == XPathNodeType.Attribute && _namespaceUri.Length == 0)
            {
                return; // '#default' aren't apply to attributes.
            }
            NamespaceInfo ResultURIInfo = compiler.FindNamespaceAlias(_namespaceUri);
            if (ResultURIInfo != null)
            {
                _namespaceUri = ResultURIInfo.nameSpace;
                if (ResultURIInfo.prefix != null)
                {
                    _prefix = ResultURIInfo.prefix;
                }
            }
        }

        public override bool Output(Processor processor, ActionFrame frame)
        {
            return processor.BeginEvent(_nodeType, _prefix, _name, _namespaceUri, _empty, _htmlProps, false);
        }
    }
}
