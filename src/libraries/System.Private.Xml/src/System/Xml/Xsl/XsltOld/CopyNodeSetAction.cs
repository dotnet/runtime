// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class CopyNodeSetAction : Action
    {
        private const int BeginEvent = 2;
        private const int Contents = 3;
        private const int Namespaces = 4;
        private const int Attributes = 5;
        private const int Subtree = 6;
        private const int EndEvent = 7;

        private static readonly CopyNodeSetAction s_Action = new CopyNodeSetAction();

        internal static CopyNodeSetAction GetAction()
        {
            Debug.Assert(s_Action != null);
            return s_Action;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            while (processor.CanContinue)
            {
                switch (frame.State)
                {
                    case Initialized:
                        if (frame.NextNode(processor))
                        {
                            frame.State = BeginEvent;
                            goto case BeginEvent;
                        }
                        else
                        {
                            frame.Finished();
                            break;
                        }
                    case BeginEvent:
                        Debug.Assert(frame.State == BeginEvent);

                        if (SendBeginEvent(processor, frame.Node!) == false)
                        {
                            // This one wasn't output
                            break;
                        }
                        frame.State = Contents;
                        continue;

                    case Contents:
                        Debug.Assert(frame.State == Contents);
                        XPathNodeType nodeType = frame.Node!.NodeType;

                        if (nodeType == XPathNodeType.Element || nodeType == XPathNodeType.Root)
                        {
                            processor.PushActionFrame(CopyNamespacesAction.GetAction(), frame.NodeSet);
                            frame.State = Namespaces;
                            break;
                        }

                        if (SendTextEvent(processor, frame.Node) == false)
                        {
                            // This one wasn't output
                            break;
                        }
                        frame.State = EndEvent;
                        continue;

                    case Namespaces:
                        processor.PushActionFrame(CopyAttributesAction.GetAction(), frame.NodeSet);
                        frame.State = Attributes;
                        break;

                    case Attributes:
                        if (frame.Node!.HasChildren)
                        {
                            processor.PushActionFrame(GetAction(), frame.Node.SelectChildren(XPathNodeType.All));
                            frame.State = Subtree;
                            break;
                        }
                        frame.State = EndEvent;
                        goto case EndEvent;

                    case Subtree:
                        //frame.Node.MoveToParent();
                        frame.State = EndEvent;
                        continue;

                    case EndEvent:
                        Debug.Assert(frame.State == EndEvent);

                        if (SendEndEvent(processor, frame.Node!) == false)
                        {
                            // This one wasn't output
                            break;
                        }

                        frame.State = Initialized;
                        continue;
                }

                break;
            }
        }

        private static bool SendBeginEvent(Processor processor, XPathNavigator node)
        {
            return processor.CopyBeginEvent(node, node.IsEmptyElement);
        }

        private static bool SendTextEvent(Processor processor, XPathNavigator node)
        {
            return processor.CopyTextEvent(node);
        }

        private static bool SendEndEvent(Processor processor, XPathNavigator node)
        {
            return processor.CopyEndEvent(node);
        }
    }
}
