// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Text;

    internal sealed class AvtEvent : TextEvent
    {
        private int _key;

        public AvtEvent(int key)
        {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            _key = key;
        }

        public override bool Output(Processor processor, ActionFrame frame)
        {
            Debug.Assert(_key != Compiler.InvalidQueryKey);
            return processor.TextEvent(processor.EvaluateString(frame, _key));
        }

        public override string Evaluate(Processor processor, ActionFrame frame)
        {
            return processor.EvaluateString(frame, _key);
        }
    }
}
