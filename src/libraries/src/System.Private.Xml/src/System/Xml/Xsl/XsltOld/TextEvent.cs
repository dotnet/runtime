// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal class TextEvent : Event
    {
        private string _text;

        protected TextEvent() { }

        public TextEvent(string text)
        {
            Debug.Assert(text != null);
            _text = text;
        }

        public TextEvent(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            Debug.Assert(input.NodeType == XPathNodeType.Text || input.NodeType == XPathNodeType.SignificantWhitespace);
            _text = input.Value;
        }

        public override bool Output(Processor processor, ActionFrame frame)
        {
            return processor.TextEvent(_text);
        }

        public virtual string Evaluate(Processor processor, ActionFrame frame)
        {
            return _text;
        }
    }
}
