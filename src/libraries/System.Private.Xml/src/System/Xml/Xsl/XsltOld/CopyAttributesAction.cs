// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class CopyAttributesAction : Action
    {
        private const int BeginEvent = 2;
        private const int TextEvent = 3;
        private const int EndEvent = 4;
        private const int Advance = 5;

        private static readonly CopyAttributesAction s_Action = new CopyAttributesAction();

        internal static CopyAttributesAction GetAction()
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
                        if (!frame.Node!.HasAttributes || frame.Node.MoveToFirstAttribute() == false)
                        {
                            frame.Finished();
                            break;
                        }

                        frame.State = BeginEvent;
                        goto case BeginEvent;

                    case BeginEvent:
                        Debug.Assert(frame.State == BeginEvent);
                        Debug.Assert(frame.Node!.NodeType == XPathNodeType.Attribute);

                        if (SendBeginEvent(processor, frame.Node) == false)
                        {
                            // This one wasn't output
                            break;
                        }
                        frame.State = TextEvent;
                        continue;

                    case TextEvent:
                        Debug.Assert(frame.State == TextEvent);
                        Debug.Assert(frame.Node!.NodeType == XPathNodeType.Attribute);

                        if (SendTextEvent(processor, frame.Node) == false)
                        {
                            // This one wasn't output
                            break;
                        }
                        frame.State = EndEvent;
                        continue;

                    case EndEvent:
                        Debug.Assert(frame.State == EndEvent);
                        Debug.Assert(frame.Node!.NodeType == XPathNodeType.Attribute);

                        if (SendEndEvent(processor, frame.Node) == false)
                        {
                            // This one wasn't output
                            break;
                        }
                        frame.State = Advance;
                        continue;

                    case Advance:
                        Debug.Assert(frame.State == Advance);
                        Debug.Assert(frame.Node!.NodeType == XPathNodeType.Attribute);

                        if (frame.Node.MoveToNextAttribute())
                        {
                            frame.State = BeginEvent;
                            continue;
                        }
                        else
                        {
                            frame.Node.MoveToParent();
                            frame.Finished();
                            break;
                        }
                }
                break;
            }// while (processor.CanContinue)
        }

        private static bool SendBeginEvent(Processor processor, XPathNavigator node)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Attribute);
            return processor.BeginEvent(XPathNodeType.Attribute, node.Prefix, node.LocalName, node.NamespaceURI, false);
        }

        private static bool SendTextEvent(Processor processor, XPathNavigator node)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Attribute);
            return processor.TextEvent(node.Value);
        }

        private static bool SendEndEvent(Processor processor, XPathNavigator node)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Attribute);
            return processor.EndEvent(XPathNodeType.Attribute);
        }
    }
}
