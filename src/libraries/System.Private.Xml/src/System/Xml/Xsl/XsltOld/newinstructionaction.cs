// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class NewInstructionAction : ContainerAction
    {
        private string? _name;
        private string? _parent;
        private bool _fallback;

        internal override void Compile(Compiler compiler)
        {
            XPathNavigator nav = compiler.Input.Navigator.Clone();
            _name = nav.Name;
            nav.MoveToParent();
            _parent = nav.Name;
            if (compiler.Recurse())
            {
                CompileSelectiveTemplate(compiler);
                compiler.ToParent();
            }
        }

        internal void CompileSelectiveTemplate(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            do
            {
                if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl) &&
                    Ref.Equal(input.LocalName, input.Atoms.Fallback))
                {
                    _fallback = true;
                    if (compiler.Recurse())
                    {
                        CompileTemplate(compiler);
                        compiler.ToParent();
                    }
                }
            }
            while (compiler.Advance());
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            switch (frame.State)
            {
                case Initialized:
                    if (!_fallback)
                    {
                        throw XsltException.Create(SR.Xslt_UnknownExtensionElement, _name);
                    }
                    if (this.containedActions != null && this.containedActions.Count > 0)
                    {
                        processor.PushActionFrame(frame);
                        frame.State = ProcessingChildren;
                        break;
                    }
                    else goto case ProcessingChildren;
                case ProcessingChildren:
                    frame.Finished();
                    break;

                default:
                    Debug.Fail("Invalid Container action execution state");
                    break;
            }
        }
    }
}
