// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class AvtEvent : TextEvent
    {
        private readonly int _key;

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
