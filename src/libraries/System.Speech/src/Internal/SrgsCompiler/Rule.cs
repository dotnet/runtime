// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.SrgsCompiler
{
    [DebuggerDisplay("{Name}")]
    internal sealed class Rule : ParseElementCollection, IRule, IComparable<Rule>
    {
        #region Constructors

        // Only used for the special transition
        internal Rule(int iSerialize)
            : base(null, null)
        {
            _iSerialize = iSerialize;
        }

        internal Rule(Backend backend, string name, CfgRule cfgRule, int iSerialize, GrammarOptions SemanticFormat, ref int cImportedRules)
            : base(backend, null)
        {
            _rule = this;
            Init(name, cfgRule, iSerialize, SemanticFormat, ref cImportedRules);
        }

        internal Rule(Backend backend, string name, int offsetName, SPCFGRULEATTRIBUTES attributes, int id, int iSerialize, GrammarOptions SemanticFormat, ref int cImportedRules)
            : base(backend, null)
        {
            _rule = this;
            Init(name, new CfgRule(id, offsetName, attributes), iSerialize, SemanticFormat, ref cImportedRules);
        }

        #endregion

        #region internal Methods

        #region IComparable<Rule> Interface implementation

        int IComparable<Rule>.CompareTo(Rule rule2)
        {
            Rule rule1 = this;

            if (rule1._cfgRule.Import)
            {
                return (rule2._cfgRule.Import) ? rule1._cfgRule._nameOffset - rule2._cfgRule._nameOffset : -1;
            }
            else if (rule1._cfgRule.Dynamic)
            {
                return (rule2._cfgRule.Dynamic) ? rule1._cfgRule._nameOffset - rule2._cfgRule._nameOffset : 1;
            }
            else
            {
                return (rule2._cfgRule.Import) ? 1 : (rule2._cfgRule.Dynamic) ? -1 : rule1._cfgRule._nameOffset - rule2._cfgRule._nameOffset;
            }
        }

        #endregion

#if DEBUG

        internal void CheckForExitPath(ref int iRecursiveDepth)
        {
            if (!_fHasExitPath)
            {
                // This check allows empty rules.
                if (_firstState != null && _firstState.NumArcs != 0)
                {
                    _firstState.CheckExitPath(ref iRecursiveDepth);
                }
            }
        }
#endif

        internal void Validate()
        {
            if ((!_cfgRule.Dynamic) && (!_cfgRule.Import) && _id != "VOID" && _firstState.NumArcs == 0)
            {
                XmlParser.ThrowSrgsException(SRID.EmptyRule);
            }
            else
            {
                _fHasDynamicRef = _cfgRule.Dynamic;
            }
        }

        internal void PopulateDynamicRef(ref int iRecursiveDepth)
        {
            if (iRecursiveDepth > CfgGrammar.MAX_TRANSITIONS_COUNT)
            {
                XmlParser.ThrowSrgsException((SRID.MaxTransitionsCount));
            }

            foreach (Rule rule in _listRules)
            {
                if (!rule._fHasDynamicRef)
                {
                    rule._fHasDynamicRef = true;
                    rule.PopulateDynamicRef(ref iRecursiveDepth);
                }
            }
        }

        internal Rule Clone(StringBlob symbol, string ruleName)
        {
            Rule rule = new(_iSerialize);

            int idWord;
            int offsetName = symbol.Add(ruleName, out idWord);

            rule._id = ruleName;
            rule._cfgRule = new CfgRule(idWord, offsetName, _cfgRule._flag)
            {
                DirtyRule = true,
                FirstArcIndex = 0
            };
            return rule;
        }

        internal void Serialize(StreamMarshaler streamBuffer)
        {

            // Dynamic rules and imports have no arcs
            _cfgRule.FirstArcIndex = _firstState != null && !_firstState.OutArcs.IsEmpty ? (uint)_firstState.SerializeId : 0;

            _cfgRule.DirtyRule = true;

            streamBuffer.WriteStream(_cfgRule);
        }

        void IElement.PostParse(IElement grammar)
        {
            // Empty rule
            if (_endArc == null)
            {
                System.Diagnostics.Debug.Assert(_startArc == null);
                _firstState = _backend.CreateNewState(this);
            }
            else
            {
                // The last arc may contain an epsilon value. Remove it.
                TrimEndEpsilons(_endArc, _backend);

                // If the first arc was an epsilon value then there is no need to create a new state
                if (_startArc.IsEpsilonTransition && _startArc.End != null && Graph.MoveSemanticTagRight(_startArc))
                {
                    // Discard the arc and replace it with the startArc
                    _firstState = _startArc.End;
                    System.Diagnostics.Debug.Assert(_startArc.End == _startArc.End);
                    _startArc.End = null;
                }
                else
                {
                    // if _first has not be set, create it
                    _firstState = _backend.CreateNewState(this);

                    // Attach the start and end arc to the rule
                    _startArc.Start = _firstState;
                }
            }
        }

        void IRule.CreateScript(IGrammar grammar, string rule, string method, RuleMethodScript type)
        {
            ((GrammarElement)grammar).CustomGrammar._scriptRefs.Add(new ScriptRef(rule, method, type));
        }

        #endregion

        #region Internal Properties

        internal string Name
        {
            get
            {
                return _id;
            }
        }

        string IRule.BaseClass
        {
            get
            {
                return _baseclass;
            }
            set
            {
                _baseclass = value;
            }
        }

        internal string BaseClass
        {
            get
            {
                return _baseclass;
            }
        }

        internal StringBuilder Script
        {
            get
            {
                return _script;
            }
        }

        internal StringBuilder Constructors
        {
            get
            {
                return _constructors;
            }
        }

        #endregion

        #region Private Methods

        private void Init(string id, CfgRule cfgRule, int iSerialize, GrammarOptions SemanticFormat, ref int cImportedRules)
        {
            _id = id;
            _cfgRule = cfgRule;
            _firstState = null;
            _cfgRule.DirtyRule = true;
            _iSerialize = iSerialize;
            _fHasExitPath = false;
            _fHasDynamicRef = false;
            _fIsEpsilonRule = false;
            _fStaticRule = false;
            if (_cfgRule.Import)
            {
                cImportedRules++;
            }
        }

        private static void TrimEndEpsilons(Arc end, Backend backend)
        {
            Arc endArc = end;

            State endState = endArc.Start;
            if (endState != null)
            {
                // Remove the end arc if possible, check done by MoveSemanticTagRight
                if (endArc.IsEpsilonTransition && endState.OutArcs.CountIsOne && Graph.MoveSemanticTagLeft(endArc))
                {
                    // State has a single input epsilon transition
                    // Delete the input epsilon transition and delete state.
                    endArc.Start = null;

                    // Remove all the in arcs duplicate the arcs first
                    foreach (Arc inArc in endState.InArcs.ToList())
                    {
                        inArc.End = null;
                        TrimEndEpsilons(inArc, backend);
                    }

                    // Delete the input epsilon transition and delete state if appropriate.
                    backend.DeleteState(endState);
                }
            }
        }

        #endregion

        #region Internal Fields

        internal CfgRule _cfgRule;

        internal State _firstState;

        internal bool _fHasExitPath;

        internal bool _fHasDynamicRef;

        internal bool _fIsEpsilonRule;

        internal int _iSerialize;
        internal int _iSerialize2;

#if DEBUG
        internal int _cStates;
#endif
        internal List<Rule> _listRules = new();

        // this is used to refer to a static rule from a dynamic rule
        internal bool _fStaticRule;

        #endregion

        #region Private Fields

        private string _id;

        // STG fields
        private string _baseclass;

        private StringBuilder _script = new();

        private StringBuilder _constructors = new();

        #endregion
    }
}
