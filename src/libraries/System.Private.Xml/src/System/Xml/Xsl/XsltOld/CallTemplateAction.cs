// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class CallTemplateAction : ContainerAction
    {
        private const int ProcessedChildren = 2;
        private const int ProcessedTemplate = 3;
        private XmlQualifiedName? _name;

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, _name, "name");
            CompileContent(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Name))
            {
                Debug.Assert(_name == null);
                _name = compiler.CreateXPathQName(value);
            }
            else
            {
                return false;
            }

            return true;
        }

        private void CompileContent(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            if (compiler.Recurse())
            {
                do
                {
                    switch (input.NodeType)
                    {
                        case XPathNodeType.Element:
                            compiler.PushNamespaceScope();
                            string nspace = input.NamespaceURI;
                            string name = input.LocalName;

                            if (Ref.Equal(nspace, input.Atoms.UriXsl) && Ref.Equal(name, input.Atoms.WithParam))
                            {
                                WithParamAction par = compiler.CreateWithParamAction();
                                CheckDuplicateParams(par.Name!);
                                AddAction(par);
                            }
                            else
                            {
                                throw compiler.UnexpectedKeyword();
                            }
                            compiler.PopScope();
                            break;
                        case XPathNodeType.Comment:
                        case XPathNodeType.ProcessingInstruction:
                        case XPathNodeType.Whitespace:
                        case XPathNodeType.SignificantWhitespace:
                            break;
                        default:
                            throw XsltException.Create(SR.Xslt_InvalidContents, "call-template");
                    }
                } while (compiler.Advance());

                compiler.ToParent();
            }
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            switch (frame.State)
            {
                case Initialized:
                    processor.ResetParams();
                    if (this.containedActions != null && this.containedActions.Count > 0)
                    {
                        processor.PushActionFrame(frame);
                        frame.State = ProcessedChildren;
                        break;
                    }
                    goto case ProcessedChildren;
                case ProcessedChildren:
                    TemplateAction? action = processor.Stylesheet.FindTemplate(_name!);
                    if (action != null)
                    {
                        frame.State = ProcessedTemplate;
                        processor.PushActionFrame(action, frame.NodeSet);
                        break;
                    }
                    else
                    {
                        throw XsltException.Create(SR.Xslt_InvalidCallTemplate, _name!.ToString());
                    }
                case ProcessedTemplate:
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid CallTemplateAction execution state");
                    break;
            }
        }
    }
}
