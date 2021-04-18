// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class ChooseAction : ContainerAction
    {
        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);

            if (compiler.Recurse())
            {
                CompileConditions(compiler);
                compiler.ToParent();
            }
        }

        private void CompileConditions(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            bool when = false;
            bool otherwise = false;

            do
            {
                switch (input.NodeType)
                {
                    case XPathNodeType.Element:
                        compiler.PushNamespaceScope();
                        string nspace = input.NamespaceURI;
                        string name = input.LocalName;

                        if (Ref.Equal(nspace, input.Atoms.UriXsl))
                        {
                            IfAction? action = null;
                            if (Ref.Equal(name, input.Atoms.When))
                            {
                                if (otherwise)
                                {
                                    throw XsltException.Create(SR.Xslt_WhenAfterOtherwise);
                                }
                                action = compiler.CreateIfAction(IfAction.ConditionType.ConditionWhen);
                                when = true;
                            }
                            else if (Ref.Equal(name, input.Atoms.Otherwise))
                            {
                                if (otherwise)
                                {
                                    throw XsltException.Create(SR.Xslt_DupOtherwise);
                                }
                                action = compiler.CreateIfAction(IfAction.ConditionType.ConditionOtherwise);
                                otherwise = true;
                            }
                            else
                            {
                                throw compiler.UnexpectedKeyword();
                            }
                            AddAction(action);
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
                        throw XsltException.Create(SR.Xslt_InvalidContents, "choose");
                }
            }
            while (compiler.Advance());
            if (!when)
            {
                throw XsltException.Create(SR.Xslt_NoWhen);
            }
        }
    }
}
