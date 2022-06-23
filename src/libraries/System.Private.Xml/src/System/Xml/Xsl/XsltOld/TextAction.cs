// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class TextAction : CompiledAction
    {
        private bool _disableOutputEscaping;
        private string? _text;

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CompileContent(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;

            if (Ref.Equal(name, compiler.Atoms.DisableOutputEscaping))
            {
                _disableOutputEscaping = compiler.GetYesNo(value);
            }
            else
            {
                return false;
            }

            return true;
        }

        private void CompileContent(Compiler compiler)
        {
            if (compiler.Recurse())
            {
                NavigatorInput input = compiler.Input;

                _text = string.Empty;

                do
                {
                    switch (input.NodeType)
                    {
                        case XPathNodeType.Text:
                        case XPathNodeType.Whitespace:
                        case XPathNodeType.SignificantWhitespace:
                            _text += input.Value;
                            break;
                        case XPathNodeType.Comment:
                        case XPathNodeType.ProcessingInstruction:
                            break;
                        default:
                            throw compiler.UnexpectedKeyword();
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
                    if (processor.TextEvent(_text, _disableOutputEscaping))
                    {
                        frame.Finished();
                    }
                    break;

                default:
                    Debug.Fail("Invalid execution state in TextAction");
                    break;
            }
        }
    }
}
