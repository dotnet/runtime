// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal abstract class CacheAxisQuery : BaseAxisQuery
    {
        // int count; -- we reusing it here
        protected List<XPathNavigator> outputBuffer;

        public CacheAxisQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest) : base(qyInput, name, prefix, typeTest)
        {
            this.outputBuffer = new List<XPathNavigator>();
            this.count = 0;
        }
        protected CacheAxisQuery(CacheAxisQuery other) : base(other)
        {
            this.outputBuffer = new List<XPathNavigator>(other.outputBuffer);
            this.count = other.count;
        }

        public override void Reset()
        {
            this.count = 0;
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.Evaluate(context);
            outputBuffer.Clear();
            return this;
        }

        public override XPathNavigator? Advance()
        {
            Debug.Assert(0 <= count && count <= outputBuffer.Count);
            if (count < outputBuffer.Count)
            {
                return outputBuffer[count++];
            }
            return null;
        }

        public override XPathNavigator? Current
        {
            get
            {
                Debug.Assert(0 <= count && count <= outputBuffer.Count);
                if (count == 0)
                {
                    return null;
                }
                return outputBuffer[count - 1];
            }
        }

        public override int CurrentPosition { get { return count; } }
        public override int Count { get { return outputBuffer.Count; } }
        public override QueryProps Properties { get { return QueryProps.Merge | QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }
    }
}
