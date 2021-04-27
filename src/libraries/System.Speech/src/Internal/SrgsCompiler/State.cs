// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.SrgsCompiler
{
    /// <summary>
    /// Class representing a state in the grammar. Note that states are not stored in the binary format
    /// instead all the arcs are, with a flag to indicate the end arc out of a state */
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{ToString ()}")]
#endif
    internal sealed class State : IComparable<State>
    {
        #region Constructors

        internal State(Rule rule, uint hState, int iSerialize)
        {
            _rule = rule;
            _iSerialize = iSerialize;
            _id = hState;
        }

        internal State(Rule rule, uint hState)
            : this(rule, hState, (int)hState)
        {
        }

        #endregion

        #region internal Methods

        #region IComparable<State> Interface implementation

        int IComparable<State>.CompareTo(State state2)
        {
            return Compare(this, state2);
        }

        #endregion

        internal void SerializeStateEntries(StreamMarshaler streamBuffer, bool tagsCannotSpanOverMultipleArcs, float[] pWeights, ref uint iArcOffset, ref int iOffset)
        {
            // The arcs must be sorted before being written to disk.
            List<Arc> outArcs = _outArcs.ToList();
            outArcs.Sort();
            Arc lastArc = outArcs.Count > 0 ? outArcs[outArcs.Count - 1] : null;

            IEnumerator<Arc> enumArcs = ((IEnumerable<Arc>)outArcs).GetEnumerator();
            enumArcs.MoveNext();

            uint nextAvailableArc = (uint)outArcs.Count + iArcOffset;
            uint saveNextAvailableArc = nextAvailableArc;

            // Write the arc of the first epsilon arc with an arc has more than one semantic tag
            foreach (Arc arc in outArcs)
            {
                // Create the first arc.
                int cSemantics = arc.SemanticTagCount;

                // Set the semantic property reference for the first arc
                if (cSemantics > 0)
                {
                    arc.SetArcIndexForTag(0, iArcOffset, tagsCannotSpanOverMultipleArcs);
                }

                // Serialize the arc
                if (cSemantics <= 1)
                {
                    pWeights[iOffset++] = arc.Serialize(streamBuffer, lastArc == arc, iArcOffset++);
                }
                else
                {
                    // update the position of the current arc
                    ++iArcOffset;

                    // more than one arc, create an epsilon transition
                    pWeights[iOffset++] = Arc.SerializeExtraEpsilonWithTag(streamBuffer, arc, lastArc == arc, nextAvailableArc);

                    // reset the position of the next available slop for an arc
                    nextAvailableArc += (uint)cSemantics - 1;
                }
            }

            enumArcs = ((IEnumerable<Arc>)outArcs).GetEnumerator();
            enumArcs.MoveNext();

            // revert the position for the new arc
            nextAvailableArc = saveNextAvailableArc;

            // write the additional arcs if we have more than one semantic tag
            foreach (Arc arc in outArcs)
            {
                int cSemantics = arc.SemanticTagCount;

                if (cSemantics > 1)
                {
                    // If more than 2 arcs insert extra new epsilon states, one per semantic tag
                    for (int i = 1; i < cSemantics - 1; i++)
                    {
                        // Set the semantic property reference
                        arc.SetArcIndexForTag(i, iArcOffset, tagsCannotSpanOverMultipleArcs);

                        // reset the position of the next available slop for an arc
                        nextAvailableArc++;

                        // create an epsilon transition
                        pWeights[iOffset++] = Arc.SerializeExtraEpsilonWithTag(streamBuffer, arc, true, nextAvailableArc);

                        // update the position of the current arc
                        ++iArcOffset;
                    }

                    // Set the semantic property reference
                    arc.SetArcIndexForTag(cSemantics - 1, iArcOffset, tagsCannotSpanOverMultipleArcs);

                    // Add the real arc at the end
                    pWeights[iOffset++] = arc.Serialize(streamBuffer, true, iArcOffset++);

                    // reset the position of the next available slop for an arc
                    nextAvailableArc++;
                }
            }
        }

        internal void SetEndArcIndexForTags()
        {
            foreach (Arc arc in _outArcs)
            {
                arc.SetEndArcIndexForTags();
            }
        }

        #region State linked list

        // The pointers for 2 linked list are stored within each state.
        // When states are created, they added into a list, the '1' list.

        // The Members of the list are Set, Add, Remove, Prev and Next.

        internal void Init()
        {
            System.Diagnostics.Debug.Assert(_next == null && _prev == null);
        }

        internal State Add(State state)
        {
            _next = state;
            state._prev = this;
            return state;
        }

        internal void Remove()
        {
            if (_prev != null)
            {
                _prev._next = _next;
            }
            if (_next != null)
            {
                _next._prev = _prev;
            }
            _next = _prev = null;
        }

        internal State Next
        {
            get
            {
                return _next;
            }
        }

        internal State Prev
        {
            get
            {
                return _prev;
            }
        }

        #endregion

#if DEBUG
        internal void CheckExitPath(ref int iRecursiveDepth)
        {
            if (iRecursiveDepth > CfgGrammar.MAX_TRANSITIONS_COUNT)
            {
                XmlParser.ThrowSrgsException(SRID.MaxTransitionsCount);
            }

            foreach (Arc arc in _outArcs)
            {
                if (_rule._fHasExitPath)
                {
                    break;
                }

                if (arc.CheckingForExitPath)
                {
                    arc.CheckingForExitPath = true;
                    if (arc.RuleRef != null)
                    {
                        arc.RuleRef.CheckForExitPath(ref iRecursiveDepth);
                        if (arc.RuleRef._fHasExitPath)
                        {
                            if (arc.End == null)
                            {
                                _rule._fHasExitPath = true;
                            }
                            else
                            {
                                arc.End.CheckExitPath(ref iRecursiveDepth);
                            }
                        }
                    }
                    else
                    {
                        if (arc.End == null)
                        {
                            _rule._fHasExitPath = true;
                        }
                        else
                        {
                            arc.End.CheckExitPath(ref iRecursiveDepth);
                        }
                    }

                    arc.CheckingForExitPath = false;
                }
            }
        }
#endif

        internal void CheckLeftRecursion(out bool fReachedEndState)
        {
            fReachedEndState = false;
            if ((int)(_recurseFlag & RecurFlag.RF_IN_LEFT_RECUR_CHECK) != 0)
            {
                XmlParser.ThrowSrgsException(SRID.CircularRuleRef, _rule != null ? _rule._rule.Name : string.Empty);
            }
            else
            {
                if ((_recurseFlag & RecurFlag.RF_CHECKED_LEFT_RECURSION) == 0)
                {
                    _recurseFlag |= RecurFlag.RF_CHECKED_LEFT_RECURSION | RecurFlag.RF_IN_LEFT_RECUR_CHECK;
                    foreach (Arc arc in _outArcs)
                    {
                        bool fRuleReachedEndState = false;                  // Does the rule ref have epsilon path to the end?

                        // Traverse any rule refs to check for circular rule reference.
                        if (arc.RuleRef != null && arc.RuleRef._firstState != null)
                        {
                            State pRuleFirstNode = arc.RuleRef._firstState;

                            if (((int)(pRuleFirstNode._recurseFlag & RecurFlag.RF_IN_LEFT_RECUR_CHECK) != 0) ||   // Circular RuleRef
                                ((int)(pRuleFirstNode._recurseFlag & RecurFlag.RF_CHECKED_LEFT_RECURSION) == 0))  // Untraversed rule
                            {
                                pRuleFirstNode.CheckLeftRecursion(out fRuleReachedEndState);
                            }
                            else
                            {
                                fRuleReachedEndState = arc.RuleRef._fIsEpsilonRule;
                            }
                        }

                        // Can transition be traversed by epsilon?
                        if (fRuleReachedEndState || ((arc.RuleRef == null) && (arc.WordId == 0) && arc.WordId == 0))
                        {
                            if (arc.End != null)
                            {
                                arc.End.CheckLeftRecursion(out fReachedEndState);
                            }
                            else
                            {
                                fReachedEndState = true;
                            }
                        }
                    }

                    _recurseFlag &= (~RecurFlag.RF_IN_LEFT_RECUR_CHECK);
                    if ((_rule._firstState == this) && fReachedEndState)
                    {
                        _rule._fIsEpsilonRule = true;
                    }
                }
            }
        }

        #endregion

        #region Internal Properties

        internal int NumArcs
        {
            get
            {
                // if the number of tags > 1 extra epsilon state needs to be inserted
                int cExtra = 0;
                foreach (Arc arc in _outArcs)
                {
                    if (arc.SemanticTagCount > 0)
                    {
                        cExtra += arc.SemanticTagCount - 1;
                    }
                }

                int cArcs = 0;
                foreach (Arc arc in _outArcs)
                {
                    cArcs++;
                }
                return cArcs + cExtra;
            }
        }

        internal int NumSemanticTags
        {
            get
            {
                int c = 0;

                foreach (Arc arc in _outArcs)
                {
                    c += arc.SemanticTagCount;
                }

                return c;
            }
        }

        internal Rule Rule
        {
            get
            {
                return _rule;
            }
        }

        internal uint Id
        {
            get
            {
                return _id;
            }
        }

        internal ArcList OutArcs
        {
            get
            {
                return _outArcs;
            }
        }

        internal ArcList InArcs
        {
            get
            {
                return _inArcs;
            }
        }

        internal int SerializeId
        {
            get
            {
                return _iSerialize;
            }
            set
            {
                _iSerialize = value;
            }
        }

        #endregion

        #region private Methods

        // Sort based on rule first, so all states, and arcs for a rule end up together.
        // Then sort on index.
        private static int Compare(State state1, State state2)
        {
            if (state1._rule._cfgRule._nameOffset != state2._rule._cfgRule._nameOffset)
            {
                return state1._rule._cfgRule._nameOffset - state2._rule._cfgRule._nameOffset;
            }
            else
            {
                // First state of a rule needs to be in front.
                int isNode1FirstNode = (state1._rule._firstState == state1) ? -1 : 0;
                int isNode2FirstNode = (state2._rule._firstState == state2) ? -1 : 0;

                if (isNode1FirstNode != isNode2FirstNode)
                {
                    return isNode1FirstNode - isNode2FirstNode;
                }
                else
                {
                    // First returns null on empty collections
                    Arc arc1 = state1._outArcs != null && !state1._outArcs.IsEmpty ? state1._outArcs.First : null;
                    Arc arc2 = state2._outArcs != null && !state2._outArcs.IsEmpty ? state2._outArcs.First : null;

                    int diff = (arc1 != null ? (arc1.RuleRef != null ? 0x1000000 : 0) + arc1.WordId : state1._iSerialize) - (arc2 != null ? (arc2.RuleRef != null ? 0x1000000 : 0) + arc2.WordId : state2._iSerialize);

                    diff = diff != 0 ? diff : state1._iSerialize - state2._iSerialize;
                    //System.Diagnostics.Debug.Assert (diff != 0);
                    return diff;
                }
            }
        }

#if DEBUG

        public override string ToString()
        {
            StringBuilder sb = new("[#");
            sb.Append(_id.ToString(CultureInfo.InvariantCulture));
            if (_rule != null && _rule._firstState == this)
            {
                sb.Append(' ');
                sb.Append(_rule.Name);
            }
            sb.Append("] ");
            if (_inArcs != null)
            {
                bool first = true;
                foreach (Arc arc in _inArcs)
                {
                    if (!first)
                    {
                        sb.Append("\x20\x25cf\x20");
                    }
                    sb.Append('#');
                    sb.Append(arc.Start != null ? arc.Start._id.ToString(CultureInfo.InvariantCulture) : "S");
                    sb.Append(' ');
                    sb.Append(arc.DebuggerDisplayTags());
                    first = false;
                }
            }
            sb.Append(" <--> ");
            if (_outArcs != null)
            {
                bool first = true;
                foreach (Arc arc in _outArcs)
                {
                    if (!first)
                    {
                        sb.Append("\x20\x25cf\x20");
                    }
                    sb.Append('#');
                    sb.Append(arc.End != null ? arc.End._id.ToString(CultureInfo.InvariantCulture) : "E");
                    sb.Append(' ');
                    sb.Append(arc.DebuggerDisplayTags());
                    first = false;
                }
            }

            return sb.ToString();
        }
#endif

        #endregion

        #region internal Fields

#pragma warning disable 56524 // Arclist does not hold on any resources

        // Collection of transitions leaving this state
        private ArcList _outArcs = new();

        // Collection of transitions entering this state
        private ArcList _inArcs = new();

#pragma warning restore 56524 // Arclist does not hold on any resources

        // Index of the first arc in the state. Also used as the state handle in SR engine interfaces.
        private int _iSerialize;

        private uint _id;

        private Rule _rule;

        private State _next;
        private State _prev;

        // Flags used for recursive validation methods
        internal enum RecurFlag : uint
        {
            RF_CHECKED_EPSILON = (1 << 0),
            RF_CHECKED_EXIT_PATH = (1 << 1),
            RF_CHECKED_LEFT_RECURSION = (1 << 2),
            RF_IN_LEFT_RECUR_CHECK = (1 << 3)
        };

        // Flags used by recursive algorithms
        private RecurFlag _recurseFlag;

        #endregion
    }
}
