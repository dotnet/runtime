// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal class AttributeSetAction : ContainerAction
    {
        internal XmlQualifiedName name;

        internal XmlQualifiedName Name
        {
            get { return this.name; }
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, this.name, "name");
            CompileContent(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Name))
            {
                Debug.Assert(this.name == null);
                this.name = compiler.CreateXPathQName(value);
            }
            else if (Ref.Equal(name, compiler.Atoms.UseAttributeSets))
            {
                // create a UseAttributeSetsAction
                // sets come before xsl:attributes
                AddAction(compiler.CreateUseAttributeSetsAction());
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

                            if (Ref.Equal(nspace, input.Atoms.UriXsl) && Ref.Equal(name, input.Atoms.Attribute))
                            {
                                // found attribute so add it
                                AddAction(compiler.CreateAttributeAction());
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
                            throw XsltException.Create(SR.Xslt_InvalidContents, "attribute-set");
                    }
                }
                while (compiler.Advance());

                compiler.ToParent();
            }
        }

        internal void Merge(AttributeSetAction attributeAction)
        {
            // add the contents of "attributeAction" to this action
            // place them at the end
            Action action;
            int i = 0;

            while ((action = attributeAction.GetAction(i)) != null)
            {
                AddAction(action);
                i++;
            }
        }
    }
}
