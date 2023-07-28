// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class ProcessingInstructionAction : ContainerAction
    {
        private const int NameReady = 3;

        private Avt? _nameAvt;
        // Compile time precalculated AVT
        private string? _name;

        internal ProcessingInstructionAction() { }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(_nameAvt, "name");

            if (_nameAvt!.IsConstant)
            {
                _name = _nameAvt.Evaluate(null, null);
                _nameAvt = null;
                if (!IsProcessingInstructionName(_name))
                {
                    // For Now: set to null to ignore action late;
                    _name = null;
                }
            }

            if (compiler.Recurse())
            {
                CompileTemplate(compiler);
                compiler.ToParent();
            }
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Name))
            {
                _nameAvt = Avt.CompileAvt(compiler, value);
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

            switch (frame.State)
            {
                case Initialized:
                    if (_nameAvt == null)
                    {
                        frame.StoredOutput = _name;
                        if (_name == null)
                        {
                            // name was static but was bad;
                            frame.Finished();
                            break;
                        }
                    }
                    else
                    {
                        frame.StoredOutput = _nameAvt.Evaluate(processor, frame);
                        if (!IsProcessingInstructionName(frame.StoredOutput))
                        {
                            frame.Finished();
                            break;
                        }
                    }
                    goto case NameReady;

                case NameReady:
                    Debug.Assert(frame.StoredOutput != null);
                    if (processor.BeginEvent(XPathNodeType.ProcessingInstruction, string.Empty, frame.StoredOutput, string.Empty, false) == false)
                    {
                        // Come back later
                        frame.State = NameReady;
                        break;
                    }
                    processor.PushActionFrame(frame);
                    frame.State = ProcessingChildren;
                    break;                              // Allow children to run

                case ProcessingChildren:
                    if (processor.EndEvent(XPathNodeType.ProcessingInstruction) == false)
                    {
                        frame.State = ProcessingChildren;
                        break;
                    }
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid ElementAction execution state");
                    frame.Finished();
                    break;
            }
        }


        internal static bool IsProcessingInstructionName(string? name)
        {
            if (name == null)
            {
                return false;
            }

            int nameLength = name.Length;
            int position = 0;

            while (position < nameLength && XmlCharType.IsWhiteSpace(name[position]))
            {
                position++;
            }

            if (position >= nameLength)
            {
                return false;
            }

            int len = ValidateNames.ParseNCName(name, position);
            if (len == 0)
            {
                return false;
            }
            position += len;

            while (position < nameLength && XmlCharType.IsWhiteSpace(name[position]))
            {
                position++;
            }

            if (position < nameLength)
            {
                return false;
            }

            if (nameLength == 3 && name.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
