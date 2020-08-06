// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class EmptyQuery : Query
    {
        public override XPathNavigator? Advance() { return null; }
        public override XPathNodeIterator Clone() { return this; }
        public override object Evaluate(XPathNodeIterator context) { return this; }
        public override int CurrentPosition { get { return 0; } }
        public override int Count { get { return 0; } }
        public override QueryProps Properties { get { return QueryProps.Merge | QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }
        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
        public override void Reset() { }
        public override XPathNavigator? Current { get { return null; } }
    }
}
