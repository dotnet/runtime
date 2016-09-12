// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;
    using System.Globalization;

    internal class TemplateAction : TemplateBaseAction
    {
        private int _matchKey = Compiler.InvalidQueryKey;
        private XmlQualifiedName _name;
        private double _priority = double.NaN;
        private XmlQualifiedName _mode;
        private int _templateId;
        private bool _replaceNSAliasesDone;

        internal int MatchKey
        {
            get { return _matchKey; }
        }

        internal XmlQualifiedName Name
        {
            get { return _name; }
        }

        internal double Priority
        {
            get { return _priority; }
        }

        internal XmlQualifiedName Mode
        {
            get { return _mode; }
        }

        internal int TemplateId
        {
            get { return _templateId; }
            set
            {
                Debug.Assert(_templateId == 0);
                _templateId = value;
            }
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            if (_matchKey == Compiler.InvalidQueryKey)
            {
                if (_name == null)
                {
                    throw XsltException.Create(SR.Xslt_TemplateNoAttrib);
                }
                if (_mode != null)
                {
                    throw XsltException.Create(SR.Xslt_InvalidModeAttribute);
                }
            }
            compiler.BeginTemplate(this);

            if (compiler.Recurse())
            {
                CompileParameters(compiler);
                CompileTemplate(compiler);

                compiler.ToParent();
            }

            compiler.EndTemplate();
            AnalyzePriority(compiler);
        }

        internal virtual void CompileSingle(Compiler compiler)
        {
            _matchKey = compiler.AddQuery("/", /*allowVars:*/false, /*allowKey:*/true, /*pattern*/true);
            _priority = Compiler.RootPriority;

            CompileOnceTemplate(compiler);
        }

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;

            if (Ref.Equal(name, compiler.Atoms.Match))
            {
                Debug.Assert(_matchKey == Compiler.InvalidQueryKey);
                _matchKey = compiler.AddQuery(value, /*allowVars:*/false, /*allowKey:*/true, /*pattern*/true);
            }
            else if (Ref.Equal(name, compiler.Atoms.Name))
            {
                Debug.Assert(_name == null);
                _name = compiler.CreateXPathQName(value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Priority))
            {
                Debug.Assert(Double.IsNaN(_priority));
                _priority = XmlConvert.ToXPathDouble(value);
                if (double.IsNaN(_priority) && !compiler.ForwardCompatibility)
                {
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, "priority", value);
                }
            }
            else if (Ref.Equal(name, compiler.Atoms.Mode))
            {
                Debug.Assert(_mode == null);
                if (compiler.AllowBuiltInMode && value == "*")
                {
                    _mode = Compiler.BuiltInMode;
                }
                else
                {
                    _mode = compiler.CreateXPathQName(value);
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private void AnalyzePriority(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;

            if (!Double.IsNaN(_priority) || _matchKey == Compiler.InvalidQueryKey)
            {
                return;
            }
            // Split Unions:
            TheQuery theQuery = (TheQuery)compiler.QueryStore[this.MatchKey];
            CompiledXpathExpr expr = (CompiledXpathExpr)theQuery.CompiledQuery;
            Query query = expr.QueryTree;
            UnionExpr union;
            while ((union = query as UnionExpr) != null)
            {
                Debug.Assert(!(union.qy2 is UnionExpr), "only qy1 can be union");
                TemplateAction copy = this.CloneWithoutName();
                compiler.QueryStore.Add(new TheQuery(
                    new CompiledXpathExpr(union.qy2, expr.Expression, false),
                    theQuery._ScopeManager
                ));
                copy._matchKey = compiler.QueryStore.Count - 1;
                copy._priority = union.qy2.XsltDefaultPriority;
                compiler.AddTemplate(copy);

                query = union.qy1;
            }
            if (expr.QueryTree != query)
            {
                // query was splitted and we need create new TheQuery for this template
                compiler.QueryStore[this.MatchKey] = new TheQuery(
                    new CompiledXpathExpr(query, expr.Expression, false),
                    theQuery._ScopeManager
                );
            }
            _priority = query.XsltDefaultPriority;
        }

        protected void CompileParameters(Compiler compiler)
        {
            NavigatorInput input = compiler.Input;
            do
            {
                switch (input.NodeType)
                {
                    case XPathNodeType.Element:
                        if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl) &&
                            Ref.Equal(input.LocalName, input.Atoms.Param))
                        {
                            compiler.PushNamespaceScope();
                            AddAction(compiler.CreateVariableAction(VariableType.LocalParameter));
                            compiler.PopScope();
                            continue;
                        }
                        else
                        {
                            return;
                        }
                    case XPathNodeType.Text:
                        return;
                    case XPathNodeType.SignificantWhitespace:
                        this.AddEvent(compiler.CreateTextEvent());
                        continue;
                    default:
                        continue;
                }
            }
            while (input.Advance());
        }

        //
        // Priority calculation plus template splitting
        //

        private TemplateAction CloneWithoutName()
        {
            TemplateAction clone = new TemplateAction();
            {
                clone.containedActions = this.containedActions;
                clone._mode = _mode;
                clone.variableCount = this.variableCount;
                clone._replaceNSAliasesDone = true; // We shouldn't replace NS in clones.
            }
            return clone;
        }

        internal override void ReplaceNamespaceAlias(Compiler compiler)
        {
            // if template has both name and match it will be twice caled by stylesheet to replace NS aliases.
            if (!_replaceNSAliasesDone)
            {
                base.ReplaceNamespaceAlias(compiler);
                _replaceNSAliasesDone = true;
            }
        }
        //
        // Execution
        //

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);

            switch (frame.State)
            {
                case Initialized:
                    if (this.variableCount > 0)
                    {
                        frame.AllocateVariables(this.variableCount);
                    }
                    if (this.containedActions != null && this.containedActions.Count > 0)
                    {
                        processor.PushActionFrame(frame);
                        frame.State = ProcessingChildren;
                    }
                    else
                    {
                        frame.Finished();
                    }
                    break;                              // Allow children to run
                case ProcessingChildren:
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid Container action execution state");
                    break;
            }
        }
    }
}
