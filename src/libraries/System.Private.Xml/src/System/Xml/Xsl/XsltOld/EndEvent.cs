// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class EndEvent : Event
    {
        private readonly XPathNodeType _nodeType;

        internal EndEvent(XPathNodeType nodeType)
        {
            Debug.Assert(nodeType != XPathNodeType.Namespace);
            _nodeType = nodeType;
        }

        public override bool Output(Processor processor, ActionFrame frame)
        {
            return processor.EndEvent(_nodeType);
        }
    }
}
