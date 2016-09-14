// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    // DescendantOverDescendantQuery: for each input it looks for the topmost descendents that matches to ns:name
    // This is possible when query which has this query as its input (child query) is descendent as well.
    // Work of this query doesn't depend on DOD of its input. 
    // It doesn't generate DOD of the output even when input is DOD. 
    internal sealed class DescendantOverDescendantQuery : DescendantBaseQuery
    {
        private int _level = 0;

        public DescendantOverDescendantQuery(Query qyParent, bool matchSelf, string name, string prefix, XPathNodeType typeTest, bool abbrAxis) :
            base(qyParent, name, prefix, typeTest, matchSelf, abbrAxis)
        { }
        private DescendantOverDescendantQuery(DescendantOverDescendantQuery other) : base(other)
        {
            _level = other._level;
        }

        public override void Reset()
        {
            _level = 0;
            base.Reset();
        }

        public override XPathNavigator Advance()
        {
            while (true)
            {
                if (_level == 0)
                {
                    currentNode = qyInput.Advance();
                    position = 0;
                    if (currentNode == null)
                    {
                        return null;
                    }
                    if (matchSelf && matches(currentNode))
                    {
                        position = 1;
                        return currentNode;
                    }
                    currentNode = currentNode.Clone();
                    if (!MoveToFirstChild())
                    {
                        continue;
                    }
                }
                else
                {
                    if (!MoveUpUntilNext())
                    {
                        continue;
                    }
                }
                do
                {
                    if (matches(currentNode))
                    {
                        position++;
                        return currentNode;
                    }
                } while (MoveToFirstChild());
            }
        }

        private bool MoveToFirstChild()
        {
            if (currentNode.MoveToFirstChild())
            {
                _level++;
                return true;
            }
            return false;
        }

        private bool MoveUpUntilNext()
        { // move up until we can move next
            while (!currentNode.MoveToNext())
            {
                --_level;
                if (_level == 0)
                {
                    return false;
                }
                bool result = currentNode.MoveToParent();
                Debug.Assert(result, "Algorithm error, We always should be able to move up if level > 0");
            }
            return true;
        }

        public override XPathNodeIterator Clone() { return new DescendantOverDescendantQuery(this); }
    }
}
