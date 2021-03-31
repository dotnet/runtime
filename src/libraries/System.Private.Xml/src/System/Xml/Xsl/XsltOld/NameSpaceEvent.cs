// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class NamespaceEvent : Event
    {
        private string _namespaceUri;
        private string _name;

        public NamespaceEvent(NavigatorInput input)
        {
            Debug.Assert(input != null);
            Debug.Assert(input.NodeType == XPathNodeType.Namespace);
            _namespaceUri = input.Value;
            _name = input.LocalName;
        }

        public override void ReplaceNamespaceAlias(Compiler compiler)
        {
            if (_namespaceUri.Length != 0)
            { // Do we need to check this for namespace?
                NamespaceInfo? ResultURIInfo = compiler.FindNamespaceAlias(_namespaceUri);
                if (ResultURIInfo != null)
                {
                    _namespaceUri = ResultURIInfo.nameSpace!;
                    if (ResultURIInfo.prefix != null)
                    {
                        _name = ResultURIInfo.prefix;
                    }
                }
            }
        }

        public override bool Output(Processor processor, ActionFrame frame)
        {
            bool res;
            res = processor.BeginEvent(XPathNodeType.Namespace, /*prefix:*/null, _name, _namespaceUri, /*empty:*/false);
            Debug.Assert(res); // Namespace node as any other attribute can't fail because it doesn't signal record change
            res = processor.EndEvent(XPathNodeType.Namespace);
            Debug.Assert(res);
            return true;
        }
    }
}
