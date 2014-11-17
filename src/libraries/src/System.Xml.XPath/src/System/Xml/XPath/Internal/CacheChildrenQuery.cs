// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.XPath;
using StackInt = MS.Internal.Xml.XPath.ClonableStack<int>;
using StackNav = MS.Internal.Xml.XPath.ClonableStack<System.Xml.XPath.XPathNavigator>;

namespace MS.Internal.Xml.XPath
{
    // This class implements Children axis on Ancestor & Descendant imputs. (as well as id(), preciding, following)
    // The problem here is that is descenant::*/child::* and ancestor::*/child::* can produce duplicates nodes
    // The algorithm havily uses the fact that in our implementation of both AncestorQuery and DecsndantQuery return nodes in document order. 
    // As result first child is always before or equal of next input. 
    // So we don't need to call DecideNextNode() when needInput == true && stack is empty.
    internal sealed class CacheChildrenQuery : ChildrenQuery
    {
        XPathNavigator nextInput = null;
        StackNav elementStk;
        StackInt positionStk;
        bool needInput;
#if DEBUG
        XPathNavigator lastNode = null;
#endif

        public CacheChildrenQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type)
        {
            this.elementStk = new StackNav();
            this.positionStk = new StackInt();
            this.needInput = true;
        }
        private CacheChildrenQuery(CacheChildrenQuery other) : base(other)
        {
            this.nextInput = Clone(other.nextInput);
            this.elementStk = other.elementStk.Clone();
            this.positionStk = other.positionStk.Clone();
            this.needInput = other.needInput;
#if DEBUG
            this.lastNode = Clone(other.lastNode);
#endif
        }

        public override void Reset()
        {
            nextInput = null;
            elementStk.Clear();
            positionStk.Clear();
            needInput = true;
            base.Reset();
#if DEBUG
            lastNode = null;
#endif
        }

        public override XPathNavigator Advance()
        {
            do
            {
                if (needInput)
                {
                    if (elementStk.Count == 0)
                    {
                        currentNode = GetNextInput();
                        if (currentNode == null)
                        {
                            return null;
                        }
                        if (!currentNode.MoveToFirstChild())
                        {
                            continue;
                        }
                        position = 0;
                    }
                    else
                    {
                        currentNode = elementStk.Pop();
                        position = positionStk.Pop();
                        if (!DecideNextNode())
                        {
                            continue;
                        }
                    }
                    needInput = false;
                }
                else
                {
                    if (!currentNode.MoveToNext() || !DecideNextNode())
                    {
                        needInput = true;
                        continue;
                    }
                }
#if DEBUG
                if (lastNode != null)
                {
                    if (currentNode.GetType().ToString() == "Microsoft.VisualStudio.Modeling.StoreNavigator")
                    {
                        XmlNodeOrder order = CompareNodes(lastNode, currentNode);
                        Debug.Assert(order == XmlNodeOrder.Before, "Algorith error. Nodes expected to be DocOrderDistinct");
                    }
                }
                lastNode = currentNode.Clone();
#endif
                if (matches(currentNode))
                {
                    position++;
                    return currentNode;
                }
            } while (true);
        } // Advance

        private bool DecideNextNode()
        {
            nextInput = GetNextInput();
            if (nextInput != null)
            {
                if (CompareNodes(currentNode, nextInput) == XmlNodeOrder.After)
                {
                    elementStk.Push(currentNode);
                    positionStk.Push(position);
                    currentNode = nextInput;
                    nextInput = null;
                    if (!currentNode.MoveToFirstChild())
                    {
                        return false;
                    }
                    position = 0;
                }
            }
            return true;
        }

        private XPathNavigator GetNextInput()
        {
            XPathNavigator result;
            if (nextInput != null)
            {
                result = nextInput;
                nextInput = null;
            }
            else
            {
                result = qyInput.Advance();
                if (result != null)
                {
                    result = result.Clone();
                }
            }
            return result;
        }

        public override XPathNodeIterator Clone() { return new CacheChildrenQuery(this); }
    } // Children Query}
}
