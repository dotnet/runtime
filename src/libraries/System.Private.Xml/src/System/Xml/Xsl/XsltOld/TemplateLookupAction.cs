// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal class TemplateLookupAction : Action
    {
        protected XmlQualifiedName? mode;
        protected Stylesheet? importsOf;

        internal void Initialize(XmlQualifiedName? mode, Stylesheet? importsOf)
        {
            this.mode = mode;
            this.importsOf = importsOf;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            Debug.Assert(frame.State == Initialized);

            Action? action;

            if (this.mode != null)
            {
                action = importsOf == null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!, this.mode)
                    : importsOf.FindTemplateImports(processor, frame.Node!, this.mode);
            }
            else
            {
                action = importsOf == null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!)
                    : importsOf.FindTemplateImports(processor, frame.Node!);
            }

            // Built-int template rules
            action ??= BuiltInTemplate(frame.Node!);

            // Jump
            if (action != null)
            {
                frame.SetAction(action);
            }
            else
            {
                frame.Finished();
            }
        }

        internal Action? BuiltInTemplate(XPathNavigator node)
        {
            Debug.Assert(node != null);
            Action? action = null;

            switch (node.NodeType)
            {
                //  <xsl:template match="*|/" [mode="?"]>
                //    <xsl:apply-templates [mode="?"]/>
                //  </xsl:template>
                case XPathNodeType.Element:
                case XPathNodeType.Root:
                    action = ApplyTemplatesAction.BuiltInRule(this.mode);
                    break;
                //  <xsl:template match="text()|@*">
                //    <xsl:value-of select="."/>
                //  </xsl:template>
                case XPathNodeType.Attribute:
                case XPathNodeType.Whitespace:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.Text:
                    action = ValueOfAction.BuiltInRule();
                    break;
                // <xsl:template match="processing-instruction()|comment()"/>
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                    // Empty action;
                    break;
                case XPathNodeType.All:
                    // Ignore the rest
                    break;
            }

            return action;
        }
    }

    internal sealed class TemplateLookupActionDbg : TemplateLookupAction
    {
        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            Debug.Assert(frame.State == Initialized);
            Debug.Assert(processor.Debugger != null);

            Action? action;

            if (this.mode == Compiler.BuiltInMode)
            {
                // mode="*" -- use one from debuggerStack
                this.mode = processor.GetPreviousMode();
                Debug.Assert(this.mode != Compiler.BuiltInMode);
            }
            processor.SetCurrentMode(this.mode);

            if (this.mode != null)
            {
                action = importsOf == null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!, this.mode)
                    : importsOf.FindTemplateImports(processor, frame.Node!, this.mode);
            }
            else
            {
                action = importsOf == null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!)
                    : importsOf.FindTemplateImports(processor, frame.Node!);
            }

            // Built-int template rules
            action ??= BuiltInTemplate(frame.Node!);

            // Jump
            if (action != null)
            {
                frame.SetAction(action);
            }
            else
            {
                frame.Finished();
            }
        }
    }
}
