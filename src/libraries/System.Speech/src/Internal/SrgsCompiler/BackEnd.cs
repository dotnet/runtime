// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.SrgsCompiler
{
    internal sealed partial class Backend
    {
        #region Constructors

        internal Backend()
        {
            _words = new StringBlob();
            _symbols = new StringBlob();
        }

        internal Backend(StreamMarshaler streamHelper)
        {
            InitFromBinaryGrammar(streamHelper);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Optimizes the grammar network by removing the epsilon states and merging
        /// duplicate transitions.
        /// </summary>
        internal void Optimize()
        {
            _states.Optimize();

            // Most likely, there will be an arc with a weight != 1.  So we need a weight table.
            _fNeedWeightTable = true;
        }

        /// <summary>
        /// Performs consistency checks of the grammar structure, creates the
        /// serialized format and either saves it to the stream provided by SetSaveOptions,
        /// or reloads it into the CFG engine.
        /// </summary>
        internal void Commit(StreamMarshaler streamBuffer)
        {
            // For debugging purpose, assert if the position is not it is assumed it should be
            // Keep the start position in the stream
            long startStreamPosition = streamBuffer.Stream.Position;

            // put all states State into a sorted array by rule parent index and serialized index
            List<State> sortedStates = new(_states);

            // Release the memory for the original list of states
            _states = null;

            sortedStates.Sort();

            // Validate the grammar
            ValidateAndTagRules();
            CheckLeftRecursion(sortedStates);

            // Include null terminator
            int cBasePath = _basePath != null ? _basePath.Length + 1 : 0;
            float[] pWeights;
            int cArcs;

            // Add the top level semantic interpretation tag
            // This should be set as the first symbol in the symbol string blog since it must hold on a 16 bits value.
            int semanticInterpretationGlobals = 0;
            if (_globalTags.Count > 0)
            {
                StringBuilder sb = new();
                foreach (string s in _globalTags)
                {
                    sb.Append(s);
                }
                _symbols.Add(sb.ToString(), out semanticInterpretationGlobals);
                semanticInterpretationGlobals = _symbols.OffsetFromId(semanticInterpretationGlobals);
                if (semanticInterpretationGlobals > ushort.MaxValue)
                {
                    throw new OverflowException(SR.Get(SRID.TooManyRulesWithSemanticsGlobals));
                }
            }

            // Write the method names as symbols
            foreach (ScriptRef script in _scriptRefs)
            {
                _symbols.Add(script._sMethod, out script._idSymbol);
            }
            // get the header
            CfgGrammar.CfgSerializedHeader header = BuildHeader(sortedStates, cBasePath, unchecked((ushort)semanticInterpretationGlobals), out cArcs, out pWeights);
            streamBuffer.WriteStream(header);

            //
            //  For the string blobs, we must explicitly report I/O error since the blobs don't
            //  use the error log facility.
            //
            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.pszWords);
            streamBuffer.WriteArrayChar(_words.SerializeData(), _words.SerializeSize());

            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.pszSymbols);
            streamBuffer.WriteArrayChar(_symbols.SerializeData(), _symbols.SerializeSize());

            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.pRules);
            foreach (Rule rule in _rules)
            {
                rule.Serialize(streamBuffer);
            }

            if (cBasePath > 0)
            {
                streamBuffer.WriteArrayChar(_basePath.ToCharArray(), _basePath.Length);

                // Add a zero to be compatible with SAPI 5
                System.Diagnostics.Debug.Assert(_basePath.Length + 1 == cBasePath);
                streamBuffer.WriteArrayChar(s_achZero, 1);

                // Zero-pad to align following structures
                streamBuffer.WriteArray(s_abZero3, cBasePath * Helpers._sizeOfChar & 3);
            }

            //
            //  Write a dummy 0 index state entry
            //
            CfgArc dummyArc = new();

            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.pArcs);
            streamBuffer.WriteStream(dummyArc);

            int ulWeightOffset = 1;
            uint arcOffset = 1;

            bool semanticInterpretation = (GrammarOptions & GrammarOptions.MssV1) == GrammarOptions.MssV1;
            foreach (State state in sortedStates)
            {
                state.SerializeStateEntries(streamBuffer, semanticInterpretation, pWeights, ref arcOffset, ref ulWeightOffset);
            }

            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.pWeights);
            if (_fNeedWeightTable)
            {
                streamBuffer.WriteArray<float>(pWeights, cArcs);
            }

            System.Diagnostics.Debug.Assert(streamBuffer.Stream.Position - startStreamPosition == header.tags);
            if (!semanticInterpretation)
            {
                foreach (State state in sortedStates)
                {
                    state.SetEndArcIndexForTags();
                }
            }

            // Remove the orphaned arcs
            // This could happen in the case of a <item repeat=0-0"> <tag /></item>
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                // When arc are created the index is set to zero. This value changes during serialization
                // if an arc references it
                if (_tags[i]._cfgTag.ArcIndex == 0)
                {
                    _tags.RemoveAt(i);
                }
            }
            // Sort the _tags array by ArcIndex
            _tags.Sort();

            // Write the _tags array
            foreach (Tag tag in _tags)
            {
                tag.Serialize(streamBuffer);
            }

            // Write the script references and the IL write after the header so getting it for the grammar
            // Does not require a seek to the end of the file
            System.Diagnostics.Debug.Assert(header.pScripts == 0 || streamBuffer.Stream.Position - startStreamPosition == header.pScripts);
            foreach (ScriptRef script in _scriptRefs)
            {
                script.Serialize(_symbols, streamBuffer);
            }

            // Write the assembly bits
            // (Not supported on this platform)
        }

        /// <summary>
        /// Description:
        /// Combine the current data in a grammar with one coming from a CFG
        /// </summary>
        internal static Backend CombineGrammar(string ruleName, Backend org, Backend extra)
        {
            Backend be = new();
            be._fLoadedFromBinary = true;
            be._fNeedWeightTable = org._fNeedWeightTable;
            be._grammarMode = org._grammarMode;
            be._grammarOptions = org._grammarOptions;

            // Hash source state to destination state
            Dictionary<State, State> srcToDestHash = new();

            // Find the rule
            foreach (Rule orgRule in org._rules)
            {
                if (orgRule.Name == ruleName)
                {
                    be.CloneSubGraph(orgRule, org, extra, srcToDestHash, true);
                }
            }
            return be;
        }

        internal State CreateNewState(Rule rule)
        {
            return _states.CreateNewState(rule);
        }

        internal void DeleteState(State state)
        {
            _states.DeleteState(state);
        }

        internal void MoveInputTransitionsAndDeleteState(State from, State to)
        {
            _states.MoveInputTransitionsAndDeleteState(from, to);
        }

        internal void MoveOutputTransitionsAndDeleteState(State from, State to)
        {
            _states.MoveOutputTransitionsAndDeleteState(from, to);
        }

        /// <summary>
        /// Tries to find the rule's initial state handle. If both a name and an id
        /// are provided, then both have to match in order for this call to succeed.
        /// If the rule doesn't already exist then we define it if fCreateIfNotExists,
        /// otherwise we return an error ().
        ///
        ///     - pszRuleName   name of rule to find/define     (null: don't care)
        ///     - ruleId      id of rule to find/define       (0: don't care)
        ///     - dwAttribute   rule attribute for defining the rule
        ///     - fCreateIfNotExists    creates the rule using name, id, and attributes
        ///                             in case the rule doesn't already exist
        ///
        /// throws:
        ///       S_OK, E_INVALIDARG, E_OUTOFMEMORY
        ///       SPERR_RULE_NOT_FOUND        -- no rule found and we don't create a new one
        ///       SPERR_RULE_NAME_ID_CONFLICT -- rule name and id don't match
        /// </summary>
        internal Rule CreateRule(string name, SPCFGRULEATTRIBUTES attributes)
        {

            SPCFGRULEATTRIBUTES allFlags = SPCFGRULEATTRIBUTES.SPRAF_TopLevel | SPCFGRULEATTRIBUTES.SPRAF_Active | SPCFGRULEATTRIBUTES.SPRAF_Export | SPCFGRULEATTRIBUTES.SPRAF_Import | SPCFGRULEATTRIBUTES.SPRAF_Interpreter | SPCFGRULEATTRIBUTES.SPRAF_Dynamic | SPCFGRULEATTRIBUTES.SPRAF_Root;

            if (attributes != 0 && ((attributes & ~allFlags) != 0 || ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Import) != 0 && (attributes & SPCFGRULEATTRIBUTES.SPRAF_Export) != 0)))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidFlagsSet), nameof(attributes));
            }

            // SAPI does not properly handle a rule marked as Import and TopLevel/Active/Root.
            // - To maintain maximal backwards compatibility, if a rule is marked as Import, we will unmark TopLevel/Active/Root.
            // - This changes the behavior when application tries to activate this rule.  However, given that it is already
            //   broken/fragile, we believe it is better to change the behavior.
            if ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Import) != 0 && ((attributes & SPCFGRULEATTRIBUTES.SPRAF_TopLevel) != 0 || (attributes & SPCFGRULEATTRIBUTES.SPRAF_Active) != 0 || (attributes & SPCFGRULEATTRIBUTES.SPRAF_Root) != 0))
            {
                attributes &= ~(SPCFGRULEATTRIBUTES.SPRAF_TopLevel | SPCFGRULEATTRIBUTES.SPRAF_Active | SPCFGRULEATTRIBUTES.SPRAF_Root);
            }

            if ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Import) != 0 && (name[0] == '\0'))
            {
                LogError(name, SRID.InvalidImport);
            }

            if (_fLoadedFromBinary)
            {
                // Scan all non-dynamic names and prevent a duplicate...
                foreach (Rule r in _rules)
                {
                    string wpszName = _symbols[r._cfgRule._nameOffset];

                    if (!r._cfgRule.Dynamic && name == wpszName)
                    {
                        LogError(name, SRID.DuplicatedRuleName);
                    }
                }
            }

            int idString;
            int cImportedRule = 0;
            Rule rule = new(this, name, _symbols.Add(name, out idString), attributes, _ruleIndex, 0, _grammarOptions & GrammarOptions.TagFormat, ref cImportedRule);

            rule._iSerialize2 = _ruleIndex++;

            if ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Root) != 0)
            {
                if (_rootRule != null)
                {
                    //We already have a root rule, return error code.
                    LogError(name, SRID.RootRuleAlreadyDefined);
                }
                else
                {
                    _rootRule = rule;
                }
            }

            // Add rule to RuleListByName and RuleListByID hash tables.
            if (rule._cfgRule._nameOffset != 0)
            {
                _nameOffsetRules.Add(rule._cfgRule._nameOffset, rule);
            }

            //
            //  It is important to insert this at the tail for dynamic rules to
            //  retain their slot number.
            //
            _rules.Add(rule);
            _rules.Sort();

            return rule;
        }

        /// <summary>
        /// Internal method for finding rule in rule list
        /// </summary>
        internal Rule FindRule(string sRule)
        {
            Rule rule = null;

            if (_nameOffsetRules.Count > 0)
            {
                // Find rule corresponding to name symbol offset corresponding to the RuleName
                int iWord = _symbols.Find(sRule);

                if (iWord > 0)
                {
                    int dwSymbolOffset = _symbols.OffsetFromId(iWord);

                    System.Diagnostics.Debug.Assert(dwSymbolOffset == 0 || _symbols[iWord] == sRule);

                    rule = dwSymbolOffset > 0 && _nameOffsetRules.TryGetValue(dwSymbolOffset, out Rule value) ? value : null;
                }
            }

            if (rule != null)
            {
                string sRuleFound = rule.Name;

                // at least one of the 2 arguments matched
                // names either match or they are both null!
                if (!((string.IsNullOrEmpty(sRule) || (!string.IsNullOrEmpty(sRule) && !string.IsNullOrEmpty(sRuleFound) && sRuleFound == sRule))))
                {
                    LogError(sRule, SRID.RuleNameIdConflict);
                }
            }

            return rule ?? null;
        }

        /// <summary>
        /// Adds a word transition from hFromState to hToState. If hToState == null
        /// then the arc will be to the (implicit) terminal state. If psz == null then
        /// we add an epsilon transition. Properties are pushed back to the
        /// first un-ambiguous arc in case we can share a common initial state path.
        /// The weight will be placed on the first arc (if there exists an arc with
        /// the same word but different weight we will create a new arc).
        /// </summary>
        internal Arc WordTransition(string sWord, float flWeight, int requiredConfidence)
        {
            return CreateTransition(sWord, flWeight, requiredConfidence);
        }

        internal Arc SubsetTransition(string text, MatchMode matchMode)
        {
            // Performs white space normalization in place
            text = NormalizeTokenWhiteSpace(text);

            return new Arc(text, null, _words, 1.0f, CfgGrammar.SP_NORMAL_CONFIDENCE, null, matchMode, ref _fNeedWeightTable);
        }

        /// <summary>
        /// Adds a rule (reference) transition from hFromState to hToState.
        /// hRule can also be one of these special transition handles:
        ///     SPRULETRANS_WILDCARD   :    "WILDCARD" transition
        ///     SPRULETRANS_DICTATION  :    single word from dictation
        ///     SPRULETRANS_TEXTBUFFER :    "TEXTBUFFER" transition
        /// </summary>
        /// <param name="rule">must be initial state of rule</param>
        /// <param name="parentRule">Rule calling the ruleref</param>
        /// <param name="flWeight">Weight</param>
        internal Arc RuleTransition(Rule rule, Rule parentRule, float flWeight)
        {
            Rule ruleToTransitionTo = null;

            if (flWeight < 0.0f)
            {
                XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
            }

            Rule specialRuleTrans = null;

            if (rule == CfgGrammar.SPRULETRANS_WILDCARD || rule == CfgGrammar.SPRULETRANS_DICTATION || rule == CfgGrammar.SPRULETRANS_TEXTBUFFER)
            {
                specialRuleTrans = rule;
            }
            else
            {
                ruleToTransitionTo = rule;
            }

            bool fNeedWeightTable = false;
            Arc arc = new(null, ruleToTransitionTo, _words, flWeight, '\0', specialRuleTrans, MatchMode.AllWords, ref fNeedWeightTable);

            AddArc(arc);

            if (ruleToTransitionTo != null && parentRule != null)
            {
                ruleToTransitionTo._listRules.Insert(0, parentRule);
            }

            return arc;
        }

        /// <summary>
        /// Adds a word transition from hFromState to hToState. If hToState == null
        /// then the arc will be to the (implicit) terminal state. If psz == null then
        /// we add an epsilon transition. Properties are pushed back to the
        /// first un-ambiguous arc in case we can share a common initial state path.
        /// The weight will be placed on the first arc (if there exists an arc with
        /// the same word but different weight we will create a new arc).
        /// </summary>
        internal Arc EpsilonTransition(float flWeight)
        {
            return CreateTransition(null, flWeight, CfgGrammar.SP_NORMAL_CONFIDENCE);
        }

        internal void AddSemanticInterpretationTag(Arc arc, CfgGrammar.CfgProperty propertyInfo)
        {

            Tag tag = new(this, propertyInfo);
            _tags.Add(tag);

            arc.AddStartTag(tag);
            arc.AddEndTag(tag);
        }

        internal void AddPropertyTag(Arc start, Arc end, CfgGrammar.CfgProperty propertyInfo)
        {

            Tag tag = new(this, propertyInfo);
            _tags.Add(tag);

            start.AddStartTag(tag);
            end.AddEndTag(tag);
        }

        /// <summary>
        /// Traverse the graph starting from SrcStartState, cloning each state as we go along,
        /// cloning each transition except ones originating from SrcEndState, and return
        /// the cloned state corresponding to SrcEndState.
        /// </summary>
        internal State CloneSubGraph(State srcFromState, State srcEndState, State destFromState)
        {
            Dictionary<State, State> SrcToDestHash = new();    // Hash source state to destination state
            Stack<State> CloneStack = new();       // States to process
            Dictionary<Tag, Tag> tags = new();

            // Add initial state to CloneStack and SrcToDestHash.
            SrcToDestHash.Add(srcFromState, destFromState);
            CloneStack.Push(srcFromState);

            // While there are still states on the CloneStack (ToDo collection)
            while (CloneStack.Count > 0)
            {
                srcFromState = CloneStack.Pop();
                destFromState = SrcToDestHash[srcFromState];
                System.Diagnostics.Debug.Assert(destFromState != null);

                // For each transition from srcFromState (except SrcEndState)
                foreach (Arc arc in srcFromState.OutArcs)
                {
                    // - Lookup the DestToState corresponding to SrcToState
                    State srcToState = arc.End;
                    State destToState = null;

                    if (srcToState != null)
                    {
                        // - If not found, clone a new DestToState, add SrcToState.DestToState to SrcToDestHash, and add SrcToState to CloneStack.
                        if (!SrcToDestHash.TryGetValue(srcToState, out destToState))
                        {
                            destToState = CreateNewState(srcToState.Rule);
                            SrcToDestHash.Add(srcToState, destToState);
                            CloneStack.Push(srcToState);
                        }
                    }

                    // - Clone the transition from SrcFromState.SrcToState at DestFromState.DestToState
                    // -- Clone Arc
                    Arc newArc = new(arc, destFromState, destToState);
                    AddArc(newArc);

                    // -- Clone SemanticTag
                    newArc.CloneTags(arc, _tags, tags, null);

                    // -- Add Arc
                    newArc.ConnectStates();
                }
            }

            System.Diagnostics.Debug.Assert(tags.Count == 0);
            return SrcToDestHash[srcEndState];
        }

        /// <summary>
        /// Traverse the graph starting from SrcStartState, cloning each state as we go along,
        /// cloning each transition except ones originating from SrcEndState, and return
        /// the cloned state corresponding to SrcEndState.
        /// </summary>
        internal void CloneSubGraph(Rule rule, Backend org, Backend extra, Dictionary<State, State> srcToDestHash, bool fromOrg)
        {
            Backend beSrc = fromOrg ? org : extra;

            List<State> CloneStack = new();       // States to process
            Dictionary<Tag, Tag> tags = new();

            // Push all the state for the top level rule
            CloneState(rule._firstState, CloneStack, srcToDestHash);

            // While there are still states on the CloneStack (ToDo collection)
            while (CloneStack.Count > 0)
            {
                State srcFromState = CloneStack[0];
                CloneStack.RemoveAt(0);
                State destFromState = srcToDestHash[srcFromState];
                // For each transition from srcFromState (except SrcEndState)
                foreach (Arc arc in srcFromState.OutArcs)
                {
                    // - Lookup the DestToState corresponding to SrcToState
                    State srcToState = arc.End;
                    State destToState = null;

                    if (srcToState != null)
                    {
                        if (!srcToDestHash.ContainsKey(srcToState))
                        {
                            // - If not found, then it is a new rule, just clown it.
                            CloneState(srcToState, CloneStack, srcToDestHash);
                        }
                        destToState = srcToDestHash[srcToState];
                    }

                    // - Clone the transition from SrcFromState.SrcToState at DestFromState.DestToState
                    // -- Clone Arc
                    int newWordId = arc.WordId;
                    if (beSrc != null && arc.WordId > 0)
                    {
                        _words.Add(beSrc.Words[arc.WordId], out newWordId);
                    }

                    Arc newArc = new(arc, destFromState, destToState, newWordId);

                    // -- Clone SemanticTag
                    newArc.CloneTags(arc, _tags, tags, this);

                    // For rule ref push the first state of the ruleref
                    if (arc.RuleRef != null)
                    {
                        string ruleName;

                        // Check for DYNAMIC grammars
                        if (arc.RuleRef.Name.StartsWith("URL:DYNAMIC#", StringComparison.Ordinal))
                        {
                            ruleName = arc.RuleRef.Name.Substring(12);
                            if (fromOrg && FindInRules(ruleName) == null)
                            {
                                Rule ruleExtra = extra.FindInRules(ruleName);
                                if (ruleExtra == null)
                                {
                                    XmlParser.ThrowSrgsException(SRID.DynamicRuleNotFound, ruleName);
                                }
                                CloneSubGraph(ruleExtra, org, extra, srcToDestHash, false);
                            }
                        }
                        else if (arc.RuleRef.Name.StartsWith("URL:STATIC#", StringComparison.Ordinal))
                        {
                            ruleName = arc.RuleRef.Name.Substring(11);
                            if (fromOrg == false && FindInRules(ruleName) == null)
                            {
                                Rule ruleOrg = org.FindInRules(ruleName);
                                if (ruleOrg == null)
                                {
                                    XmlParser.ThrowSrgsException(SRID.DynamicRuleNotFound, ruleName);
                                }
                                CloneSubGraph(ruleOrg, org, extra, srcToDestHash, true);
                            }
                        }
                        else
                        {
                            ruleName = arc.RuleRef.Name;
                            Rule ruleExtra = org.FindInRules(ruleName);
                            if (fromOrg == false)
                            {
                                CloneSubGraph(arc.RuleRef, org, extra, srcToDestHash, true);
                            }
                        }
                        Rule refRule = FindInRules(ruleName);
                        refRule ??= CloneState(arc.RuleRef._firstState, CloneStack, srcToDestHash);
                        newArc.RuleRef = refRule;
                    }

                    // -- Add Arc
                    newArc.ConnectStates();
                }
            }
            System.Diagnostics.Debug.Assert(tags.Count == 0);
        }

        /// <summary>
        /// Delete disconnected subgraph starting at hState.
        /// Traverse the graph starting from SrcStartState, and delete each state as we go along.
        /// </summary>
        internal void DeleteSubGraph(State state)
        {
            // Add initial state to DeleteStack.
            Stack<State> stateToProcess = new();           // States to delete
            Collection<Arc> arcsToDelete = new();
            Collection<State> statesToDelete = new();
            stateToProcess.Push(state);

            // While there are still states on the listDelete (ToDo collection)
            while (stateToProcess.Count > 0)
            {
                // For each transition from state,
                state = stateToProcess.Pop();
                statesToDelete.Add(state);
                arcsToDelete.Clear();

                // Accumulate the arcs to delete and add new states to the stack of states to process
                foreach (Arc arc in state.OutArcs)
                {
                    // Add EndState to listDelete, if unique
                    State endState = arc.End;

                    // Add this state to the list of states to delete
                    if (endState != null && !stateToProcess.Contains(endState) && !statesToDelete.Contains(endState))
                    {
                        stateToProcess.Push(endState);
                    }
                    arcsToDelete.Add(arc);
                }
                // Clear up the arcs
                foreach (Arc arc in arcsToDelete)
                {
                    arc.Start = arc.End = null;
                }
            }

            foreach (State stateToDelete in statesToDelete)
            {
                // Delete state and remove from listDelete
                System.Diagnostics.Debug.Assert(stateToDelete != null);
                System.Diagnostics.Debug.Assert(stateToDelete.InArcs.IsEmpty);
                System.Diagnostics.Debug.Assert(stateToDelete.OutArcs.IsEmpty);
                DeleteState(stateToDelete);
            }
        }

        /// <summary>
        /// Modify the placeholder rule attributes after it has been created.
        /// This is only safe to use in the context of SrgsGrammarCompiler.
        /// </summary>
        internal void SetRuleAttributes(Rule rule, SPCFGRULEATTRIBUTES dwAttributes)
        {
            // Check if this is the Root rule
            if ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Root) != 0)
            {
                if (_rootRule != null)
                {
                    //We already have a root rule, return error code.
                    XmlParser.ThrowSrgsException(SRID.RootRuleAlreadyDefined);
                }
                else
                {
                    _rootRule = rule;
                }
            }

            rule._cfgRule.TopLevel = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_TopLevel) != 0);
            rule._cfgRule.DefaultActive = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Active) != 0);
            rule._cfgRule.PropRule = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Interpreter) != 0);
            rule._cfgRule.Export = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Export) != 0);
            rule._cfgRule.Dynamic = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Dynamic) != 0);
            rule._cfgRule.Import = ((dwAttributes & SPCFGRULEATTRIBUTES.SPRAF_Import) != 0);
        }

        /// <summary>
        /// Set the path from which relative grammar imports are calculated. As specified by xml:base / meta base
        /// Null or empty string will clear any existing base path.
        /// </summary>
        internal void SetBasePath(string sBasePath)
        {
            if (!string.IsNullOrEmpty(sBasePath))
            {
                // Validate base path.
                Uri uri = new(sBasePath, UriKind.RelativeOrAbsolute);

                //Url Canonicalized
                _basePath = uri.ToString();
            }
            else
            {
                _basePath = null;
            }
        }

        /// <summary>
        /// Perform white space normalization in place.
        /// - Trim leading/trailing white spaces.
        /// - Collapse white space sequences to a single ' '.
        /// </summary>
        internal static string NormalizeTokenWhiteSpace(string sToken)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(sToken));

            // Trim leading and ending white spaces
            sToken = sToken.Trim(Helpers._achTrimChars);

            // Easy out if there are no consecutive double white spaces
            if (!sToken.Contains("  ", StringComparison.Ordinal))
            {
                return sToken;
            }

            // Normalize internal spaces
            char[] achSrc = sToken.ToCharArray();
            int iDest = 0;

            for (int i = 0; i < achSrc.Length;)
            {
                // Collapsed multiple white spaces into ' '
                if (achSrc[i] == ' ')
                {
                    do
                    {
                        i++;
                    } while (achSrc[i] == ' ');

                    achSrc[iDest++] = ' ';
                    continue;
                }

                // Copy the non-white space character
                achSrc[iDest++] = achSrc[i++];
            }

            return new string(achSrc, 0, iDest);
        }

        #endregion

        #region Internal Property

        internal StringBlob Words
        {
            get
            {
                return _words;
            }
        }

        internal StringBlob Symbols
        {
            get
            {
                return _symbols;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Description:
        /// Load compiled grammar data. This overwrites any existing data in the grammar
        /// We end up with containers of words, symbols, rules, arcs, states and state handles, etc.
        /// </summary>
        internal void InitFromBinaryGrammar(StreamMarshaler streamHelper)
        {
            CfgGrammar.CfgHeader header = CfgGrammar.ConvertCfgHeader(streamHelper);

            _words = header.pszWords;
            _symbols = header.pszSymbols;

            _grammarOptions = header.GrammarOptions;

            //
            // Build up the internal representation
            //
            State[] apStateTable = new State[header.arcs.Length];
            SortedDictionary<int, Rule> ruleFirstArcs = new();

            //
            // Initialize the rules
            //

            int previousCfgLastRules = _rules.Count;

            BuildRulesFromBinaryGrammar(header, apStateTable, ruleFirstArcs, previousCfgLastRules);

            //
            //  Initialize the arcs
            //
            Arc[] apArcTable = new Arc[header.arcs.Length];
            bool fLastArcNull = true;
            CfgArc pLastArc = new();
            State currentState = null;
            SortedDictionary<int, Rule>.Enumerator ieFirstArcs = ruleFirstArcs.GetEnumerator();

            // If no rules, then we have no arcs
            if (ieFirstArcs.MoveNext())
            {
                KeyValuePair<int, Rule> kvFirstArc = ieFirstArcs.Current;
                Rule ruleCur = kvFirstArc.Value;

                //  We repersist the static AND dynamic parts for now. This allows the grammar to be queried
                //  with the automation interfaces
                for (int k = 1; k < header.arcs.Length; k++)
                {
                    CfgArc arc = header.arcs[k];

                    // Reset the Transition index based on the combined string blobs
                    if (arc.RuleRef)
                    {
                        // for a ruleref offset the rule index
                        ruleCur._listRules.Add(_rules[(int)arc.TransitionIndex]);
                    }

                    if (kvFirstArc.Key == k)
                    {
                        // we are entering a new rule now
                        ruleCur = kvFirstArc.Value;

                        // Reset to zero once we have read the last rule.
                        if (ieFirstArcs.MoveNext())
                        {
                            kvFirstArc = ieFirstArcs.Current;
                        }
                    }

                    // new currentState?
                    if (fLastArcNull || pLastArc.LastArc)
                    {
                        if (apStateTable[k] == null)
                        {
                            uint hNewState = CfgGrammar.NextHandle;

                            apStateTable[k] = new State(ruleCur, hNewState, k);
                            AddState(apStateTable[k]);
                        }

                        currentState = apStateTable[k];
                    }

                    //
                    // now get the arc
                    //
                    int iNextArc = (int)(arc.NextStartArcIndex);
                    Arc newArc;
                    State targetState = null;

                    if (currentState != null && iNextArc != 0)
                    {
                        if (apStateTable[iNextArc] == null)
                        {
                            uint hNewState = CfgGrammar.NextHandle;

                            apStateTable[iNextArc] = new State(ruleCur, hNewState, iNextArc);
                            AddState(apStateTable[iNextArc]);
                        }

                        targetState = apStateTable[iNextArc];
                    }

                    float flWeight = header.weights != null ? header.weights[k] : CfgGrammar.DEFAULT_WEIGHT;

                    // determine properties of the arc now ...
                    if (arc.RuleRef)
                    {
                        Rule ruleToTransitionTo = _rules[(int)arc.TransitionIndex];

                        newArc = new Arc(null, ruleToTransitionTo, _words, flWeight, CfgGrammar.SP_NORMAL_CONFIDENCE, null, MatchMode.AllWords, ref _fNeedWeightTable);
                    }
                    else
                    {
                        int transitionIndex = (int)arc.TransitionIndex;
                        int ulSpecialTransitionIndex = (transitionIndex == CfgGrammar.SPWILDCARDTRANSITION || transitionIndex == CfgGrammar.SPDICTATIONTRANSITION || transitionIndex == CfgGrammar.SPTEXTBUFFERTRANSITION) ? transitionIndex : 0;
                        newArc = new Arc((ulSpecialTransitionIndex != 0) ? 0 : (int)arc.TransitionIndex, flWeight, arc.LowConfRequired ? CfgGrammar.SP_LOW_CONFIDENCE : arc.HighConfRequired ? CfgGrammar.SP_HIGH_CONFIDENCE : CfgGrammar.SP_NORMAL_CONFIDENCE, ulSpecialTransitionIndex, MatchMode.AllWords, ref _fNeedWeightTable);
                    }
                    newArc.Start = currentState;
                    newArc.End = targetState;

                    AddArc(newArc);
                    apArcTable[k] = newArc;
                    fLastArcNull = false;
                    pLastArc = arc;
                }
            }

            //  Initialize the Semantics tags
            for (int k = 1, iCurTag = 0; k < header.arcs.Length; k++)
            {
                CfgArc arc = header.arcs[k];

                if (arc.HasSemanticTag)
                {
                    System.Diagnostics.Debug.Assert(header.tags[iCurTag].StartArcIndex == k);

                    while (iCurTag < header.tags.Length && header.tags[iCurTag].StartArcIndex == k)
                    {
                        // we should already point to the tag
                        CfgSemanticTag semTag = header.tags[iCurTag];

                        Tag tag = new(this, semTag);

                        _tags.Add(tag);
                        apArcTable[tag._cfgTag.StartArcIndex].AddStartTag(tag);
                        apArcTable[tag._cfgTag.EndArcIndex].AddEndTag(tag);

                        // If we have ms-properties than _nameOffset != otherwise it is w3c tags.
                        if (semTag._nameOffset > 0)
                        {
                            tag._cfgTag._nameOffset = _symbols.OffsetFromId(_symbols.Find(_symbols.FromOffset(semTag._nameOffset)));
                        }
                        else
                        {
                            // The offset of the JScrip expression is stored in the value field.
                            tag._cfgTag._valueOffset = _symbols.OffsetFromId(_symbols.Find(_symbols.FromOffset(semTag._valueOffset)));
                        }
                        iCurTag++;
                    }
                }
            }
            _fNeedWeightTable = true;
            if (header.BasePath != null)
            {
                SetBasePath(header.BasePath);
            }

            _guid = header.GrammarGUID;
            _langId = header.langId;
            _grammarMode = header.GrammarMode;

            _fLoadedFromBinary = true;
            // Save Last ArcIndex

        }

        private Arc CreateTransition(string sWord, float flWeight, int requiredConfidence)
        {
            // epsilon transition for empty words
            return AddSingleWordTransition(!string.IsNullOrEmpty(sWord) ? sWord : null, flWeight, requiredConfidence);
        }

        private CfgGrammar.CfgSerializedHeader BuildHeader(List<State> sortedStates, int cBasePath, ushort iSemanticGlobals, out int cArcs, out float[] pWeights)
        {
            cArcs = 1; // Start with offset one! (0 indicates dead state).
            pWeights = null;

            int cSemanticTags = 0;
            int cLargest = 0;

            foreach (State state in sortedStates)
            {
                // For new states SerializeId is INFINITE so we set it correctly here.
                // For existing states we preserve the index from loading,
                //  unless new states have been added in.
                state.SerializeId = cArcs;

                int thisState = state.NumArcs;

#if DEBUG
                if (thisState == 0 && state.InArcs.IsEmpty && state.Rule._cStates > 1)
                {
                    XmlParser.ThrowSrgsException(SRID.StateWithNoArcs);
                }
#endif
                cArcs += thisState;
                if (cLargest < thisState)
                {
                    cLargest = thisState;
                }
                cSemanticTags += state.NumSemanticTags;
            }

            CfgGrammar.CfgSerializedHeader header = new();
            uint ulOffset = (uint)Marshal.SizeOf<CfgGrammar.CfgSerializedHeader>();

            header.FormatId = CfgGrammar._SPGDF_ContextFree;
            _guid = Guid.NewGuid();
            header.GrammarGUID = _guid;
            header.LangID = (ushort)_langId;
            header.pszSemanticInterpretationGlobals = iSemanticGlobals;
            header.cArcsInLargestState = cLargest;

            header.cchWords = _words.StringSize();
            header.cWords = _words.Count;

            // For compat with SAPI 5.x add one to cWords if there's more than one word.
            // The CFGEngine code assumes cWords includes the initial empty-string word.
            // See PS 11491 and 61982.
            if (header.cWords > 0)
            {
                header.cWords++;
            }

            header.pszWords = ulOffset;
            ulOffset += (uint)_words.SerializeSize() * Helpers._sizeOfChar;
            header.cchSymbols = _symbols.StringSize();
            header.pszSymbols = ulOffset;
            ulOffset += (uint)_symbols.SerializeSize() * Helpers._sizeOfChar;
            header.cRules = _rules.Count;
            header.pRules = ulOffset;
            ulOffset += (uint)(_rules.Count * Marshal.SizeOf<CfgRule>());
            header.cBasePath = cBasePath > 0 ? ulOffset : 0; //If there is no base path offset is set to zero
            ulOffset += (uint)((cBasePath * Helpers._sizeOfChar + 3) & ~3);
            header.cArcs = cArcs;
            header.pArcs = ulOffset;
            ulOffset += (uint)(cArcs * Marshal.SizeOf<CfgArc>());
            if (_fNeedWeightTable)
            {
                header.pWeights = ulOffset;
                ulOffset += (uint)(cArcs * sizeof(float));
                pWeights = new float[cArcs];
                pWeights[0] = 0.0f;
            }
            else
            {
                header.pWeights = 0;
                ulOffset += 0;
            }

            if (_rootRule != null)
            {
                //We have a root rule
                header.ulRootRuleIndex = (uint)_rootRule._iSerialize;
            }
            else
            {
                //-1 means there is no root rule
                header.ulRootRuleIndex = 0xFFFFFFFF;
            }

            header.GrammarOptions = _grammarOptions | ((_alphabet == AlphabetType.Sapi) ? 0 : GrammarOptions.IpaPhoneme);
            header.GrammarOptions |= _scriptRefs.Count > 0 ? GrammarOptions.STG | GrammarOptions.KeyValuePairSrgs : 0;
            header.GrammarMode = (uint)_grammarMode;
            header.cTags = cSemanticTags;
            header.tags = ulOffset;
            ulOffset += (uint)(cSemanticTags * Marshal.SizeOf<CfgSemanticTag>());
            header.cScripts = _scriptRefs.Count;
            header.pScripts = header.cScripts > 0 ? ulOffset : 0;
            ulOffset += (uint)(_scriptRefs.Count * Marshal.SizeOf<CfgScriptRef>());
            header.cIL = 0;
            header.pIL = 0;
            ulOffset += (uint)(header.cIL * sizeof(byte));
            header.cPDB = 0;
            header.pPDB = 0;
            ulOffset += (uint)(header.cPDB * sizeof(byte));
            header.ulTotalSerializedSize = ulOffset;
            return header;
        }

        private CfgGrammar.CfgHeader BuildRulesFromBinaryGrammar(CfgGrammar.CfgHeader header, State[] apStateTable, SortedDictionary<int, Rule> ruleFirstArcs, int previousCfgLastRules)
        {
            for (int i = 0; i < header.rules.Length; i++)
            {
                // Check if the rule does not exist already
                CfgRule cfgRule = header.rules[i];
                int firstArc = (int)cfgRule.FirstArcIndex;

                cfgRule._nameOffset = _symbols.OffsetFromId(_symbols.Find(header.pszSymbols.FromOffset(cfgRule._nameOffset)));

                Rule rule = new(this, _symbols.FromOffset(cfgRule._nameOffset), cfgRule, i + previousCfgLastRules, _grammarOptions & GrammarOptions.TagFormat, ref _cImportedRules);

                rule._firstState = _states.CreateNewState(rule);
                _rules.Add(rule);

                // Add the rule to the list of firstArc/rule
                if (firstArc > 0)
                {
                    ruleFirstArcs.Add((int)cfgRule.FirstArcIndex, rule);
                }

                rule._fStaticRule = (cfgRule.Dynamic) ? false : true;
                rule._cfgRule.DirtyRule = false;

                // by default loaded static rules have an exist
                rule._fHasExitPath = (rule._fStaticRule) ? true : false;

                // or they wouldn't be there in the first place
                if (firstArc != 0)
                {
                    System.Diagnostics.Debug.Assert(apStateTable[firstArc] == null);
                    rule._firstState.SerializeId = (int)cfgRule.FirstArcIndex;
                    apStateTable[firstArc] = rule._firstState;
                }

                if (rule._cfgRule.HasResources)
                {
                    throw new NotImplementedException();
                }

                if (header.ulRootRuleIndex == i)
                {
                    _rootRule = rule;
                }

                // Add rule to RuleListByName and RuleListByID hash tables.
                if (rule._cfgRule._nameOffset != 0)
                {
                    // Look for the rule in the original CFG and map it in the combined string blobs
                    _nameOffsetRules.Add(rule._cfgRule._nameOffset, rule);
                }
            }
            return header;
        }

        private Rule CloneState(State srcToState, List<State> CloneStack, Dictionary<State, State> srcToDestHash)
        {
            bool newRule = false;
            int posDynamic = srcToState.Rule.Name.IndexOf("URL:DYNAMIC#", StringComparison.Ordinal);
            string ruleName = posDynamic != 0 ? srcToState.Rule.Name : srcToState.Rule.Name.Substring(12);
            Rule dstRule = FindInRules(ruleName);

            // Clone this rule into this GrammarBuilder if it does not exist yet
            if (dstRule == null)
            {
                dstRule = srcToState.Rule.Clone(_symbols, ruleName);
                _rules.Add(dstRule);
                newRule = true;
            }

            // Should not exist yet
            System.Diagnostics.Debug.Assert(!srcToDestHash.ContainsKey(srcToState));

            // push all the states for that rule
            State newState = CreateNewState(dstRule);
            srcToDestHash.Add(srcToState, newState);
            CloneStack.Add(srcToState);

            if (newRule)
            {
                dstRule._firstState = newState;
            }

            return dstRule;
        }

        private Rule FindInRules(string ruleName)
        {
            foreach (Rule rule in _rules)
            {
                if (rule.Name == ruleName)
                {
                    return rule;
                }
            }
            return null;
        }

        private static void LogError(string rule, SRID srid, params object[] args)
        {
            string sError = SR.Get(srid, args);
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Rule=\"{0}\" - ", rule) + sError);
        }

        /// <summary>
        /// Connect arc to the state graph.
        /// </summary>
#if DEBUG
        private
#else
        private static
#endif
        void AddArc(Arc arc)
        {
#if DEBUG
            arc.Backend = this;
#endif
        }

        private void ValidateAndTagRules()
        {
            //

            bool fAtLeastOneRule = false;
            int ulIndex = 0;

            foreach (Rule rule in _rules)
            {
                // set _fHasExitPath = true for empty dynamic grammars and imported rules
                // Clear this for the next loop through the rules....
                rule._fHasExitPath |= (rule._cfgRule.Dynamic | rule._cfgRule.Import) ? true : false;
                rule._iSerialize = ulIndex++;
                fAtLeastOneRule |= (rule._cfgRule.Dynamic || rule._cfgRule.TopLevel || rule._cfgRule.Export);
                rule.Validate();
            }
#if DEBUG
            //
            //  Now make sure that all rules have an exit path.
            //
            foreach (Rule rule in _rules)
            {
                _ulRecursiveDepth = 0;

                //The following function will use recursive function that might change _ulRecursiveDepth
                rule.CheckForExitPath(ref _ulRecursiveDepth);
            }
#endif
            //
            //  Check each exported rule if it has a dynamic rule in its "scope"
            //
            foreach (Rule rule in _rules)
            {
                if (rule._cfgRule.Dynamic)
                {
                    rule._cfgRule.HasDynamicRef = true;
                    _ulRecursiveDepth = 0;
                    rule.PopulateDynamicRef(ref _ulRecursiveDepth);
                }
            }
        }

        private void CheckLeftRecursion(List<State> states)
        {
            bool fReachedEndState;
            foreach (State state in states)
            {
                state.CheckLeftRecursion(out fReachedEndState);
            }
        }

        private Arc AddSingleWordTransition(string s, float flWeight, int requiredConfidence)
        {

            Arc arc = new(s, null, _words, flWeight, requiredConfidence, null, MatchMode.AllWords, ref _fNeedWeightTable);
            AddArc(arc);
            return arc;
        }

        internal void AddState(State state)
        {
            _states.Add(state);
        }

        #endregion

        #region Internal Properties

        internal int LangId
        {
            get
            {
                return _langId;
            }
            set
            {
                _langId = value;
            }
        }

        internal GrammarOptions GrammarOptions
        {
            get
            {
                return _grammarOptions;
            }
            set
            {
                _grammarOptions = value;
            }
        }

        internal GrammarType GrammarMode
        {
            set
            {
                _grammarMode = value;
            }
        }

        internal AlphabetType Alphabet
        {
            get
            {
                return _alphabet;
            }
            set
            {
                _alphabet = value;
            }
        }

        internal Collection<string> GlobalTags
        {
            get
            {
                return _globalTags;
            }
            set
            {
                _globalTags = value;
            }
        }

        internal Collection<ScriptRef> ScriptRefs
        {
            set
            {
                _scriptRefs = value;
            }
        }

        #endregion

        #region Private Fields

        private int _langId = CultureInfo.CurrentUICulture.LCID;

        private StringBlob _words;

        private StringBlob _symbols;

        //private int _cResources;

        private Guid _guid;

        private bool _fNeedWeightTable;

        private Graph _states = new();

        private List<Rule> _rules = new();

        private int _ruleIndex;

        private Dictionary<int, Rule> _nameOffsetRules = new();

        private Rule _rootRule;

        private GrammarOptions _grammarOptions = GrammarOptions.KeyValuePairs;

        // It is used sequentially. So there is no thread issue
        private int _ulRecursiveDepth;

        // Path from which relative grammar imports are calculated. As specified by xml:base
        private string _basePath;

        // Collection of all SemanticTags in the grammar (sorted by StartArc)
        private List<Tag> _tags = new();

        // Voice or DTMF
        private GrammarType _grammarMode = GrammarType.VoiceGrammar;

        // Pron information is either IPA or SAPI
        private AlphabetType _alphabet = AlphabetType.Sapi;

        // Global value for the semantic interpretation tags
        private Collection<string> _globalTags = new();

        //
        private static byte[] s_abZero3 = new byte[] { 0, 0, 0 };

        private static char[] s_achZero = new char[] { '\0' };
        private int _cImportedRules;

        // List of cd /reference Rule->rule 'on'method-> .NET method
        private Collection<ScriptRef> _scriptRefs = new();

        private bool _fLoadedFromBinary;

        #endregion
    }
}
