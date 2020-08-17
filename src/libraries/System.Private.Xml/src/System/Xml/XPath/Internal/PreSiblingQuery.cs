// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    // This class can be rewritten much more efficient.
    // Algorithm could be like one for FollowingSibling:
    // - Build InputArrays: pares (first, sentinel)
    // -- Cash all input nodes as sentinel
    // -- Add first node of its parent for each input node.
    // -- Sort these pares by first nodes.
    // - Advance algorithm will look like:
    // -- For each row in InputArrays we will output first node + all its following nodes which are < sentinel
    // -- Before outputting each node in row #I we will check that it is < first node in row #I+1
    // --- if true we actually output it
    // --- if false, we hold with row #I and apply this algorithm starting for row #I+1
    // --- when we done with #I+1 we continue with row #I

    internal class PreSiblingQuery : CacheAxisQuery
    {
        public PreSiblingQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest) : base(qyInput, name, prefix, typeTest) { }
        protected PreSiblingQuery(PreSiblingQuery other) : base(other) { }

        private static bool NotVisited(XPathNavigator nav, List<XPathNavigator> parentStk)
        {
            XPathNavigator nav1 = nav.Clone();
            nav1.MoveToParent();
            for (int i = 0; i < parentStk.Count; i++)
            {
                if (nav1.IsSamePosition(parentStk[i]))
                {
                    return false;
                }
            }
            parentStk.Add(nav1);
            return true;
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.Evaluate(context);

            // Fill up base.outputBuffer
            List<XPathNavigator> parentStk = new List<XPathNavigator>();
            Stack<XPathNavigator> inputStk = new Stack<XPathNavigator>();
            while ((currentNode = qyInput.Advance()) != null)
            {
                inputStk.Push(currentNode.Clone());
            }
            while (inputStk.Count != 0)
            {
                XPathNavigator input = inputStk.Pop();
                if (input.NodeType == XPathNodeType.Attribute || input.NodeType == XPathNodeType.Namespace)
                {
                    continue;
                }
                if (NotVisited(input, parentStk))
                {
                    XPathNavigator prev = input.Clone();
                    if (prev.MoveToParent())
                    {
                        bool test = prev.MoveToFirstChild();
                        Debug.Assert(test, "We just moved to parent, how we can not have first child?");
                        while (!prev.IsSamePosition(input))
                        {
                            if (matches(prev))
                            {
                                Insert(outputBuffer, prev);
                            }
                            if (!prev.MoveToNext())
                            {
                                Debug.Fail("We managed to miss sentinel node (input)");
                                break;
                            }
                        }
                    }
                }
            }
            return this;
        }

        public override XPathNodeIterator Clone() { return new PreSiblingQuery(this); }
        public override QueryProps Properties { get { return base.Properties | QueryProps.Reverse; } }
    }
}
