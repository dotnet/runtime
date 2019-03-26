// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt
{
    using TypeFactory = XmlQueryTypeFactory;
    using XPathFunctionInfo = XPathBuilder.FunctionInfo<XPathBuilder.FuncId>;
    using XsltFunctionInfo = XPathBuilder.FunctionInfo<QilGenerator.FuncId>;

    // ------------------------------- XslAstAnalyzer -------------------------------

    internal class XslAstAnalyzer : XslVisitor<XslFlags>
    {
        private CompilerScopeManager<VarPar> _scope;
        private Compiler _compiler;
#if DEBUG
        // List of all variables and parameters
        private List<VarPar> _allVarPars = new List<VarPar>();
#endif
        private int _forEachDepth = 0;
        private XPathAnalyzer _xpathAnalyzer;
        private ProtoTemplate _currentTemplate;

        // Type donor of the last analyzed VarPar. Used for optimization of WithParam's.
        private VarPar _typeDonor;

        // Template dependencies
        // rev/fwd - Callee-to-Coller/Coller-to-Callee
        // 0/1     - for-each depth
        private Graph<ProtoTemplate> _revCall0Graph = new Graph<ProtoTemplate>();
        private Graph<ProtoTemplate> _revCall1Graph = new Graph<ProtoTemplate>();
        private Dictionary<Template, Stylesheet> _fwdApplyImportsGraph = new Dictionary<Template, Stylesheet>();
        private Dictionary<QilName, List<ProtoTemplate>> _revApplyTemplatesGraph = new Dictionary<QilName, List<ProtoTemplate>>();

        // Data flow graph
        private Graph<VarPar> _dataFlow = new Graph<VarPar>();

        // Mapping (mode, param name) -> helper vertex in data flow graph
        private Dictionary<ModeName, VarPar> _applyTemplatesParams = new Dictionary<ModeName, VarPar>();

        // ---------------------------------- Graph<V> ----------------------------------
        /// <summary>
        /// Represents a graph using hashtable of adjacency lists.
        /// </summary>
        /// <typeparam name="V">Vertex type</typeparam>
        internal class Graph<V> : Dictionary<V, List<V>>
            where V : XslNode
        {
            private static IList<V> s_empty = (new List<V>()).AsReadOnly();

            public IEnumerable<V> GetAdjList(V v)
            {
                List<V> adjList;
                if (TryGetValue(v, out adjList) && adjList != null)
                {
                    return adjList;
                }
                return s_empty;
            }

            public void AddEdge(V v1, V v2)
            {
                // Ignore loops
                if ((object)v1 == (object)v2)
                {
                    return;
                }

                List<V> adjList;
                if (!TryGetValue(v1, out adjList) || adjList == null)
                {
                    adjList = this[v1] = new List<V>();
                }

                // NOTE: We do not check for duplicate edges here
                adjList.Add(v2);
                if (!TryGetValue(v2, out adjList))
                {
                    this[v2] = null;
                }

                Debug.WriteLineIf(DiagnosticsSwitches.XslTypeInference.TraceVerbose, v1.TraceName + " -> " + v2.TraceName);
            }

            public void PropagateFlag(XslFlags flag)
            {
                // Clean Stop flags
                foreach (V v in Keys)
                {
                    v.Flags &= ~XslFlags.Stop;
                }

                foreach (V v in Keys)
                {
                    if ((v.Flags & XslFlags.Stop) == 0)
                    {
                        if ((v.Flags & flag) != 0)
                        {
                            DepthFirstSearch(v, flag);
                        }
                    }
                }
            }

            private void DepthFirstSearch(V v, XslFlags flag)
            {
                Debug.Assert((v.Flags & XslFlags.Stop) == 0, "Already visited this vertex");
                v.Flags |= (flag | XslFlags.Stop);
                foreach (V u in GetAdjList(v))
                {
                    if ((u.Flags & XslFlags.Stop) == 0)
                    {
                        DepthFirstSearch(u, flag);
                    }
                    Debug.Assert((u.Flags & flag) == flag, "Flag was not set on an adjacent vertex");
                }
            }
        }

        internal struct ModeName
        {
            public QilName Mode;
            public QilName Name;

            public ModeName(QilName mode, QilName name)
            {
                this.Mode = mode;
                this.Name = name;
            }

            public override int GetHashCode()
            {
                return Mode.GetHashCode() ^ Name.GetHashCode();
            }
        }

        public XslFlags Analyze(Compiler compiler)
        {
            _compiler = compiler;
            _scope = new CompilerScopeManager<VarPar>();
            _xpathAnalyzer = new XPathAnalyzer(compiler, _scope);

            // Add global parameters and variables to the scope, they are visible everywhere
            foreach (VarPar par in compiler.ExternalPars)
            {
                _scope.AddVariable(par.Name, par);
            }
            foreach (VarPar var in compiler.GlobalVars)
            {
                _scope.AddVariable(var.Name, var);
            }

            // Visit global parameters and variables, but ignore calculated flags
            foreach (VarPar par in compiler.ExternalPars)
            {
                Visit(par);
                par.Flags |= XslFlags.AnyType;
            }
            foreach (VarPar var in compiler.GlobalVars)
            {
                Visit(var);
            }

            // Global "naked" current/position/last flags
            XslFlags result = XslFlags.None;

            // Visit templates and attribute sets
            foreach (ProtoTemplate tmpl in compiler.AllTemplates)
            {
                _currentTemplate = tmpl;
                result |= Visit(tmpl);
            }

            // At this point for every local parameter we know whether its default value could be used
            // by one of the callers of its template. Update flags for local parameters accordingly.
            foreach (ProtoTemplate tmpl in compiler.AllTemplates)
            {
                foreach (XslNode instr in tmpl.Content)
                {
                    // Take care of a bizarre case <xsl:template match="/" xml:space="preserve">  <xsl:param name="par"/>
                    if (instr.NodeType == XslNodeType.Text)
                    {
                        continue;
                    }
                    if (instr.NodeType != XslNodeType.Param)
                    {
                        break;
                    }

                    VarPar par = (VarPar)instr;
                    if ((par.Flags & XslFlags.MayBeDefault) != 0)
                    {
                        par.Flags |= par.DefValueFlags;
                    }
                }
            }

            // Infer XPath types for all variables and local parameters by propagating literal
            // types Rtf, Nodeset, Node, Boolean, Number, String through the data flow graph.
            for (int flag = (int)XslFlags.Rtf; flag != 0; flag >>= 1)
            {
                _dataFlow.PropagateFlag((XslFlags)flag);
            }
            _dataFlow = null;

            // We need to follow revCall0Graph graph to propagate focus flags. But first complete
            // dependency graph with fwdApplyImportsGraph
            foreach (KeyValuePair<Template, Stylesheet> pair in _fwdApplyImportsGraph)
            {
                foreach (Stylesheet import in pair.Value.Imports)
                {
                    AddImportDependencies(import, /*focusDonor:*/pair.Key);
                }
            }
            _fwdApplyImportsGraph = null; // Finaly done with this.

            if ((result & XslFlags.Current) != 0)
            {
                _revCall0Graph.PropagateFlag(XslFlags.Current);
            }
            if ((result & XslFlags.Position) != 0)
            {
                _revCall0Graph.PropagateFlag(XslFlags.Position);
            }
            if ((result & XslFlags.Last) != 0)
            {
                _revCall0Graph.PropagateFlag(XslFlags.Last);
            }
            if ((result & XslFlags.SideEffects) != 0)
            {
                PropagateSideEffectsFlag();
            }
            _revCall0Graph = null;
            _revCall1Graph = null;
            _revApplyTemplatesGraph = null;

            // We can do this only after all flags were propagated.
            // Otherwise we can miss case when flag comes to template from attribute-set
            FillModeFlags(compiler.Root.ModeFlags, compiler.Root.Imports[0]);

            TraceResults();
            return result;
        }

        private void AddImportDependencies(Stylesheet sheet, Template focusDonor)
        {
            foreach (Template tmpl in sheet.Templates)
            {
                if (tmpl.Mode.Equals(focusDonor.Mode))
                {
                    _revCall0Graph.AddEdge(tmpl, focusDonor);
                }
            }
            foreach (Stylesheet import in sheet.Imports)
            {
                AddImportDependencies(import, focusDonor);
            }
        }

        private void FillModeFlags(Dictionary<QilName, XslFlags> parentModeFlags, Stylesheet sheet)
        {
            // Recursion: Process all imports to calculate ModeFlags for apply-import in this sheet
            foreach (Stylesheet import in sheet.Imports)
            {
                FillModeFlags(sheet.ModeFlags, import);
            }
            // My parent depends on my templates and templates imported
            // 1. Copy ModeFlags of my imports to my parent
            foreach (KeyValuePair<QilName, XslFlags> modeFlag in sheet.ModeFlags)
            {
                XslFlags modeFlags;
                if (!parentModeFlags.TryGetValue(modeFlag.Key, out modeFlags))
                {
                    modeFlags = 0;
                }
                parentModeFlags[modeFlag.Key] = modeFlags | modeFlag.Value;
            }
            // 2. Add ModeFlags of my templates to my parent
            foreach (Template tmpl in sheet.Templates)
            {
                Debug.Assert(tmpl.Match != null);
                XslFlags templateFlags = tmpl.Flags & (XslFlags.FocusFilter | XslFlags.SideEffects);
                if (templateFlags != 0)
                {
                    XslFlags modeFlags;
                    if (!parentModeFlags.TryGetValue(tmpl.Mode, out modeFlags))
                    {
                        modeFlags = 0;
                    }
                    parentModeFlags[tmpl.Mode] = modeFlags | templateFlags;
                }
            }
        }

        private void TraceResults()
        {
#if DEBUG
            if (DiagnosticsSwitches.XslTypeInference.TraceVerbose)
            {
                Debug.WriteLine(string.Empty);
                foreach (ProtoTemplate tmpl in _compiler.AllTemplates)
                {
                    Debug.WriteLine(tmpl.TraceName + " = " + (tmpl.Flags & XslFlags.FocusFilter));
                }

                Debug.WriteLine(string.Empty);
                foreach (VarPar varPar in _allVarPars)
                {
                    Debug.WriteLine(varPar.TraceName + " = " + (varPar.Flags & XslFlags.TypeFilter));
                }
                Debug.WriteLine(string.Empty);
            }

            if (DiagnosticsSwitches.XslTypeInference.TraceInfo)
            {
                int current = 0, position = 0, last = 0;

                foreach (ProtoTemplate tmpl in _compiler.AllTemplates)
                {
                    if ((tmpl.Flags & XslFlags.Current) != 0)
                    {
                        current++;
                    }
                    if ((tmpl.Flags & XslFlags.Position) != 0)
                    {
                        position++;
                    }
                    if ((tmpl.Flags & XslFlags.Last) != 0)
                    {
                        last++;
                    }
                }

                int stringType = 0, numberType = 0, booleanType = 0, nodeNotRtfType = 0, nodesetNotRtfType = 0;
                int nodeType = 0, nodesetType = 0, noneType = 0, anyType = 0, totalVarPars = 0;

                foreach (VarPar varPar in _allVarPars)
                {
                    switch (varPar.Flags & XslFlags.TypeFilter)
                    {
                        case XslFlags.String: stringType++; break;
                        case XslFlags.Number: numberType++; break;
                        case XslFlags.Boolean: booleanType++; break;
                        case XslFlags.Node: nodeNotRtfType++; break;
                        case XslFlags.Nodeset: nodesetNotRtfType++; break;
                        case XslFlags.Rtf: nodeType++; break;
                        case XslFlags.Node | XslFlags.Rtf: nodeType++; break;
                        case XslFlags.Node | XslFlags.Nodeset: nodesetNotRtfType++; break;
                        case XslFlags.Nodeset | XslFlags.Rtf: nodesetType++; break;
                        case XslFlags.Node | XslFlags.Nodeset | XslFlags.Rtf: nodesetType++; break;
                        case XslFlags.None: noneType++; break;
                        default: anyType++; break;
                    }
                    totalVarPars++;
                }

                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Total => templates/attribute-sets: {0}, variables/parameters: {1}.",
                    _compiler.AllTemplates.Count, totalVarPars
                ));

                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Inferred focus => current: {0}, position: {1}, last: {2}.",
                    current, position, last
                ));

                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Inferred types => string: {0}, number: {1}, boolean: {2}, node: {3}, node-set: {4}, " +
                    "node-or-rtf: {5}, node-set-or-rtf: {6}, none: {7}, any: {8}.",
                    stringType, numberType, booleanType, nodeNotRtfType, nodesetNotRtfType,
                    nodeType, nodesetType, noneType, anyType
                ));
            }
#endif
        }

        protected override XslFlags Visit(XslNode node)
        {
            _scope.EnterScope(node.Namespaces);
            XslFlags result = base.Visit(node);
            _scope.ExitScope();

            // Local variables and parameters must be added to the outer scope
            if (_currentTemplate != null && (node.NodeType == XslNodeType.Variable || node.NodeType == XslNodeType.Param))
            {
                _scope.AddVariable(node.Name, (VarPar)node);
            }
            Debug.Assert(
                (result & XslFlags.TypeFilter & ~XslFlags.Rtf) == 0,
                "Instructions always return Rtf. node=" + node.NodeType.ToString() + " result=" + result.ToString()
            );
            return result;
        }

        protected override XslFlags VisitChildren(XslNode node)
        {
            XslFlags result = XslFlags.None;
            foreach (var child in node.Content)
            {
                // Visit this child (recurses)
                result |= this.Visit(child);
            }
            return result;
        }

        protected override XslFlags VisitAttributeSet(AttributeSet node)
        {
            // @use-attribute-sets was processed into a sequence of UseAttributeSet nodes,
            // which were prepended to the content of node
            node.Flags = VisitChildren(node);
            return node.Flags;
        }

        protected override XslFlags VisitTemplate(Template node)
        {
            // @match does not affect any flags
            //ProcessPattern(match);
            node.Flags = VisitChildren(node);
            return node.Flags;
        }

        protected override XslFlags VisitApplyImports(XslNode node)
        {
            Debug.Assert(_forEachDepth == 0, "xsl:apply-imports cannot be inside of xsl:for-each");
            Debug.Assert(_currentTemplate is Template, "xsl:apply-imports can only occur within xsl:template");
            _fwdApplyImportsGraph[(Template)_currentTemplate] = (Stylesheet)node.Arg;
            // xsl:apply-imports uses context node and is not in context of any for-each so it requires current
            return XslFlags.HasCalls | XslFlags.Current | XslFlags.Rtf;
        }

        protected override XslFlags VisitApplyTemplates(XslNode node)
        {
            Debug.Assert(node.Select != null, "Absent @select should be replaced with 'node()' in XsltLoader");
            XslFlags result = ProcessExpr(node.Select);

            foreach (XslNode instr in node.Content)
            {
                result |= Visit(instr);
                if (instr.NodeType == XslNodeType.WithParam)
                {
                    ModeName mn = new ModeName(/*mode:*/node.Name, instr.Name);
                    VarPar modePar;

                    if (!_applyTemplatesParams.TryGetValue(mn, out modePar))
                    {
                        modePar = _applyTemplatesParams[mn] = AstFactory.WithParam(instr.Name);
                    }

                    if (_typeDonor != null)
                    {
                        _dataFlow.AddEdge(_typeDonor, modePar);
                    }
                    else
                    {
                        modePar.Flags |= instr.Flags & XslFlags.TypeFilter;
                    }
                }
            }

            if (_currentTemplate != null)
            {
                AddApplyTemplatesEdge(/*mode:*/node.Name, _currentTemplate);
            }

            return XslFlags.HasCalls | XslFlags.Rtf | result;
        }

        protected override XslFlags VisitAttribute(NodeCtor node)
        {
            return (
                XslFlags.Rtf |
                ProcessAvt(node.NameAvt) |
                ProcessAvt(node.NsAvt) |
                VisitChildren(node)
            );
        }

        protected override XslFlags VisitCallTemplate(XslNode node)
        {
            XslFlags result = XslFlags.None;
            Template target;

            if (!_compiler.NamedTemplates.TryGetValue(node.Name, out target))
            {
                Debug.WriteLineIf(DiagnosticsSwitches.XslTypeInference.TraceError, "Unknown template " + node.Name.QualifiedName, "Error");
            }
            else
            {
                Debug.Assert(target != null);
                if (_currentTemplate != null)
                {
                    if (_forEachDepth == 0)
                    {
                        // Naked xsl:call-template, target would take its focus from currentTemplate
                        _revCall0Graph.AddEdge(target, _currentTemplate);
                    }
                    else
                    {
                        // in other cases we need it as donor for side effects flag
                        _revCall1Graph.AddEdge(target, _currentTemplate);
                    }
                }
            }

            VarPar[] typeDonors = new VarPar[node.Content.Count];
            int idx = 0;

            foreach (XslNode instr in node.Content)
            {
                Debug.Assert(instr.NodeType == XslNodeType.WithParam);
                result |= Visit(instr);
                typeDonors[idx++] = _typeDonor;
            }

            // For each xsl:param in the target template find the corresponding xsl:with-param, and:
            //   a) if the type of xsl:with-param is known, add it to the type of xsl:param;
            //   b) if value of xsl:with-param is a VarPar reference, add an edge connecting it with xsl:param
            //      to the data flow graph.

            if (target != null)
            {
                foreach (XslNode instr in target.Content)
                {
                    // Take care of a bizarre case <xsl:template match="/" xml:space="preserve">  <xsl:param name="par"/>
                    if (instr.NodeType == XslNodeType.Text)
                    {
                        continue;
                    }
                    if (instr.NodeType != XslNodeType.Param)
                    {
                        break;
                    }

                    VarPar par = (VarPar)instr;
                    VarPar found = null;
                    idx = 0;

                    foreach (XslNode withPar in node.Content)
                    {
                        if (withPar.Name.Equals(par.Name))
                        {
                            found = (VarPar)withPar;
                            _typeDonor = typeDonors[idx];
                            break;
                        }
                        idx++;
                    }

                    if (found != null)
                    {
                        // Found corresponding xsl:with-param, check its type
                        if (_typeDonor != null)
                        {
                            // add an edge from its type donor to xsl:param
                            _dataFlow.AddEdge(_typeDonor, par);
                        }
                        else
                        {
                            par.Flags |= found.Flags & XslFlags.TypeFilter;
                        }
                    }
                    else
                    {
                        // No value was specified for this xsl:param, default value will be used for it
                        par.Flags |= XslFlags.MayBeDefault;
                    }
                }
            }

            return XslFlags.HasCalls | XslFlags.Rtf | result;
        }

        //protected override XslFlags VisitChoose(XslNode node) { return VisitChildren(node); }

        protected override XslFlags VisitComment(XslNode node)
        {
            return XslFlags.Rtf | VisitChildren(node);
        }

        protected override XslFlags VisitCopy(XslNode node)
        {
            // @use-attribute-sets was processed into a sequence of UseAttributeSet nodes,
            // which were prepended to the content of node
            return XslFlags.Current | XslFlags.Rtf | VisitChildren(node);
        }

        protected override XslFlags VisitCopyOf(XslNode node)
        {
            return XslFlags.Rtf | ProcessExpr(node.Select);
        }

        protected override XslFlags VisitElement(NodeCtor node)
        {
            // @use-attribute-sets was processed into a sequence of UseAttributeSet nodes,
            // which were prepended to the content of node
            return (
                XslFlags.Rtf |
                ProcessAvt(node.NameAvt) |
                ProcessAvt(node.NsAvt) |
                VisitChildren(node)
            );
        }

        protected override XslFlags VisitError(XslNode node)
        {
            return (VisitChildren(node) & ~XslFlags.TypeFilter) | XslFlags.SideEffects;
        }

        protected override XslFlags VisitForEach(XslNode node)
        {
            XslFlags result = ProcessExpr(node.Select);
            _forEachDepth++;
            foreach (XslNode child in node.Content)
            {
                if (child.NodeType == XslNodeType.Sort)
                {
                    result |= Visit(child);
                }
                else
                {
                    // Since for-each creates new focus, the focus flags of its children does not contribute into result
                    result |= Visit(child) & ~XslFlags.FocusFilter;
                }
            }
            _forEachDepth--;
            return result;
        }

        protected override XslFlags VisitIf(XslNode node)
        {
            return ProcessExpr(node.Select) | VisitChildren(node);
        }

        /*
        protected override XslFlags VisitKey(Key node) {
            // @match and @use do not affect any flags
            //ProcessPattern(node.Match);
            //ProcessExpr(node.Use);
        }
        */

        //protected override XslFlags VisitList(XslNode node) { return VisitChildren(node); }

        protected override XslFlags VisitLiteralAttribute(XslNode node)
        {
            return (
                XslFlags.Rtf |
                ProcessAvt(node.Select) |
                VisitChildren(node)
            );
        }

        protected override XslFlags VisitLiteralElement(XslNode node)
        {
            return XslFlags.Rtf | VisitChildren(node);
        }

        protected override XslFlags VisitMessage(XslNode node)
        {
            return (VisitChildren(node) & ~XslFlags.TypeFilter) | XslFlags.SideEffects;
        }

        //protected override XslFlags VisitNop(XslNode node) { return VisitChildren(node); }

        protected override XslFlags VisitNumber(Number node)
        {
            return (
                XslFlags.Rtf |
                ProcessPattern(node.Count) |
                ProcessPattern(node.From) |
                (node.Value != null ? ProcessExpr(node.Value) : XslFlags.Current) |
                ProcessAvt(node.Format) |
                ProcessAvt(node.Lang) |
                ProcessAvt(node.LetterValue) |
                ProcessAvt(node.GroupingSeparator) |
                ProcessAvt(node.GroupingSize)
            );
        }

        //protected override XslFlags VisitOtherwise(XslNode node) { return VisitChildren(node); }

        protected override XslFlags VisitPI(XslNode node)
        {
            return (
                XslFlags.Rtf |
                ProcessAvt(node.Select) |
                VisitChildren(node)
            );
        }

        protected override XslFlags VisitSort(Sort node)
        {
            return (
                // @select is calculated in context of xsl:for-each or xsl:apply-templates,
                // so it does not affect focus flags
                ProcessExpr(node.Select) & ~XslFlags.FocusFilter |
                ProcessAvt(node.Lang) |
                ProcessAvt(node.DataType) |
                ProcessAvt(node.Order) |
                ProcessAvt(node.CaseOrder)
            );
        }

        protected override XslFlags VisitText(Text node)
        {
            return XslFlags.Rtf | VisitChildren(node);
        }

        protected override XslFlags VisitUseAttributeSet(XslNode node)
        {
            AttributeSet attSet;
            if (!_compiler.AttributeSets.TryGetValue(node.Name, out attSet))
            {
                Debug.WriteLineIf(DiagnosticsSwitches.XslTypeInference.TraceError, "Unknown attribute-set " + node.Name.QualifiedName, "Error");
            }
            else if (_currentTemplate != null)
            {
                if (_forEachDepth == 0)
                {
                    // Naked [xsl:]use-attribute-sets, attSet would take its focus from currentTemplate
                    _revCall0Graph.AddEdge(attSet, _currentTemplate);
                }
                else
                {
                    // in other cases we need it as donor for side effects flag
                    _revCall1Graph.AddEdge(attSet, _currentTemplate);
                }
            }
            return XslFlags.HasCalls | XslFlags.Rtf;
        }

        protected override XslFlags VisitValueOf(XslNode node)
        {
            return XslFlags.Rtf | ProcessExpr(node.Select);
        }

        protected override XslFlags VisitValueOfDoe(XslNode node)
        {
            return XslFlags.Rtf | ProcessExpr(node.Select);
        }

        protected override XslFlags VisitParam(VarPar node)
        {
            Template tmpl = _currentTemplate as Template;
            if (tmpl != null && tmpl.Match != null)
            {
                // This template has 'match' attribute and might be called from built-in template rules,
                // all xsl:param's will be defaulted in that case
                node.Flags |= XslFlags.MayBeDefault;

                ModeName mn = new ModeName(tmpl.Mode, node.Name);
                VarPar par;

                if (!_applyTemplatesParams.TryGetValue(mn, out par))
                {
                    par = _applyTemplatesParams[mn] = AstFactory.WithParam(node.Name);
                }
                _dataFlow.AddEdge(par, node);
            }
            node.DefValueFlags = ProcessVarPar(node);
            return node.DefValueFlags & ~XslFlags.TypeFilter;
        }

        protected override XslFlags VisitVariable(VarPar node)
        {
            node.Flags = ProcessVarPar(node);
            return node.Flags & ~XslFlags.TypeFilter;
        }

        protected override XslFlags VisitWithParam(VarPar node)
        {
            node.Flags = ProcessVarPar(node);
            return node.Flags & ~XslFlags.TypeFilter;
        }

        private XslFlags ProcessVarPar(VarPar node)
        {
            XslFlags result;
#if DEBUG
            if (node.NodeType != XslNodeType.WithParam)
            {
                _allVarPars.Add(node);
            }
#endif

            if (node.Select != null)
            {
                if (node.Content.Count != 0)
                {
                    // In case of incorrect stylesheet, variable or parameter may have both a 'select' attribute and non-empty content
                    // NOTE: This code must be in sync with recovery logic in QilGenerator
                    result = _xpathAnalyzer.Analyze(node.Select) | VisitChildren(node) | XslFlags.AnyType;
                    _typeDonor = null;
                }
                else
                {
                    result = _xpathAnalyzer.Analyze(node.Select);
                    _typeDonor = _xpathAnalyzer.TypeDonor;
                    if (_typeDonor != null && node.NodeType != XslNodeType.WithParam)
                    {
                        _dataFlow.AddEdge(_typeDonor, node);
                    }
                }
            }
            else if (node.Content.Count != 0)
            {
                result = XslFlags.Rtf | VisitChildren(node);
                _typeDonor = null;
            }
            else
            {
                result = XslFlags.String;
                _typeDonor = null;
            }
            return result;
        }

        // Ignores XPath type flags
        private XslFlags ProcessExpr(string expr)
        {
            return _xpathAnalyzer.Analyze(expr) & ~XslFlags.TypeFilter;
        }

        // Ignores XPath type flags
        private XslFlags ProcessAvt(string avt)
        {
            return _xpathAnalyzer.AnalyzeAvt(avt) & ~XslFlags.TypeFilter;
        }

        // Ignores XPath type flags and focus flags
        private XslFlags ProcessPattern(string pattern)
        {
            // We need to analyze using of variables in the pattern
            return _xpathAnalyzer.Analyze(pattern) & ~XslFlags.TypeFilter & ~XslFlags.FocusFilter;
        }

        private void AddApplyTemplatesEdge(QilName mode, ProtoTemplate dependentTemplate)
        {
            List<ProtoTemplate> templates;
            if (!_revApplyTemplatesGraph.TryGetValue(mode, out templates))
            {
                templates = new List<ProtoTemplate>();
                _revApplyTemplatesGraph.Add(mode, templates);
            }
            else
            {
                if (templates[templates.Count - 1] == dependentTemplate)
                {
                    return; // this is a duplicate
                }
            }

            templates.Add(dependentTemplate);
        }

        private void PropagateSideEffectsFlag()
        {
            // Clean Stop flags
            foreach (ProtoTemplate t in _revCall0Graph.Keys)
            {
                t.Flags &= ~XslFlags.Stop;
            }
            foreach (ProtoTemplate t in _revCall1Graph.Keys)
            {
                t.Flags &= ~XslFlags.Stop;
            }

            foreach (ProtoTemplate t in _revCall0Graph.Keys)
            {
                if ((t.Flags & XslFlags.Stop) == 0)
                {
                    if ((t.Flags & XslFlags.SideEffects) != 0)
                    {
                        DepthFirstSearch(t);
                    }
                }
            }
            foreach (ProtoTemplate t in _revCall1Graph.Keys)
            {
                if ((t.Flags & XslFlags.Stop) == 0)
                {
                    if ((t.Flags & XslFlags.SideEffects) != 0)
                    {
                        DepthFirstSearch(t);
                    }
                }
            }
        }

        private void DepthFirstSearch(ProtoTemplate t)
        {
            Debug.Assert((t.Flags & XslFlags.Stop) == 0, "Already visited this vertex");
            t.Flags |= (XslFlags.SideEffects | XslFlags.Stop);
            List<ProtoTemplate> list;
            foreach (ProtoTemplate u in _revCall0Graph.GetAdjList(t))
            {
                if ((u.Flags & XslFlags.Stop) == 0)
                {
                    DepthFirstSearch(u);
                }
                Debug.Assert((u.Flags & XslFlags.SideEffects) == XslFlags.SideEffects, "Flag was not set on an adjacent vertex");
            }
            foreach (ProtoTemplate u in _revCall1Graph.GetAdjList(t))
            {
                if ((u.Flags & XslFlags.Stop) == 0)
                {
                    DepthFirstSearch(u);
                }
                Debug.Assert((u.Flags & XslFlags.SideEffects) == XslFlags.SideEffects, "Flag was not set on an adjacent vertex");
            }
            Template template = t as Template;
            if (
                template != null &&                                     // This ProteTemplate is Template
                _revApplyTemplatesGraph.TryGetValue(template.Mode, out list)      // list - ProtoTemplates that have apply-templatess mode="{template.Mode}"
            )
            {
                _revApplyTemplatesGraph.Remove(template.Mode);                    // to prevent recursion remove this list from dictionary
                foreach (ProtoTemplate u in list)
                {
                    if ((u.Flags & XslFlags.Stop) == 0)
                    {
                        DepthFirstSearch(u);
                    }
                    Debug.Assert((u.Flags & XslFlags.SideEffects) == XslFlags.SideEffects, "Flag was not set on an adjacent vertex");
                }
            }
        }

        // ------------------------------- XPathAnalyzer --------------------------------

        // Ignores all errors and warnings
        internal readonly struct NullErrorHelper : IErrorHelper
        {
            public void ReportError(string res, params string[] args) { }
            public void ReportWarning(string res, params string[] args) { }
        }

        internal class XPathAnalyzer : IXPathBuilder<XslFlags>
        {
            private XPathParser<XslFlags> _xpathParser = new XPathParser<XslFlags>();
            private CompilerScopeManager<VarPar> _scope;
            private Compiler _compiler;

            // True if the expression needs XSLT's current() node
            private bool _xsltCurrentNeeded;

            // If the expression is just a reference to some VarPar, like "(($foo))",
            // then this field contains that VarPar, and null otherwise.
            private VarPar _typeDonor;

            public VarPar TypeDonor
            {
                get { return _typeDonor; }
            }

            public XPathAnalyzer(Compiler compiler, CompilerScopeManager<VarPar> scope)
            {
                _compiler = compiler;
                _scope = scope;
            }

            public XslFlags Analyze(string xpathExpr)
            {
                _typeDonor = null;
                if (xpathExpr == null)
                {
                    return XslFlags.None;
                }
                try
                {
                    // Note that the constructor may throw an exception, for example, in case of the expression "'"
                    _xsltCurrentNeeded = false;
                    XPathScanner scanner = new XPathScanner(xpathExpr);
                    XslFlags result = _xpathParser.Parse(scanner, this, LexKind.Eof);
                    if (_xsltCurrentNeeded)
                    {
                        result |= XslFlags.Current;
                    }
                    return result;
                }
                catch (XslLoadException)
                {
                    return XslFlags.AnyType | XslFlags.FullFocus;
                }
            }

            public XslFlags AnalyzeAvt(string source)
            {
                _typeDonor = null;
                if (source == null)
                {
                    return XslFlags.None;
                }
                try
                {
                    _xsltCurrentNeeded = false;
                    XslFlags result = XslFlags.None;
                    int pos = 0;
                    while (pos < source.Length)
                    {
                        pos = source.IndexOf('{', pos);
                        if (pos == -1)
                        {
                            break; // no more AVTs
                        }
                        pos++;
                        if (pos < source.Length && source[pos] == '{')
                        { // "{{"
                            pos++;
                            continue;
                        }
                        if (pos < source.Length)
                        { // '{' encountered, parse an expression
                            XPathScanner scanner = new XPathScanner(source, pos);
                            result |= _xpathParser.Parse(scanner, this, LexKind.RBrace);
                            pos = scanner.LexStart + 1;
                        }
                    }
                    if (_xsltCurrentNeeded)
                    {
                        result |= XslFlags.Current;
                    }
                    return result & ~XslFlags.TypeFilter;
                }
                catch (XslLoadException)
                {
                    return XslFlags.FullFocus;
                }
            }

            // Returns null in case of error
            private VarPar ResolveVariable(string prefix, string name)
            {
                string ns = ResolvePrefix(prefix);
                if (ns == null)
                {
                    return null;
                }
                return _scope.LookupVariable(name, ns);
            }

            // Returns null in case of error
            private string ResolvePrefix(string prefix)
            {
                // ignoreDefaultNs == true
                if (prefix.Length == 0)
                {
                    return string.Empty;
                }
                else
                {
                    return _scope.LookupNamespace(prefix);
                }
            }

            public virtual void StartBuild()
            {
            }

            public virtual XslFlags EndBuild(XslFlags result)
            {
                return result;
            }

            public virtual XslFlags String(string value)
            {
                _typeDonor = null;
                return XslFlags.String;
            }

            public virtual XslFlags Number(double value)
            {
                _typeDonor = null;
                return XslFlags.Number;
            }

            private static XslFlags[] s_operatorType = {
                /*Unknown   */ XslFlags.AnyType,
                /*Or        */ XslFlags.Boolean,
                /*And       */ XslFlags.Boolean,
                /*Eq        */ XslFlags.Boolean,
                /*Ne        */ XslFlags.Boolean,
                /*Lt        */ XslFlags.Boolean,
                /*Le        */ XslFlags.Boolean,
                /*Gt        */ XslFlags.Boolean,
                /*Ge        */ XslFlags.Boolean,
                /*Plus      */ XslFlags.Number ,
                /*Minus     */ XslFlags.Number ,
                /*Multiply  */ XslFlags.Number ,
                /*Divide    */ XslFlags.Number ,
                /*Modulo    */ XslFlags.Number ,
                /*UnaryMinus*/ XslFlags.Number ,
                /*Union     */ XslFlags.Nodeset,
            };

            public virtual XslFlags Operator(XPathOperator op, XslFlags left, XslFlags right)
            {
                _typeDonor = null;
                Debug.Assert(op != XPathOperator.Unknown);
                XslFlags result = (left | right) & ~XslFlags.TypeFilter;
                return result | s_operatorType[(int)op];
            }

            public virtual XslFlags Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name)
            {
                _typeDonor = null;
                if (xpathAxis == XPathAxis.Self && nodeType == XPathNodeType.All && prefix == null && name == null)
                {
                    return XslFlags.Current | XslFlags.Node;
                }
                else
                {
                    return XslFlags.Current | XslFlags.Nodeset;
                }
            }

            // "left/right"
            public virtual XslFlags JoinStep(XslFlags left, XslFlags right)
            {
                _typeDonor = null;
                return (left & ~XslFlags.TypeFilter) | XslFlags.Nodeset; // "ex:Foo(position())/Bar"
            }

            // "nodeset[predicate]"
            public virtual XslFlags Predicate(XslFlags nodeset, XslFlags predicate, bool isReverseStep)
            {
                _typeDonor = null;
                return (nodeset & ~XslFlags.TypeFilter) | XslFlags.Nodeset | (predicate & XslFlags.SideEffects); // "ex:Foo(position())[Bar]"
            }

            public virtual XslFlags Variable(string prefix, string name)
            {
                _typeDonor = ResolveVariable(prefix, name);
                if (_typeDonor == null)
                {
                    Debug.WriteLineIf(DiagnosticsSwitches.XslTypeInference.TraceError, "Unresolved variable " + Compiler.ConstructQName(prefix, name), "Error");
                    return XslFlags.AnyType;
                }
                return XslFlags.None;
            }

            public virtual XslFlags Function(string prefix, string name, IList<XslFlags> args)
            {
                _typeDonor = null;

                XslFlags argsFlags = XslFlags.None;
                foreach (XslFlags t in args)
                {
                    argsFlags |= t;
                }

                XslFlags funcFlags = XslFlags.None;

                if (prefix.Length == 0)
                {
                    XPathFunctionInfo xpathFunc;
                    XsltFunctionInfo xsltFunc;

                    if (XPathBuilder.FunctionTable.TryGetValue(name, out xpathFunc))
                    {
                        XPathBuilder.FuncId funcId = xpathFunc.id;
                        funcFlags = s_XPathFunctionFlags[(int)funcId];
                        if (args.Count == 0 && (
                            funcId == XPathBuilder.FuncId.LocalName ||
                            funcId == XPathBuilder.FuncId.NamespaceUri ||
                            funcId == XPathBuilder.FuncId.Name ||
                            funcId == XPathBuilder.FuncId.String ||
                            funcId == XPathBuilder.FuncId.Number ||
                            funcId == XPathBuilder.FuncId.StringLength ||
                            funcId == XPathBuilder.FuncId.Normalize
                        ))
                        {
                            funcFlags |= XslFlags.Current;
                        }
                    }
                    else if (QilGenerator.FunctionTable.TryGetValue(name, out xsltFunc))
                    {
                        QilGenerator.FuncId funcId = xsltFunc.id;
                        funcFlags = s_xsltFunctionFlags[(int)funcId];
                        if (funcId == QilGenerator.FuncId.Current)
                        {
                            _xsltCurrentNeeded = true;
                        }
                        else if (funcId == QilGenerator.FuncId.GenerateId && args.Count == 0)
                        {
                            funcFlags |= XslFlags.Current;
                        }
                    }
                }
                else
                {
                    string ns = ResolvePrefix(prefix);
                    if (ns == XmlReservedNs.NsMsxsl)
                    {
                        switch (name)
                        {
                            case "node-set": funcFlags = XslFlags.Nodeset; break;
                            case "string-compare": funcFlags = XslFlags.Number; break;
                            case "utc": funcFlags = XslFlags.String; break;
                            case "format-date": funcFlags = XslFlags.String; break;
                            case "format-time": funcFlags = XslFlags.String; break;
                            case "local-name": funcFlags = XslFlags.String; break;
                            case "namespace-uri": funcFlags = XslFlags.String | XslFlags.Current; break;
                            case "number": funcFlags = XslFlags.Number; break;
                        }
                    }
                    else if (ns == XmlReservedNs.NsExsltCommon)
                    {
                        switch (name)
                        {
                            case "node-set": funcFlags = XslFlags.Nodeset; break;
                            case "object-type": funcFlags = XslFlags.String; break;
                        }
                    }

                    if (funcFlags == XslFlags.None)
                    {
                        // Unknown function. Can be script function or extension function
                        funcFlags = XslFlags.AnyType;
                        if (_compiler.Settings.EnableScript && ns != null)
                        {
                            XmlExtensionFunction scrFunc = _compiler.Scripts.ResolveFunction(name, ns, args.Count, new NullErrorHelper());
                            if (scrFunc != null)
                            {
                                XmlQueryType xt = scrFunc.XmlReturnType;
                                if (xt == TypeFactory.StringX)
                                {
                                    funcFlags = XslFlags.String;
                                }
                                else if (xt == TypeFactory.DoubleX)
                                {
                                    funcFlags = XslFlags.Number;
                                }
                                else if (xt == TypeFactory.BooleanX)
                                {
                                    funcFlags = XslFlags.Boolean;
                                }
                                else if (xt == TypeFactory.NodeNotRtf)
                                {
                                    funcFlags = XslFlags.Node;
                                }
                                else if (xt == TypeFactory.NodeSDod)
                                {
                                    funcFlags = XslFlags.Nodeset;
                                }
                                else if (xt == TypeFactory.ItemS)
                                {
                                    funcFlags = XslFlags.AnyType;
                                }
                                else if (xt == TypeFactory.Empty)
                                {
                                    funcFlags = XslFlags.Nodeset;
                                }
                                else
                                {
                                    Debug.Fail("Unexpected XmlQueryType for script function: " + xt.ToString());
                                }
                            }
                        }
                        funcFlags |= XslFlags.SideEffects;
                    }
                }

                return (argsFlags & ~XslFlags.TypeFilter) | funcFlags;
            }

            #region XPath Function Flags
            private static XslFlags[] s_XPathFunctionFlags = {
            /*Last              */ XslFlags.Number | XslFlags.Last,
            /*Position          */ XslFlags.Number | XslFlags.Position,
            /*Count             */ XslFlags.Number,
            /*LocalName         */ XslFlags.String, // | XslFlags.Current if 0 args
            /*NamespaceUri      */ XslFlags.String, // | XslFlags.Current if 0 args
            /*Name              */ XslFlags.String, // | XslFlags.Current if 0 args
            /*String            */ XslFlags.String, // | XslFlags.Current if 0 args
            /*Number            */ XslFlags.Number, // | XslFlags.Current if 0 args
            /*Boolean           */ XslFlags.Boolean,
            /*True              */ XslFlags.Boolean,
            /*False             */ XslFlags.Boolean,
            /*Not               */ XslFlags.Boolean,
            /*Id                */ XslFlags.Nodeset | XslFlags.Current,
            /*Concat            */ XslFlags.String,
            /*StartsWith        */ XslFlags.Boolean,
            /*Contains          */ XslFlags.Boolean,
            /*SubstringBefore   */ XslFlags.String,
            /*SubstringAfter    */ XslFlags.String,
            /*Substring         */ XslFlags.String,
            /*StringLength      */ XslFlags.Number, // | XslFlags.Current if 0 args
            /*Normalize         */ XslFlags.String, // | XslFlags.Current if 0 args
            /*Translate         */ XslFlags.String,
            /*Lang              */ XslFlags.Boolean | XslFlags.Current,
            /*Sum               */ XslFlags.Number,
            /*Floor             */ XslFlags.Number,
            /*Ceiling           */ XslFlags.Number,
            /*Round             */ XslFlags.Number,
        };
            #endregion

            #region Xslt Function Flags
            private static XslFlags[] s_xsltFunctionFlags = {
            /*Current           */ XslFlags.Node,   // xsltCurrentNeeded = true
            /*Document          */ XslFlags.Nodeset,
            /*Key               */ XslFlags.Nodeset | XslFlags.Current,
            /*FormatNumber      */ XslFlags.String,
            /*UnparsedEntityUri */ XslFlags.String, // | XslFlags.Current if it is implemented
            /*GenerateId        */ XslFlags.String, // | XslFlags.Current if 0 args
            /*SystemProperty    */ XslFlags.String | XslFlags.Number,
            /*ElementAvailable  */ XslFlags.Boolean,
            /*FunctionAvailable */ XslFlags.Boolean,
        };
            #endregion
        }
    }

    // ------------------------------- XslAstRewriter -------------------------------

    internal sealed class XslAstRewriter
    {
        private static readonly QilName s_nullMode = AstFactory.QName(string.Empty);

        private CompilerScopeManager<VarPar> _scope;
        private Stack<Template> _newTemplates;
        private Compiler _compiler;

        public void Rewrite(Compiler compiler)
        {
            _compiler = compiler;
            _scope = new CompilerScopeManager<VarPar>();
            _newTemplates = new Stack<Template>();

            // Rewrite every template
            foreach (var template in compiler.AllTemplates)
            {
                _scope.EnterScope();
                CheckNodeCost(template);
                _scope.CheckEmpty();
            }

            // Add the new templates to the compiled set
            while (_newTemplates.Count > 0)
            {
                var newtemplate = _newTemplates.Pop();

                // From Stylesheet.AddTemplate(newtemplate):
                compiler.AllTemplates.Add(newtemplate);
                compiler.NamedTemplates.Add(newtemplate.Name, newtemplate);

                _scope.EnterScope();
                CheckNodeCost(newtemplate);
                _scope.CheckEmpty();
            }
        }

        // Returns a cost based on an estimate of the number of locals required for the given expression
        private static int NodeCostForXPath(string xpath)
        {
            int cost = 0;
            if (xpath != null)
            {
                // Every XPath expression needs at least one iterator
                cost = IteratorNodeCost;
                // Count slashes, two characters at a time, ignore leading slash
                for (int t = 2; t < xpath.Length; t += 2)
                {
                    if (xpath[t] == '/' || xpath[t - 1] == '/')
                    {
                        cost += IteratorNodeCost;
                    }
                }
            }
            return cost;
        }

        // These values should be changed to achieve methods with ~2KB IL
        private const int FixedNodeCost = 1;        // should probably depend on node type
        private const int IteratorNodeCost = 2;     // XPath iterators are more expensive
        private const int CallTemplateCost = 1;     // best kept at a minimum, 1
        private const int RewriteThreshold = 100;

        // These are all the node types for which the .Select member has an XPath expression
        private const int NodesWithSelect =
            (1 << (int)XslNodeType.Param) |
            (1 << (int)XslNodeType.Variable) |
            (1 << (int)XslNodeType.WithParam) |
            (1 << (int)XslNodeType.ApplyTemplates) |
            (1 << (int)XslNodeType.CopyOf) |
            (1 << (int)XslNodeType.ForEach) |
            (1 << (int)XslNodeType.If) |
            //(1 << (int)XslNodeType.Number) |          // has XPath, but not in .Select member
            (1 << (int)XslNodeType.Sort) |
            (1 << (int)XslNodeType.ValueOf) |
            (1 << (int)XslNodeType.ValueOfDoe);

        // These are all the node types which can have call-template as a child and are therefor suitable for refactoring
        private const int ParentsOfCallTemplate =
            (1 << (int)XslNodeType.Attribute) |
            (1 << (int)XslNodeType.Comment) |
            (1 << (int)XslNodeType.Copy) |
            (1 << (int)XslNodeType.Element) |
            (1 << (int)XslNodeType.ForEach) |
            (1 << (int)XslNodeType.If) |                // also used for xsl:when
            (1 << (int)XslNodeType.Message) |
            (1 << (int)XslNodeType.Otherwise) |
            (1 << (int)XslNodeType.Param) |
            (1 << (int)XslNodeType.PI) |
            (1 << (int)XslNodeType.Template) |
            (1 << (int)XslNodeType.Variable) |
            (1 << (int)XslNodeType.WithParam) |
            (1 << (int)XslNodeType.LiteralAttribute) |
            (1 << (int)XslNodeType.LiteralElement);

        // Tests whether the specified XslNodeType bit is set in the provided flags
        private static bool NodeTypeTest(XslNodeType nodetype, int flags)
        {
            return ((flags >> (int)nodetype) & 1) != 0;
        }

        private int CheckNodeCost(XslNode node)
        {
            _scope.EnterScope(node.Namespaces);

            // We don't want to allow rewriting by default
            bool canRewrite = false;

            // Use a constant cost for all nodes (should probably depend on the node's type)
            int nodeCost = FixedNodeCost;

            // Detect the number of iterators used by the node's 'select' attribute
            if (NodeTypeTest(node.NodeType, NodesWithSelect))
            {
                nodeCost += NodeCostForXPath(node.Select);
            }

            // Iterate through all the child nodes
            var content = node.Content;
            int last = content.Count - 1;
            for (int t = 0; t <= last; ++t)
            {
                var child = content[t];

                var costForChild = CheckNodeCost(child);        // recurse

                nodeCost += costForChild;
                if (canRewrite && nodeCost > RewriteThreshold)
                {
                    // This child would overflow the limit for this scope; create a new scope

                    // Don't refactor the code if this is the last child and its cost is trivial
                    if (t < last || costForChild > CallTemplateCost)
                    {
                        //Debug.WriteLine("Node {0} (within {5}) on {1}:{2} has cost {3}; {4} total",
                        //    child.NodeType, child.SourceLine.Start.Line, child.SourceLine.Start.Pos, costForChild, nodeCost, node.NodeType);

                        // Refactor this node, moving the current child and all after into a new template
                        Refactor(node, t);

                        // The new template (containing the remainder of the current node) will be processed later
                        nodeCost -= costForChild;
                        nodeCost += CallTemplateCost;
                    }
                    break;
                }

                // Local variables and parameters must be added to the outer scope
                if (child.NodeType == XslNodeType.Variable || child.NodeType == XslNodeType.Param)
                {
                    _scope.AddVariable(child.Name, (VarPar)child);
                    // Parameters will cause code generation at the call-site, not in the callee
                    if (child.NodeType == XslNodeType.Param)
                    {
                        nodeCost -= costForChild;
                    }
                }
                else if (!canRewrite)
                {
                    // We're passed the parameters and our first real node; start checking the cost
                    // Note: some nodes like xsl:choose cannot contain xsl:call-template
                    canRewrite = NodeTypeTest(node.NodeType, ParentsOfCallTemplate);
                }
            }

            _scope.ExitScope();
            return nodeCost;
        }

        // Splits the children into two pieces: prefix and suffix
        // The prefix calls a new template T, which contains all of the suffix:
        // F=PREFIX~SUFFIX => F=PREFIX~C(T) and T=PARAMS~SUFFIX
        private void Refactor(XslNode parent, int split)
        {
            Debug.Assert(split > 0);
            var content = (List<XslNode>)parent.Content;

            var node = content[split];

            // Generate unique name for the new template
            QilName templatename = AstFactory.QName("generated", _compiler.CreatePhantomNamespace(), "compiler");

            // Create fake ContextInfo for the new nodes, based on the context for the old node
            var fakeCtxInfo = new XsltInput.ContextInfo(node.SourceLine);

            // Create the new call-template node
            var calltemplate = AstFactory.CallTemplate(templatename, fakeCtxInfo);
            XsltLoader.SetInfo(calltemplate, null, fakeCtxInfo);

            // Create a new template node
            Template newtemplate = AstFactory.Template(templatename, null, XsltLoader.nullMode, double.NaN, node.XslVersion);
            XsltLoader.SetInfo(newtemplate, null, fakeCtxInfo);
            _newTemplates.Push(newtemplate);

            // Pre-allocate the new content list to minimize the number of resizes (adding some space for any params)
            newtemplate.SetContent(new List<XslNode>(content.Count - split + 8));

            // Pass parameters from the current scope into the called template
            foreach (var scoperecord in _scope.GetActiveRecords())
            {
                if (!scoperecord.IsVariable)
                {
                    // The scope record is either a namespace declaration or an exclusion namespace
                    Debug.Assert(scoperecord.IsNamespace || scoperecord.ncName == null);
                    Debug.Assert(!_compiler.IsPhantomNamespace(scoperecord.nsUri));
                    newtemplate.Namespaces = new NsDecl(newtemplate.Namespaces, scoperecord.ncName, scoperecord.nsUri);
                }
                else
                {
                    // The scope contains a variable that we must pass into the new template
                    var variable = scoperecord.value;

                    // Skip variables generated during errors
                    if (_compiler.IsPhantomNamespace(variable.Name.NamespaceUri))
                    {
                        continue;
                    }

                    // Need to create a new QilName (can't reuse the one from the variable, eventhough it's exactly the same)
                    var paramname = AstFactory.QName(variable.Name.LocalName, variable.Name.NamespaceUri, variable.Name.Prefix);

                    // For each variable in scope, add xsl:with-param to the xsl:call-template
                    var withparam = AstFactory.VarPar(XslNodeType.WithParam, paramname, '$' + paramname.QualifiedName, XslVersion.Current);
                    XsltLoader.SetInfo(withparam, null, fakeCtxInfo);
                    withparam.Namespaces = variable.Namespaces;
                    calltemplate.AddContent(withparam);

                    // For each variable in scope, add xsl:param to the xsl:template
                    var param = AstFactory.VarPar(XslNodeType.Param, paramname, null, XslVersion.Current);
                    XsltLoader.SetInfo(param, null, fakeCtxInfo);
                    param.Namespaces = variable.Namespaces;
                    newtemplate.AddContent(param);
                }
            }

            // Move all the other children to the new template as well (AddRange)
            for (int t = split; t < content.Count; ++t)
            {
                newtemplate.AddContent(content[t]);
            }

            // Replace the child with the rewritten child; remove the rest
            content[split] = calltemplate;
            content.RemoveRange(split + 1, content.Count - split - 1);

            Debug.Assert(parent.Content.Count == split + 1);
        }
    }
}
