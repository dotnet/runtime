// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal abstract class CacheOutputQuery : Query
    {
        internal Query input;
        // int count; -- we reusing it here
        protected List<XPathNavigator> outputBuffer;

        public CacheOutputQuery(Query input)
        {
            this.input = input;
            this.outputBuffer = new List<XPathNavigator>();
            this.count = 0;
        }
        protected CacheOutputQuery(CacheOutputQuery other) : base(other)
        {
            this.input = Clone(other.input);
            this.outputBuffer = new List<XPathNavigator>(other.outputBuffer);
            this.count = other.count;
        }

        public override void Reset()
        {
            this.count = 0;
        }

        public override void SetXsltContext(XsltContext context)
        {
            input.SetXsltContext(context);
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            outputBuffer.Clear();
            count = 0;
            return input.Evaluate(context);// This is trick. IDQuery needs this value. Otherwise we would return this.
                                           // All subclasses should and would anyway override this method and return this.
        }

        public override XPathNavigator Advance()
        {
            Debug.Assert(0 <= count && count <= outputBuffer.Count);
            if (count < outputBuffer.Count)
            {
                return outputBuffer[count++];
            }
            return null;
        }

        public override XPathNavigator Current
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

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
        public override int CurrentPosition { get { return count; } }
        public override int Count { get { return outputBuffer.Count; } }
        public override QueryProps Properties { get { return QueryProps.Merge | QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }
    }
}
