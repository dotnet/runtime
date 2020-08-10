// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class GroupQuery : BaseAxisQuery
    {
        public GroupQuery(Query qy) : base(qy) { }
        private GroupQuery(GroupQuery other) : base(other) { }

        public override XPathNavigator? Advance()
        {
            currentNode = qyInput.Advance();
            if (currentNode != null)
            {
                position++;
            }
            return currentNode;
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            return qyInput.Evaluate(nodeIterator);
        }

        public override XPathNodeIterator Clone() { return new GroupQuery(this); }
        public override XPathResultType StaticType { get { return qyInput.StaticType; } }
        public override QueryProps Properties { get { return QueryProps.Position; } } // Doesn't have QueryProps.Merge
    }
}
