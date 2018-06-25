// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml.XPath;
    using System.Xml.Xsl.XsltOld.Debugger;
    using MS.Internal.Xml.XPath;

    internal sealed class Processor : IXsltProcessor
    {
        //
        // Static constants
        //

        private const int StackIncrement = 10;

        //
        // Execution result
        //

        internal enum ExecResult
        {
            Continue,           // Continues next iteration immediately
            Interrupt,          // Returns to caller, was processed enough
            Done                // Execution finished
        }

        internal enum OutputResult
        {
            Continue,
            Interrupt,
            Overflow,
            Error,
            Ignore
        }

        private ExecResult _execResult;

        //
        // Compiled stylesheet
        //

        private Stylesheet _stylesheet;     // Root of import tree of template managers
        private RootAction _rootAction;
        private Key[] _keyList;
        private List<TheQuery> _queryStore;

        //
        // Document Being transformed
        //

        private XPathNavigator _document;

        //
        // Execution action stack
        //

        private HWStack _actionStack;
        private HWStack _debuggerStack;

        //
        // Register for returning value from calling nested action
        //

        private StringBuilder _sharedStringBuilder;

        //
        // Output related member variables
        //
        private int _ignoreLevel;
        private StateMachine _xsm;
        private RecordBuilder _builder;

        private XsltOutput _output;

        private XmlNameTable _nameTable = new NameTable();

        private XmlResolver _resolver;

#pragma warning disable 618
        private XsltArgumentList _args;
#pragma warning restore 618
        private Hashtable _scriptExtensions;

        private ArrayList _numberList;
        //
        // Template lookup action
        //

        private TemplateLookupAction _templateLookup = new TemplateLookupAction();

        private IXsltDebugger _debugger;
        private Query[] _queryList;

        private ArrayList _sortArray;

        private Hashtable _documentCache;

        // NOTE: ValueOf() can call Matches() through XsltCompileContext.PreserveWhitespace(),
        // that's why we use two different contexts here, valueOfContext and matchesContext
        private XsltCompileContext _valueOfContext;
        private XsltCompileContext _matchesContext;

        internal XPathNavigator Current
        {
            get
            {
                ActionFrame frame = (ActionFrame)_actionStack.Peek();
                return frame != null ? frame.Node : null;
            }
        }

        internal ExecResult ExecutionResult
        {
            get { return _execResult; }

            set
            {
                Debug.Assert(_execResult == ExecResult.Continue);
                _execResult = value;
            }
        }

        internal Stylesheet Stylesheet
        {
            get { return _stylesheet; }
        }

        internal XmlResolver Resolver
        {
            get
            {
                Debug.Assert(_resolver != null, "Constructor should create it if null passed");
                return _resolver;
            }
        }

        internal ArrayList SortArray
        {
            get
            {
                Debug.Assert(_sortArray != null, "InitSortArray() wasn't called");
                return _sortArray;
            }
        }

        internal Key[] KeyList
        {
            get { return _keyList; }
        }

        internal XPathNavigator GetNavigator(Uri ruri)
        {
            XPathNavigator result = null;
            if (_documentCache != null)
            {
                result = _documentCache[ruri] as XPathNavigator;
                if (result != null)
                {
                    return result.Clone();
                }
            }
            else
            {
                _documentCache = new Hashtable();
            }

            object input = _resolver.GetEntity(ruri, null, null);
            if (input is Stream)
            {
                XmlTextReaderImpl tr = new XmlTextReaderImpl(ruri.ToString(), (Stream)input);
                {
                    tr.XmlResolver = _resolver;
                }
                // reader is closed by Compiler.LoadDocument()
                result = ((IXPathNavigable)Compiler.LoadDocument(tr)).CreateNavigator();
            }
            else if (input is XPathNavigator)
            {
                result = (XPathNavigator)input;
            }
            else
            {
                throw XsltException.Create(SR.Xslt_CantResolve, ruri.ToString());
            }
            _documentCache[ruri] = result.Clone();
            return result;
        }

        internal void AddSort(Sort sortinfo)
        {
            Debug.Assert(_sortArray != null, "InitSortArray() wasn't called");
            _sortArray.Add(sortinfo);
        }

        internal void InitSortArray()
        {
            if (_sortArray == null)
            {
                _sortArray = new ArrayList();
            }
            else
            {
                _sortArray.Clear();
            }
        }

        internal object GetGlobalParameter(XmlQualifiedName qname)
        {
            object parameter = _args.GetParam(qname.Name, qname.Namespace);
            if (parameter == null)
            {
                return null;
            }
            if (
                parameter is XPathNodeIterator ||
                parameter is XPathNavigator ||
                parameter is bool ||
                parameter is double ||
                parameter is string
            )
            {
                // doing nothing
            }
            else if (
              parameter is short || parameter is ushort ||
              parameter is int || parameter is uint ||
              parameter is long || parameter is ulong ||
              parameter is float || parameter is decimal
          )
            {
                parameter = XmlConvert.ToXPathDouble(parameter);
            }
            else
            {
                parameter = parameter.ToString();
            }
            return parameter;
        }

        internal object GetExtensionObject(string nsUri)
        {
            return _args.GetExtensionObject(nsUri);
        }

        internal object GetScriptObject(string nsUri)
        {
            return _scriptExtensions[nsUri];
        }

        internal RootAction RootAction
        {
            get { return _rootAction; }
        }

        internal XPathNavigator Document
        {
            get { return _document; }
        }

#if DEBUG
        private bool _stringBuilderLocked = false;
#endif

        internal StringBuilder GetSharedStringBuilder()
        {
#if DEBUG
            Debug.Assert(!_stringBuilderLocked);
#endif
            if (_sharedStringBuilder == null)
            {
                _sharedStringBuilder = new StringBuilder();
            }
            else
            {
                _sharedStringBuilder.Length = 0;
            }
#if DEBUG
            _stringBuilderLocked = true;
#endif
            return _sharedStringBuilder;
        }

        internal void ReleaseSharedStringBuilder()
        {
            // don't clean stringBuilderLocked here. ToString() will happen after this call
#if DEBUG
            _stringBuilderLocked = false;
#endif
        }

        internal ArrayList NumberList
        {
            get
            {
                if (_numberList == null)
                {
                    _numberList = new ArrayList();
                }
                return _numberList;
            }
        }

        internal IXsltDebugger Debugger
        {
            get { return _debugger; }
        }

        internal HWStack ActionStack
        {
            get { return _actionStack; }
        }

        internal XsltOutput Output
        {
            get { return _output; }
        }

        //
        // Construction
        //
        public Processor(
            XPathNavigator doc, XsltArgumentList args, XmlResolver resolver,
            Stylesheet stylesheet, List<TheQuery> queryStore, RootAction rootAction,
            IXsltDebugger debugger
        )
        {
            _stylesheet = stylesheet;
            _queryStore = queryStore;
            _rootAction = rootAction;
            _queryList = new Query[queryStore.Count];
            {
                for (int i = 0; i < queryStore.Count; i++)
                {
                    _queryList[i] = Query.Clone(queryStore[i].CompiledQuery.QueryTree);
                }
            }

            _xsm = new StateMachine();
            _document = doc;
            _builder = null;
            _actionStack = new HWStack(StackIncrement);
            _output = _rootAction.Output;
            _resolver = resolver ?? XmlNullResolver.Singleton;
            _args = args ?? new XsltArgumentList();
            _debugger = debugger;
            if (_debugger != null)
            {
                _debuggerStack = new HWStack(StackIncrement, /*limit:*/1000);
                _templateLookup = new TemplateLookupActionDbg();
            }

            // Clone the compile-time KeyList
            if (_rootAction.KeyList != null)
            {
                _keyList = new Key[_rootAction.KeyList.Count];
                for (int i = 0; i < _keyList.Length; i++)
                {
                    _keyList[i] = _rootAction.KeyList[i].Clone();
                }
            }

            _scriptExtensions = new Hashtable(_stylesheet.ScriptObjectTypes.Count);
            {
                foreach (DictionaryEntry entry in _stylesheet.ScriptObjectTypes)
                {
                    string namespaceUri = (string)entry.Key;
                    if (GetExtensionObject(namespaceUri) != null)
                    {
                        throw XsltException.Create(SR.Xslt_ScriptDub, namespaceUri);
                    }
                    _scriptExtensions.Add(namespaceUri, Activator.CreateInstance((Type)entry.Value,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, null));
                }
            }

            this.PushActionFrame(_rootAction, /*nodeSet:*/null);
        }

        public ReaderOutput StartReader()
        {
            ReaderOutput output = new ReaderOutput(this);
            _builder = new RecordBuilder(output, _nameTable);
            return output;
        }

        public void Execute(Stream stream)
        {
            RecordOutput recOutput = null;

            switch (_output.Method)
            {
                case XsltOutput.OutputMethod.Text:
                    recOutput = new TextOnlyOutput(this, stream);
                    break;
                case XsltOutput.OutputMethod.Xml:
                case XsltOutput.OutputMethod.Html:
                case XsltOutput.OutputMethod.Other:
                case XsltOutput.OutputMethod.Unknown:
                    recOutput = new TextOutput(this, stream);
                    break;
            }
            _builder = new RecordBuilder(recOutput, _nameTable);
            Execute();
        }

        public void Execute(TextWriter writer)
        {
            RecordOutput recOutput = null;

            switch (_output.Method)
            {
                case XsltOutput.OutputMethod.Text:
                    recOutput = new TextOnlyOutput(this, writer);
                    break;
                case XsltOutput.OutputMethod.Xml:
                case XsltOutput.OutputMethod.Html:
                case XsltOutput.OutputMethod.Other:
                case XsltOutput.OutputMethod.Unknown:
                    recOutput = new TextOutput(this, writer);
                    break;
            }
            _builder = new RecordBuilder(recOutput, _nameTable);
            Execute();
        }

        public void Execute(XmlWriter writer)
        {
            _builder = new RecordBuilder(new WriterOutput(this, writer), _nameTable);
            Execute();
        }

        //
        //  Execution part of processor
        //
        internal void Execute()
        {
            Debug.Assert(_actionStack != null);

            while (_execResult == ExecResult.Continue)
            {
                ActionFrame frame = (ActionFrame)_actionStack.Peek();

                if (frame == null)
                {
                    Debug.Assert(_builder != null);
                    _builder.TheEnd();
                    ExecutionResult = ExecResult.Done;
                    break;
                }

                // Execute the action which was on the top of the stack
                if (frame.Execute(this))
                {
                    _actionStack.Pop();
                }
            }

            if (_execResult == ExecResult.Interrupt)
            {
                _execResult = ExecResult.Continue;
            }
        }

        //
        // Action frame support
        //

        internal ActionFrame PushNewFrame()
        {
            ActionFrame prent = (ActionFrame)_actionStack.Peek();
            ActionFrame frame = (ActionFrame)_actionStack.Push();
            if (frame == null)
            {
                frame = new ActionFrame();
                _actionStack.AddToTop(frame);
            }
            Debug.Assert(frame != null);

            if (prent != null)
            {
                frame.Inherit(prent);
            }

            return frame;
        }

        internal void PushActionFrame(Action action, XPathNodeIterator nodeSet)
        {
            ActionFrame frame = PushNewFrame();
            frame.Init(action, nodeSet);
        }

        internal void PushActionFrame(ActionFrame container)
        {
            this.PushActionFrame(container, container.NodeSet);
        }

        internal void PushActionFrame(ActionFrame container, XPathNodeIterator nodeSet)
        {
            ActionFrame frame = PushNewFrame();
            frame.Init(container, nodeSet);
        }

        internal void PushTemplateLookup(XPathNodeIterator nodeSet, XmlQualifiedName mode, Stylesheet importsOf)
        {
            Debug.Assert(_templateLookup != null);
            _templateLookup.Initialize(mode, importsOf);
            PushActionFrame(_templateLookup, nodeSet);
        }

        internal string GetQueryExpression(int key)
        {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            return _queryStore[key].CompiledQuery.Expression;
        }

        internal Query GetCompiledQuery(int key)
        {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            TheQuery theQuery = _queryStore[key];
            theQuery.CompiledQuery.CheckErrors();
            Query expr = Query.Clone(_queryList[key]);
            expr.SetXsltContext(new XsltCompileContext(theQuery._ScopeManager, this));
            return expr;
        }

        internal Query GetValueQuery(int key)
        {
            return GetValueQuery(key, null);
        }

        internal Query GetValueQuery(int key, XsltCompileContext context)
        {
            Debug.Assert(key != Compiler.InvalidQueryKey);
            TheQuery theQuery = _queryStore[key];
            theQuery.CompiledQuery.CheckErrors();
            Query expr = _queryList[key];

            if (context == null)
            {
                context = new XsltCompileContext(theQuery._ScopeManager, this);
            }
            else
            {
                context.Reinitialize(theQuery._ScopeManager, this);
            }

            expr.SetXsltContext(context);
            return expr;
        }

        private XsltCompileContext GetValueOfContext()
        {
            if (_valueOfContext == null)
            {
                _valueOfContext = new XsltCompileContext();
            }
            return _valueOfContext;
        }

        [Conditional("DEBUG")]
        private void RecycleValueOfContext()
        {
            if (_valueOfContext != null)
            {
                _valueOfContext.Recycle();
            }
        }

        private XsltCompileContext GetMatchesContext()
        {
            if (_matchesContext == null)
            {
                _matchesContext = new XsltCompileContext();
            }
            return _matchesContext;
        }

        [Conditional("DEBUG")]
        private void RecycleMatchesContext()
        {
            if (_matchesContext != null)
            {
                _matchesContext.Recycle();
            }
        }

        internal string ValueOf(ActionFrame context, int key)
        {
            string result;

            Query query = this.GetValueQuery(key, GetValueOfContext());
            object value = query.Evaluate(context.NodeSet);
            if (value is XPathNodeIterator)
            {
                XPathNavigator n = query.Advance();
                result = n != null ? ValueOf(n) : string.Empty;
            }
            else
            {
                result = XmlConvert.ToXPathString(value);
            }

            RecycleValueOfContext();
            return result;
        }

        internal string ValueOf(XPathNavigator n)
        {
            if (_stylesheet.Whitespace && n.NodeType == XPathNodeType.Element)
            {
                StringBuilder builder = this.GetSharedStringBuilder();
                ElementValueWithoutWS(n, builder);
                this.ReleaseSharedStringBuilder();
                return builder.ToString();
            }
            return n.Value;
        }

        private void ElementValueWithoutWS(XPathNavigator nav, StringBuilder builder)
        {
            Debug.Assert(nav.NodeType == XPathNodeType.Element);
            bool preserve = this.Stylesheet.PreserveWhiteSpace(this, nav);
            if (nav.MoveToFirstChild())
            {
                do
                {
                    switch (nav.NodeType)
                    {
                        case XPathNodeType.Text:
                        case XPathNodeType.SignificantWhitespace:
                            builder.Append(nav.Value);
                            break;
                        case XPathNodeType.Whitespace:
                            if (preserve)
                            {
                                builder.Append(nav.Value);
                            }
                            break;
                        case XPathNodeType.Element:
                            ElementValueWithoutWS(nav, builder);
                            break;
                    }
                } while (nav.MoveToNext());
                nav.MoveToParent();
            }
        }

        internal XPathNodeIterator StartQuery(XPathNodeIterator context, int key)
        {
            Query query = GetCompiledQuery(key);
            object result = query.Evaluate(context);
            if (result is XPathNodeIterator)
            {
                return new XPathSelectionIterator(context.Current, query);
            }
            throw XsltException.Create(SR.XPath_NodeSetExpected);
        }

        internal object Evaluate(ActionFrame context, int key)
        {
            return GetValueQuery(key).Evaluate(context.NodeSet);
        }

        internal object RunQuery(ActionFrame context, int key)
        {
            Query query = GetCompiledQuery(key);
            object value = query.Evaluate(context.NodeSet);
            XPathNodeIterator it = value as XPathNodeIterator;
            if (it != null)
            {
                return new XPathArrayIterator(it);
            }

            return value;
        }

        internal string EvaluateString(ActionFrame context, int key)
        {
            object objValue = Evaluate(context, key);
            string value = null;
            if (objValue != null)
                value = XmlConvert.ToXPathString(objValue);
            if (value == null)
                value = string.Empty;
            return value;
        }

        internal bool EvaluateBoolean(ActionFrame context, int key)
        {
            object objValue = Evaluate(context, key);

            if (objValue != null)
            {
                XPathNavigator nav = objValue as XPathNavigator;
                return nav != null ? Convert.ToBoolean(nav.Value, CultureInfo.InvariantCulture) : Convert.ToBoolean(objValue, CultureInfo.InvariantCulture);
            }
            else
            {
                return false;
            }
        }

        internal bool Matches(XPathNavigator context, int key)
        {
            // We don't use XPathNavigator.Matches() to avoid cloning of Query on each call
            Query query = this.GetValueQuery(key, GetMatchesContext());

            try
            {
                bool result = query.MatchNode(context) != null;

                RecycleMatchesContext();
                return result;
            }
            catch (XPathException)
            {
                throw XsltException.Create(SR.Xslt_InvalidPattern, this.GetQueryExpression(key));
            }
        }

        //
        // Outputting part of processor
        //

        internal XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        internal bool CanContinue
        {
            get { return _execResult == ExecResult.Continue; }
        }

        internal bool ExecutionDone
        {
            get { return _execResult == ExecResult.Done; }
        }

        internal void ResetOutput()
        {
            Debug.Assert(_builder != null);
            _builder.Reset();
        }
        internal bool BeginEvent(XPathNodeType nodeType, string prefix, string name, string nspace, bool empty)
        {
            return BeginEvent(nodeType, prefix, name, nspace, empty, null, true);
        }

        internal bool BeginEvent(XPathNodeType nodeType, string prefix, string name, string nspace, bool empty, object htmlProps, bool search)
        {
            Debug.Assert(_xsm != null);

            int stateOutlook = _xsm.BeginOutlook(nodeType);

            if (_ignoreLevel > 0 || stateOutlook == StateMachine.Error)
            {
                _ignoreLevel++;
                return true;                        // We consumed the event, so pretend it was output.
            }

            switch (_builder.BeginEvent(stateOutlook, nodeType, prefix, name, nspace, empty, htmlProps, search))
            {
                case OutputResult.Continue:
                    _xsm.Begin(nodeType);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State);
                    Debug.Assert(ExecutionResult == ExecResult.Continue);
                    return true;
                case OutputResult.Interrupt:
                    _xsm.Begin(nodeType);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State);
                    ExecutionResult = ExecResult.Interrupt;
                    return true;
                case OutputResult.Overflow:
                    ExecutionResult = ExecResult.Interrupt;
                    return false;
                case OutputResult.Error:
                    _ignoreLevel++;
                    return true;
                case OutputResult.Ignore:
                    return true;
                default:
                    Debug.Fail("Unexpected result of RecordBuilder.BeginEvent()");
                    return true;
            }
        }

        internal bool TextEvent(string text)
        {
            return this.TextEvent(text, false);
        }

        internal bool TextEvent(string text, bool disableOutputEscaping)
        {
            Debug.Assert(_xsm != null);

            if (_ignoreLevel > 0)
            {
                return true;
            }

            int stateOutlook = _xsm.BeginOutlook(XPathNodeType.Text);

            switch (_builder.TextEvent(stateOutlook, text, disableOutputEscaping))
            {
                case OutputResult.Continue:
                    _xsm.Begin(XPathNodeType.Text);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State);
                    Debug.Assert(ExecutionResult == ExecResult.Continue);
                    return true;
                case OutputResult.Interrupt:
                    _xsm.Begin(XPathNodeType.Text);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State);
                    ExecutionResult = ExecResult.Interrupt;
                    return true;
                case OutputResult.Overflow:
                    ExecutionResult = ExecResult.Interrupt;
                    return false;
                case OutputResult.Error:
                case OutputResult.Ignore:
                    return true;
                default:
                    Debug.Fail("Unexpected result of RecordBuilder.TextEvent()");
                    return true;
            }
        }

        internal bool EndEvent(XPathNodeType nodeType)
        {
            Debug.Assert(_xsm != null);

            if (_ignoreLevel > 0)
            {
                _ignoreLevel--;
                return true;
            }

            int stateOutlook = _xsm.EndOutlook(nodeType);

            switch (_builder.EndEvent(stateOutlook, nodeType))
            {
                case OutputResult.Continue:
                    _xsm.End(nodeType);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State);
                    return true;
                case OutputResult.Interrupt:
                    _xsm.End(nodeType);
                    Debug.Assert(StateMachine.StateOnly(stateOutlook) == _xsm.State,
                                 "StateMachine.StateOnly(stateOutlook) == this.xsm.State");
                    ExecutionResult = ExecResult.Interrupt;
                    return true;
                case OutputResult.Overflow:
                    ExecutionResult = ExecResult.Interrupt;
                    return false;
                case OutputResult.Error:
                case OutputResult.Ignore:
                default:
                    Debug.Fail("Unexpected result of RecordBuilder.TextEvent()");
                    return true;
            }
        }

        internal bool CopyBeginEvent(XPathNavigator node, bool emptyflag)
        {
            switch (node.NodeType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Attribute:
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                    return BeginEvent(node.NodeType, node.Prefix, node.LocalName, node.NamespaceURI, emptyflag);
                case XPathNodeType.Namespace:
                    // value instead of namespace here!
                    return BeginEvent(XPathNodeType.Namespace, null, node.LocalName, node.Value, false);
                case XPathNodeType.Text:
                    // Text will be copied in CopyContents();
                    break;

                case XPathNodeType.Root:
                case XPathNodeType.Whitespace:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.All:
                    break;

                default:
                    Debug.Fail("Invalid XPathNodeType in CopyBeginEvent");
                    break;
            }

            return true;
        }

        internal bool CopyTextEvent(XPathNavigator node)
        {
            switch (node.NodeType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Namespace:
                    break;

                case XPathNodeType.Attribute:
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                case XPathNodeType.Text:
                case XPathNodeType.Whitespace:
                case XPathNodeType.SignificantWhitespace:
                    string text = node.Value;
                    return TextEvent(text);

                case XPathNodeType.Root:
                case XPathNodeType.All:
                    break;

                default:
                    Debug.Fail("Invalid XPathNodeType in CopyTextEvent");
                    break;
            }

            return true;
        }

        internal bool CopyEndEvent(XPathNavigator node)
        {
            switch (node.NodeType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Attribute:
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                case XPathNodeType.Namespace:
                    return EndEvent(node.NodeType);

                case XPathNodeType.Text:
                    // Text was copied in CopyContents();
                    break;


                case XPathNodeType.Root:
                case XPathNodeType.Whitespace:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.All:
                    break;

                default:
                    Debug.Fail("Invalid XPathNodeType in CopyEndEvent");
                    break;
            }

            return true;
        }

        internal static bool IsRoot(XPathNavigator navigator)
        {
            Debug.Assert(navigator != null);

            if (navigator.NodeType == XPathNodeType.Root)
            {
                return true;
            }
            else if (navigator.NodeType == XPathNodeType.Element)
            {
                XPathNavigator clone = navigator.Clone();
                clone.MoveToRoot();
                return clone.IsSamePosition(navigator);
            }
            else
            {
                return false;
            }
        }

        //
        // Builder stack
        //
        internal void PushOutput(RecordOutput output)
        {
            Debug.Assert(output != null);
            _builder.OutputState = _xsm.State;
            RecordBuilder lastBuilder = _builder;
            _builder = new RecordBuilder(output, _nameTable);
            _builder.Next = lastBuilder;

            _xsm.Reset();
        }

        internal RecordOutput PopOutput()
        {
            Debug.Assert(_builder != null);

            RecordBuilder topBuilder = _builder;
            _builder = topBuilder.Next;
            _xsm.State = _builder.OutputState;

            topBuilder.TheEnd();

            return topBuilder.Output;
        }

        internal bool SetDefaultOutput(XsltOutput.OutputMethod method)
        {
            if (Output.Method != method)
            {
                _output = _output.CreateDerivedOutput(method);
                return true;
            }
            return false;
        }

        internal object GetVariableValue(VariableAction variable)
        {
            int variablekey = variable.VarKey;
            if (variable.IsGlobal)
            {
                ActionFrame rootFrame = (ActionFrame)_actionStack[0];
                object result = rootFrame.GetVariable(variablekey);
                if (result == VariableAction.BeingComputedMark)
                {
                    throw XsltException.Create(SR.Xslt_CircularReference, variable.NameStr);
                }
                if (result != null)
                {
                    return result;
                }
                // Variable wasn't evaluated yet
                int saveStackSize = _actionStack.Length;
                ActionFrame varFrame = PushNewFrame();
                varFrame.Inherit(rootFrame);
                varFrame.Init(variable, rootFrame.NodeSet);
                do
                {
                    bool endOfFrame = ((ActionFrame)_actionStack.Peek()).Execute(this);
                    if (endOfFrame)
                    {
                        _actionStack.Pop();
                    }
                } while (saveStackSize < _actionStack.Length);
                Debug.Assert(saveStackSize == _actionStack.Length);
                result = rootFrame.GetVariable(variablekey);
                Debug.Assert(result != null, "Variable was just calculated and result can't be null");
                return result;
            }
            else
            {
                return ((ActionFrame)_actionStack.Peek()).GetVariable(variablekey);
            }
        }

        internal void SetParameter(XmlQualifiedName name, object value)
        {
            Debug.Assert(1 < _actionStack.Length);
            ActionFrame parentFrame = (ActionFrame)_actionStack[_actionStack.Length - 2];
            parentFrame.SetParameter(name, value);
        }

        internal void ResetParams()
        {
            ActionFrame frame = (ActionFrame)_actionStack[_actionStack.Length - 1];
            frame.ResetParams();
        }

        internal object GetParameter(XmlQualifiedName name)
        {
            Debug.Assert(2 < _actionStack.Length);
            ActionFrame parentFrame = (ActionFrame)_actionStack[_actionStack.Length - 3];
            return parentFrame.GetParameter(name);
        }

        // ---------------------- Debugger stack -----------------------

        internal class DebuggerFrame
        {
            internal ActionFrame actionFrame;
            internal XmlQualifiedName currentMode;
        }

        internal void PushDebuggerStack()
        {
            Debug.Assert(this.Debugger != null, "We don't generate calls this function if ! debugger");
            DebuggerFrame dbgFrame = (DebuggerFrame)_debuggerStack.Push();
            if (dbgFrame == null)
            {
                dbgFrame = new DebuggerFrame();
                _debuggerStack.AddToTop(dbgFrame);
            }
            dbgFrame.actionFrame = (ActionFrame)_actionStack.Peek(); // In a case of next builtIn action.
        }

        internal void PopDebuggerStack()
        {
            Debug.Assert(this.Debugger != null, "We don't generate calls this function if ! debugger");
            _debuggerStack.Pop();
        }

        internal void OnInstructionExecute()
        {
            Debug.Assert(this.Debugger != null, "We don't generate calls this function if ! debugger");
            DebuggerFrame dbgFrame = (DebuggerFrame)_debuggerStack.Peek();
            Debug.Assert(dbgFrame != null, "PushDebuggerStack() wasn't ever called");
            dbgFrame.actionFrame = (ActionFrame)_actionStack.Peek();
            this.Debugger.OnInstructionExecute((IXsltProcessor)this);
        }

        internal XmlQualifiedName GetPrevioseMode()
        {
            Debug.Assert(this.Debugger != null, "We don't generate calls this function if ! debugger");
            Debug.Assert(2 <= _debuggerStack.Length);
            return ((DebuggerFrame)_debuggerStack[_debuggerStack.Length - 2]).currentMode;
        }

        internal void SetCurrentMode(XmlQualifiedName mode)
        {
            Debug.Assert(this.Debugger != null, "We don't generate calls this function if ! debugger");
            ((DebuggerFrame)_debuggerStack[_debuggerStack.Length - 1]).currentMode = mode;
        }
    }
}
