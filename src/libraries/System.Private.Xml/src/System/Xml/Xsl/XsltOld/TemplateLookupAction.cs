// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;
    using System.Xml.XPath;

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
            Debug.Assert(processor is not null && frame is not null);
            Debug.Assert(frame.State == Initialized);

            Action? action = null;

            if (this.mode is not null)
            {
                action = importsOf is null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!, this.mode)
                    : importsOf.FindTemplateImports(processor, frame.Node!, this.mode);
            }
            else
            {
                action = importsOf is null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!)
                    : importsOf.FindTemplateImports(processor, frame.Node!);
            }

            // Built-int template rules
            if (action is null)
            {
                action = BuiltInTemplate(frame.Node!);
            }

            // Jump
            if (action is not null)
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
            Debug.Assert(node is not null);
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

    internal class TemplateLookupActionDbg : TemplateLookupAction
    {
        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor is not null && frame is not null);
            Debug.Assert(frame.State == Initialized);
            Debug.Assert(processor.Debugger is not null);

            Action? action = null;

            if (this.mode == Compiler.BuiltInMode)
            {
                // mode="*" -- use one from debuggerStack
                this.mode = processor.GetPreviousMode();
                Debug.Assert(this.mode != Compiler.BuiltInMode);
            }
            processor.SetCurrentMode(this.mode);

            if (this.mode is not null)
            {
                action = importsOf is null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!, this.mode)
                    : importsOf.FindTemplateImports(processor, frame.Node!, this.mode);
            }
            else
            {
                action = importsOf is null
                    ? processor.Stylesheet.FindTemplate(processor, frame.Node!)
                    : importsOf.FindTemplateImports(processor, frame.Node!);
            }

            // Built-int template rules
            if (action is null && processor.RootAction!.builtInSheet is not null)
            {
                action = processor.RootAction.builtInSheet.FindTemplate(processor, frame.Node!, Compiler.BuiltInMode);
            }
            if (action is null)
            {
                action = BuiltInTemplate(frame.Node!);
            }

            // Jump
            if (action is not null)
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
