// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;

    internal class ActionFrame
    {
        private int _state;         // Action execution state
        private int _counter;       // Counter, for the use of particular action
        private object[] _variables;     // Store for template local variable values
        private Hashtable _withParams;
        private Action _action;        // Action currently being executed
        private ActionFrame _container;     // Frame of enclosing container action and index within it
        private int _currentAction;
        private XPathNodeIterator _nodeSet;       // Current node set
        private XPathNodeIterator _newNodeSet;    // Node set for processing children or other templates

        // Variables to store action data between states:
        private PrefixQName _calulatedName; // Used in ElementAction and AttributeAction
        private string _storedOutput;  // Used in NumberAction, CopyOfAction, ValueOfAction and ProcessingInstructionAction

        internal PrefixQName CalulatedName
        {
            get { return _calulatedName; }
            set { _calulatedName = value; }
        }

        internal string StoredOutput
        {
            get { return _storedOutput; }
            set { _storedOutput = value; }
        }

        internal int State
        {
            get { return _state; }
            set { _state = value; }
        }

        internal int Counter
        {
            get { return _counter; }
            set { _counter = value; }
        }

        internal ActionFrame Container
        {
            get { return _container; }
        }

        internal XPathNavigator Node
        {
            get
            {
                if (_nodeSet != null)
                {
                    return _nodeSet.Current;
                }
                return null;
            }
        }

        internal XPathNodeIterator NodeSet
        {
            get { return _nodeSet; }
        }

        internal XPathNodeIterator NewNodeSet
        {
            get { return _newNodeSet; }
        }

        internal int IncrementCounter()
        {
            return ++_counter;
        }

        internal void AllocateVariables(int count)
        {
            if (0 < count)
            {
                _variables = new object[count];
            }
            else
            {
                _variables = null;
            }
        }

        internal object GetVariable(int index)
        {
            Debug.Assert(_variables != null && index < _variables.Length);
            return _variables[index];
        }

        internal void SetVariable(int index, object value)
        {
            Debug.Assert(_variables != null && index < _variables.Length);
            _variables[index] = value;
        }

        internal void SetParameter(XmlQualifiedName name, object value)
        {
            if (_withParams == null)
            {
                _withParams = new Hashtable();
            }
            Debug.Assert(!_withParams.Contains(name), "We should check duplicate params at compile time");
            _withParams[name] = value;
        }

        internal void ResetParams()
        {
            if (_withParams != null)
                _withParams.Clear();
        }

        internal object GetParameter(XmlQualifiedName name)
        {
            if (_withParams != null)
            {
                return _withParams[name];
            }
            return null;
        }

        internal void InitNodeSet(XPathNodeIterator nodeSet)
        {
            Debug.Assert(nodeSet != null);
            _nodeSet = nodeSet;
        }

        internal void InitNewNodeSet(XPathNodeIterator nodeSet)
        {
            Debug.Assert(nodeSet != null);
            _newNodeSet = nodeSet;
        }

        internal void SortNewNodeSet(Processor proc, ArrayList sortarray)
        {
            Debug.Assert(0 < sortarray.Count);
            int numSorts = sortarray.Count;
            XPathSortComparer comparer = new XPathSortComparer(numSorts);
            for (int i = 0; i < numSorts; i++)
            {
                Sort sort = (Sort)sortarray[i];
                Query expr = proc.GetCompiledQuery(sort.select);

                comparer.AddSort(expr, new XPathComparerHelper(sort.order, sort.caseOrder, sort.lang, sort.dataType));
            }
            List<SortKey> results = new List<SortKey>();

            Debug.Assert(proc.ActionStack.Peek() == this, "the trick we are doing with proc.Current will work only if this is topmost frame");

            while (NewNextNode(proc))
            {
                XPathNodeIterator savedNodeset = _nodeSet;
                _nodeSet = _newNodeSet;              // trick proc.Current node

                SortKey key = new SortKey(numSorts, /*originalPosition:*/results.Count, _newNodeSet.Current.Clone());

                for (int j = 0; j < numSorts; j++)
                {
                    key[j] = comparer.Expression(j).Evaluate(_newNodeSet);
                }
                results.Add(key);

                _nodeSet = savedNodeset;                 // restore proc.Current node
            }
            results.Sort(comparer);
            _newNodeSet = new XPathSortArrayIterator(results);
        }

        // Finished
        internal void Finished()
        {
            State = Action.Finished;
        }

        internal void Inherit(ActionFrame parent)
        {
            Debug.Assert(parent != null);
            _variables = parent._variables;
        }

        private void Init(Action action, ActionFrame container, XPathNodeIterator nodeSet)
        {
            _state = Action.Initialized;
            _action = action;
            _container = container;
            _currentAction = 0;
            _nodeSet = nodeSet;
            _newNodeSet = null;
        }

        internal void Init(Action action, XPathNodeIterator nodeSet)
        {
            Init(action, null, nodeSet);
        }

        internal void Init(ActionFrame containerFrame, XPathNodeIterator nodeSet)
        {
            Init(containerFrame.GetAction(0), containerFrame, nodeSet);
        }

        internal void SetAction(Action action)
        {
            SetAction(action, Action.Initialized);
        }

        internal void SetAction(Action action, int state)
        {
            _action = action;
            _state = state;
        }

        private Action GetAction(int actionIndex)
        {
            Debug.Assert(_action is ContainerAction);
            return ((ContainerAction)_action).GetAction(actionIndex);
        }

        internal void Exit()
        {
            Finished();
            _container = null;
        }

        /*
         * Execute
         *  return values: true - pop, false - nothing
         */
        internal bool Execute(Processor processor)
        {
            if (_action == null)
            {
                return true;
            }

            // Execute the action
            _action.Execute(processor, this);

            // Process results
            if (State == Action.Finished)
            {
                // Advanced to next action
                if (_container != null)
                {
                    _currentAction++;
                    _action = _container.GetAction(_currentAction);
                    State = Action.Initialized;
                }
                else
                {
                    _action = null;
                }
                return _action == null;
            }

            return false;                       // Do not pop, unless specified otherwise
        }

        internal bool NextNode(Processor proc)
        {
            bool next = _nodeSet.MoveNext();
            if (next && proc.Stylesheet.Whitespace)
            {
                XPathNodeType type = _nodeSet.Current.NodeType;
                if (type == XPathNodeType.Whitespace)
                {
                    XPathNavigator nav = _nodeSet.Current.Clone();
                    bool flag;
                    do
                    {
                        nav.MoveTo(_nodeSet.Current);
                        nav.MoveToParent();
                        flag = !proc.Stylesheet.PreserveWhiteSpace(proc, nav) && (next = _nodeSet.MoveNext());
                        type = _nodeSet.Current.NodeType;
                    }
                    while (flag && (type == XPathNodeType.Whitespace));
                }
            }
            return next;
        }

        internal bool NewNextNode(Processor proc)
        {
            bool next = _newNodeSet.MoveNext();
            if (next && proc.Stylesheet.Whitespace)
            {
                XPathNodeType type = _newNodeSet.Current.NodeType;
                if (type == XPathNodeType.Whitespace)
                {
                    XPathNavigator nav = _newNodeSet.Current.Clone();
                    bool flag;
                    do
                    {
                        nav.MoveTo(_newNodeSet.Current);
                        nav.MoveToParent();
                        flag = !proc.Stylesheet.PreserveWhiteSpace(proc, nav) && (next = _newNodeSet.MoveNext());
                        type = _newNodeSet.Current.NodeType;
                    }
                    while (flag && (type == XPathNodeType.Whitespace));
                }
            }
            return next;
        }

        // special array iterator that iterates over ArrayList of SortKey
        private class XPathSortArrayIterator : XPathArrayIterator
        {
            public XPathSortArrayIterator(List<SortKey> list) : base(list) { }
            public XPathSortArrayIterator(XPathSortArrayIterator it) : base(it) { }

            public override XPathNodeIterator Clone()
            {
                return new XPathSortArrayIterator(this);
            }

            public override XPathNavigator Current
            {
                get
                {
                    Debug.Assert(index > 0, "MoveNext() wasn't called");
                    return ((SortKey)this.list[this.index - 1]).Node;
                }
            }
        }
    }
}
