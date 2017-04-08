// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal enum VariableType
    {
        GlobalVariable,
        GlobalParameter,
        LocalVariable,
        LocalParameter,
        WithParameter,
    }

    internal class VariableAction : ContainerAction, IXsltContextVariable
    {
        public static object BeingComputedMark = new object();
        private const int ValueCalculated = 2;

        protected XmlQualifiedName name;
        protected string nameStr;
        protected string baseUri;
        protected int selectKey = Compiler.InvalidQueryKey;
        protected int stylesheetid;
        protected VariableType varType;
        private int _varKey;

        internal int Stylesheetid
        {
            get { return this.stylesheetid; }
        }
        internal XmlQualifiedName Name
        {
            get { return this.name; }
        }
        internal string NameStr
        {
            get { return this.nameStr; }
        }
        internal VariableType VarType
        {
            get { return this.varType; }
        }
        internal int VarKey
        {
            get { return _varKey; }
        }
        internal bool IsGlobal
        {
            get { return this.varType == VariableType.GlobalVariable || this.varType == VariableType.GlobalParameter; }
        }

        internal VariableAction(VariableType type)
        {
            this.varType = type;
        }

        internal override void Compile(Compiler compiler)
        {
            this.stylesheetid = compiler.Stylesheetid;
            this.baseUri = compiler.Input.BaseURI;
            CompileAttributes(compiler);
            CheckRequiredAttribute(compiler, this.name, "name");


            if (compiler.Recurse())
            {
                CompileTemplate(compiler);
                compiler.ToParent();

                if (this.selectKey != Compiler.InvalidQueryKey && this.containedActions != null)
                {
                    throw XsltException.Create(SR.Xslt_VariableCntSel2, this.nameStr);
                }
            }
            if (this.containedActions != null)
            {
                baseUri = baseUri + '#' + compiler.GetUnicRtfId();
            }
            else
            {
                baseUri = null;
            }

            _varKey = compiler.InsertVariable(this);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;

            if (Ref.Equal(name, compiler.Atoms.Name))
            {
                Debug.Assert(this.name == null && this.nameStr == null);
                this.nameStr = value;
                this.name = compiler.CreateXPathQName(this.nameStr);
            }
            else if (Ref.Equal(name, compiler.Atoms.Select))
            {
                this.selectKey = compiler.AddQuery(value);
            }
            else
            {
                return false;
            }

            return true;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null && frame.State != ValueCalculated);
            object value = null;

            switch (frame.State)
            {
                case Initialized:
                    if (IsGlobal)
                    {
                        if (frame.GetVariable(_varKey) != null)
                        { // This var was calculated already
                            frame.Finished();
                            break;
                        }
                        // Mark that the variable is being computed to check for circular references
                        frame.SetVariable(_varKey, BeingComputedMark);
                    }
                    // If this is a parameter, check whether the caller has passed the value
                    if (this.varType == VariableType.GlobalParameter)
                    {
                        value = processor.GetGlobalParameter(this.name);
                    }
                    else if (this.varType == VariableType.LocalParameter)
                    {
                        value = processor.GetParameter(this.name);
                    }
                    if (value != null)
                    {
                        goto case ValueCalculated;
                    }

                    // If value was not passed, check the 'select' attribute
                    if (this.selectKey != Compiler.InvalidQueryKey)
                    {
                        value = processor.RunQuery(frame, this.selectKey);
                        goto case ValueCalculated;
                    }

                    // If there is no 'select' attribute and the content is empty, use the empty string
                    if (this.containedActions == null)
                    {
                        value = string.Empty;
                        goto case ValueCalculated;
                    }

                    // RTF case
                    NavigatorOutput output = new NavigatorOutput(this.baseUri);
                    processor.PushOutput(output);
                    processor.PushActionFrame(frame);
                    frame.State = ProcessingChildren;
                    break;

                case ProcessingChildren:
                    RecordOutput recOutput = processor.PopOutput();
                    Debug.Assert(recOutput is NavigatorOutput);
                    value = ((NavigatorOutput)recOutput).Navigator;
                    goto case ValueCalculated;

                case ValueCalculated:
                    Debug.Assert(value != null);
                    frame.SetVariable(_varKey, value);
                    frame.Finished();
                    break;

                default:
                    Debug.Fail("Invalid execution state inside VariableAction.Execute");
                    break;
            }
        }

        // ---------------------- IXsltContextVariable --------------------

        XPathResultType IXsltContextVariable.VariableType
        {
            get { return XPathResultType.Any; }
        }
        object IXsltContextVariable.Evaluate(XsltContext xsltContext)
        {
            return ((XsltCompileContext)xsltContext).EvaluateVariable(this);
        }
        bool IXsltContextVariable.IsLocal
        {
            get { return this.varType == VariableType.LocalVariable || this.varType == VariableType.LocalParameter; }
        }
        bool IXsltContextVariable.IsParam
        {
            get { return this.varType == VariableType.LocalParameter || this.varType == VariableType.GlobalParameter; }
        }
    }
}
