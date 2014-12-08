// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.XPath;
using StackNav = MS.Internal.Xml.XPath.ClonableStack<System.Xml.XPath.XPathNavigator>;

namespace MS.Internal.Xml.XPath
{
    internal sealed class FollSiblingQuery : BaseAxisQuery
    {
        StackNav elementStk;
        List<XPathNavigator> parentStk;
        XPathNavigator nextInput;

        public FollSiblingQuery(Query qyInput, string name, string prefix, XPathNodeType type) : base(qyInput, name, prefix, type)
        {
            this.elementStk = new StackNav();
            this.parentStk = new List<XPathNavigator>();
        }
        private FollSiblingQuery(FollSiblingQuery other) : base(other)
        {
            this.elementStk = other.elementStk.Clone();
            this.parentStk = new List<XPathNavigator>(other.parentStk);
            this.nextInput = Clone(other.nextInput);
        }

        public override void Reset()
        {
            elementStk.Clear();
            parentStk.Clear();
            nextInput = null;
            base.Reset();
        }

        private bool Visited(XPathNavigator nav)
        {
            XPathNavigator parent = nav.Clone();
            parent.MoveToParent();
            for (int i = 0; i < parentStk.Count; i++)
            {
                if (parent.IsSamePosition(parentStk[i]))
                {
                    return true;
                }
            }
            parentStk.Add(parent);
            return false;
        }

        private XPathNavigator FetchInput()
        {
            XPathNavigator input;
            do
            {
                input = qyInput.Advance();
                if (input == null)
                {
                    return null;
                }
            } while (Visited(input));
            return input.Clone();
        }

        public override XPathNavigator Advance()
        {
            while (true)
            {
                if (currentNode == null)
                {
                    if (nextInput == null)
                    {
                        nextInput = FetchInput(); // This can happen at the beginning and at the end 
                    }
                    if (elementStk.Count == 0)
                    {
                        if (nextInput == null)
                        {
                            return null;
                        }
                        currentNode = nextInput;
                        nextInput = FetchInput();
                    }
                    else
                    {
                        currentNode = elementStk.Pop();
                    }
                }

                while (currentNode.IsDescendant(nextInput))
                {
                    elementStk.Push(currentNode);
                    currentNode = nextInput;
                    nextInput = qyInput.Advance();
                    if (nextInput != null)
                    {
                        nextInput = nextInput.Clone();
                    }
                }

                while (currentNode.MoveToNext())
                {
                    if (matches(currentNode))
                    {
                        position++;
                        return currentNode;
                    }
                }
                currentNode = null;
            }
        } // Advance

        public override XPathNodeIterator Clone() { return new FollSiblingQuery(this); }
    }
}
