// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;

    internal sealed class IfAction : ContainerAction
    {
        internal enum ConditionType
        {
            ConditionIf,
            ConditionWhen,
            ConditionOtherwise
        }

        private readonly ConditionType _type;
        private int _testKey = Compiler.InvalidQueryKey;

        internal IfAction(ConditionType type)
        {
            _type = type;
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            if (_type != ConditionType.ConditionOtherwise)
            {
                CheckRequiredAttribute(compiler, _testKey != Compiler.InvalidQueryKey, "test");
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
            if (Ref.Equal(name, compiler.Atoms.Test))
            {
                if (_type == ConditionType.ConditionOtherwise)
                {
                    return false;
                }
                _testKey = compiler.AddBooleanQuery(value);
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
                    if (_type == ConditionType.ConditionIf || _type == ConditionType.ConditionWhen)
                    {
                        Debug.Assert(_testKey != Compiler.InvalidQueryKey);
                        bool value = processor.EvaluateBoolean(frame, _testKey);
                        if (value == false)
                        {
                            frame.Finished();
                            break;
                        }
                    }

                    processor.PushActionFrame(frame);
                    frame.State = ProcessingChildren;
                    break;                              // Allow children to run

                case ProcessingChildren:
                    if (_type == ConditionType.ConditionWhen || _type == ConditionType.ConditionOtherwise)
                    {
                        Debug.Assert(frame.Container != null);
                        frame.Exit();
                    }

                    frame.Finished();
                    break;

                default:
                    Debug.Fail("Invalid IfAction execution state");
                    break;
            }
        }
    }
}
