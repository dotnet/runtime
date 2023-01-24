// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class CopyAction : ContainerAction
    {
        // Local execution states
        private const int NamespaceCopy = 5;

        private const int ContentsCopy = 6;
        private const int ProcessChildren = 7;
        private const int ChildrenOnly = 8;

        private bool _empty;

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);

            if (compiler.Recurse())
            {
                CompileTemplate(compiler);
                compiler.ToParent();
            }
            if (this.containedActions == null)
                _empty = true;
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            if (Ref.Equal(name, compiler.Atoms.UseAttributeSets))
            {
                AddAction(compiler.CreateUseAttributeSetsAction());
            }
            else
            {
                return false;
            }
            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            while (processor.CanContinue)
            {
                switch (frame.State)
                {
                    case Initialized:
                        if (Processor.IsRoot(frame.Node!))
                        {
                            processor.PushActionFrame(frame);
                            frame.State = ChildrenOnly;
                            break;
                        }

                        if (processor.CopyBeginEvent(frame.Node!, _empty) == false)
                        {
                            // This event wasn't processed
                            break;
                        }
                        frame.State = NamespaceCopy;

                        continue;
                    case NamespaceCopy:
                        frame.State = ContentsCopy;
                        if (frame.Node!.NodeType == XPathNodeType.Element)
                        {
                            processor.PushActionFrame(CopyNamespacesAction.GetAction(), frame.NodeSet);
                            break;
                        }
                        continue;
                    case ContentsCopy:
                        if (frame.Node!.NodeType == XPathNodeType.Element && !_empty)
                        {
                            //Debug.Assert(frame.Node.HasValue == false);
                            processor.PushActionFrame(frame);
                            frame.State = ProcessChildren;
                            break;
                        }
                        else
                        {
                            if (processor.CopyTextEvent(frame.Node))
                            {
                                frame.State = ProcessChildren;
                                continue;
                            }
                            else
                            {
                                // This event wasn't processed
                                break;
                            }
                        }

                    case ProcessChildren:
                        if (processor.CopyEndEvent(frame.Node!))
                        {
                            frame.Finished();
                        }
                        break;

                    case ChildrenOnly:
                        frame.Finished();
                        break;

                    default:
                        Debug.Fail("Invalid CopyAction execution state");
                        break;
                }

                break;
            }
        }
    }
}
