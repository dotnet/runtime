// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class WithParamAction : VariableAction
    {
        internal WithParamAction() : base(VariableType.WithParameter) { }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckRequiredAttribute(this.name, "name");
            if (compiler.Recurse())
            {
                CompileTemplate(compiler);
                compiler.ToParent();

                if (this.selectKey != Compiler.InvalidQueryKey && this.containedActions != null)
                {
                    throw XsltException.Create(SR.Xslt_VariableCntSel2, this.nameStr);
                }
            }
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            object ParamValue;
            switch (frame.State)
            {
                case Initialized:
                    if (this.selectKey != Compiler.InvalidQueryKey)
                    {
                        ParamValue = processor.RunQuery(frame, this.selectKey);
                        processor.SetParameter(this.name!, ParamValue);
                        frame.Finished();
                    }
                    else
                    {
                        if (this.containedActions == null)
                        {
                            processor.SetParameter(this.name!, string.Empty);
                            frame.Finished();
                            break;
                        }
                        NavigatorOutput output = new NavigatorOutput(baseUri!);
                        processor.PushOutput(output);
                        processor.PushActionFrame(frame);
                        frame.State = ProcessingChildren;
                    }
                    break;
                case ProcessingChildren:
                    IRecordOutput recOutput = processor.PopOutput();
                    Debug.Assert(recOutput is NavigatorOutput);
                    processor.SetParameter(this.name!, ((NavigatorOutput)recOutput).Navigator);
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid execution state inside VariableAction.Execute");
                    break;
            }
        }
    }
}
