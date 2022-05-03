// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class ApplyImportsAction : CompiledAction
    {
        private XmlQualifiedName? _mode;
        private Stylesheet? _stylesheet;
        private const int TemplateProcessed = 2;
        internal override void Compile(Compiler compiler)
        {
            CheckEmpty(compiler);
            if (!compiler.CanHaveApplyImports)
            {
                throw XsltException.Create(SR.Xslt_ApplyImports);
            }
            _mode = compiler.CurrentMode;
            _stylesheet = compiler.CompiledStylesheet;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            switch (frame.State)
            {
                case Initialized:
                    processor.PushTemplateLookup(frame.NodeSet, _mode, /*importsOf:*/_stylesheet);
                    frame.State = TemplateProcessed;
                    break;
                case TemplateProcessed:
                    frame.Finished();
                    break;
            }
        }
    }
}
