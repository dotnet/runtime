// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal abstract class ParseElementCollection : ParseElement
    {
        protected ParseElementCollection(Backend backend, Rule rule)
            : base(rule)
        {
            _backend = backend;
        }

        /// <summary>
        /// Attach a semantic tag to word. If the word is a rule ref then an
        /// epsilon transition must be created
        /// </summary>
        internal void AddSemanticInterpretationTag(CfgGrammar.CfgProperty propertyInfo)
        {
            // If the word is a rule ref, an epsilon transition must be created
            if (_endArc != null && _endArc.RuleRef != null)
            {
                Arc tagTransition = _backend.EpsilonTransition(1.0f);
                _backend.AddSemanticInterpretationTag(tagTransition, propertyInfo);

                // Create a new state
                State state = _backend.CreateNewState(_rule);

                // Connect the new state with the end arc
                tagTransition.Start = state;
                _endArc.End = state;
                _endArc = tagTransition;
            }
            else
            {
                _startArc ??= _endArc = _backend.EpsilonTransition(1.0f);
                _backend.AddSemanticInterpretationTag(_endArc, propertyInfo);
            }
        }

        // must add the rule Id
        // _propInfo._ulId = (uint) ((ParseElement) parent).StartState._rule._iSerialize2;
        internal void AddSementicPropertyTag(CfgGrammar.CfgProperty propertyInfo)
        {
            _startArc ??= _endArc = _backend.EpsilonTransition(1.0f);
            _backend.AddPropertyTag(_startArc, _endArc, propertyInfo);
        }

        /// <summary>
        /// Insert an epsilon state either before or after the current arc
        /// </summary>
        protected Arc InsertState(Arc arc, float weight, Position position)
        {
            // If the arc is a epsilon, creating a new epsilon arc might not be needed
            if (arc.IsEpsilonTransition)
            {
                if (position == Position.Before && arc.End != null && arc.End.InArcs.CountIsOne && Graph.MoveSemanticTagRight(arc))
                {
                    return arc;
                }
                if (position == Position.After && arc.Start != null && arc.Start.OutArcs.CountIsOne && Graph.MoveSemanticTagLeft(arc))
                {
                    return arc;
                }
            }

            // Create an epsilon transition
            Arc epsilon = _backend.EpsilonTransition(weight);

            // Insert a state
            State insertionState = _backend.CreateNewState(_rule);

            if (position == Position.Before)
            {
                epsilon.End = insertionState;
                arc.Start = insertionState;
            }
            else
            {
                arc.End = insertionState;
                epsilon.Start = insertionState;
            }
            return epsilon;
        }

        /// <summary>
        /// Remove all the epsilon transitions at the beginning of a sub graph
        /// </summary>
        protected static Arc TrimStart(Arc start, Backend backend)
        {
            Arc startArc = start;

            if (start.End != null)
            {
                // Remove the added startState if possible, check done by MoveSemanticTagRight
                for (State startState = startArc.End; startArc.IsEpsilonTransition && startState != null && Graph.MoveSemanticTagRight(startArc) && startState.InArcs.CountIsOne && startState.OutArcs.CountIsOne; startState = startArc.End)
                {
                    // State has a single input epsilon transition
                    // Delete the input epsilon transition and delete state.
                    System.Diagnostics.Debug.Assert(startArc.End == startState);
                    startArc.End = null;

                    // Reset the start Arc
                    System.Diagnostics.Debug.Assert(startState.OutArcs.CountIsOne);
                    startArc = startState.OutArcs.First;
                    System.Diagnostics.Debug.Assert(startArc.Start == startState);
                    startArc.Start = null;

                    // Delete the input epsilon transition and delete state if appropriate.
                    backend.DeleteState(startState);
                }
            }
            return startArc;
        }

        /// <summary>
        /// Remove all the epsilon transition at the end
        /// </summary>
        protected static Arc TrimEnd(Arc end, Backend backend)
        {
            Arc endArc = end;

            if (endArc != null)
            {
                // Remove the end arc if possible, check done by MoveSemanticTagRight
                for (State endState = endArc.Start; endArc.IsEpsilonTransition && endState != null && Graph.MoveSemanticTagLeft(endArc) && endState.InArcs.CountIsOne && endState.OutArcs.CountIsOne; endState = endArc.Start)
                {
                    // State has a single input epsilon transition
                    // Delete the input epsilon transition and delete state.
                    System.Diagnostics.Debug.Assert(endArc.Start == endState);
                    endArc.Start = null;

                    // Reset the end Arc
                    System.Diagnostics.Debug.Assert(endState.InArcs.CountIsOne);
                    endArc = endState.InArcs.First;
                    System.Diagnostics.Debug.Assert(endArc.End == endState);
                    endArc.End = null;

                    // Delete the input epsilon transition and delete state if appropriate.
                    backend.DeleteState(endState);
                }
            }
            return endArc;
        }

        protected void PostParse(ParseElementCollection parent)
        {
            if (_startArc != null)
            {
                parent.AddArc(_startArc, _endArc);
            }
        }

        internal void AddArc(Arc arc) { AddArc(arc, arc); }

        internal enum Position
        {
            Before,
            After
        }

        /// <summary>
        /// New sets of arcs are added after the last arc
        /// </summary>
        internal virtual void AddArc(Arc start, Arc end)
        {
            State state = null;
            if (_startArc == null)
            {
                _startArc = start;
                _endArc = end;
            }
            else
            {
                bool done = false;

                // Successive <one-of> have 2 epsilon transition
                if (_endArc.IsEpsilonTransition && start.IsEpsilonTransition)
                {
                    // Trim the start tag.
                    start = TrimStart(start, _backend);

                    // If Trimming didn't create a non epsilon, try to trim the end
                    if (start.IsEpsilonTransition)
                    {
                        _endArc = TrimEnd(_endArc, _backend);

                        // start and end are still epsilon transition
                        if (_endArc.IsEpsilonTransition)
                        {
                            // we do the merging
                            State from = _endArc.Start;
                            State to = start.End;
                            done = true;

                            if (from == null)
                            {
                                // Ignore the current _start _end
                                Arc.CopyTags(_endArc, start, Direction.Right);
                                _startArc = start;
                            }
                            else if (to == null)
                            {
                                // Ignore the old _startArc _endArc
                                Arc.CopyTags(start, _endArc, Direction.Left);
                                end = _endArc;
                            }
                            else
                            {
                                // No tags, just fold the start and end state
                                if (_endArc.IsPropertylessTransition && start.IsPropertylessTransition)
                                {
                                    // Move the end arc
                                    start.End = null;
                                    _endArc.Start = null;
                                    _backend.MoveInputTransitionsAndDeleteState(from, to);
                                }
                                else
                                {
                                    // Discard the endstate and replace it with the startArc
                                    Arc.CopyTags(start, _endArc, Direction.Left);
                                    start.End = null;
                                    _endArc.End = to;
                                }
                            }
                        }
                    }
                }

                if (!done)
                {
                    // If the last arc is an epsilon value then there is no need to create a new state
                    if (_endArc.IsEpsilonTransition && Graph.CanTagsBeMoved(_endArc, start))
                    {
                        // Copy the tags from "endArc" to the "start"
                        Arc.CopyTags(_endArc, start, Direction.Right);

                        if (_endArc.Start != null)
                        {
                            // Discard the endstate and replace it with the startArc
                            state = _endArc.Start;
                            _endArc.Start = null;

                            // Connexion between the state end the start is done below
                            //state.OutArcs.Add (start);
                            //start.Start = state;
                        }
                        if (_endArc == _startArc)
                        {
                            _startArc = start;
                        }
                    }
                    else
                    {
                        // If the first arc is an epsilon value then there is no need to create a new state
                        if (start.IsEpsilonTransition && Graph.CanTagsBeMoved(start, _endArc))
                        {
                            // Copy the tags from "endArc" to the "start"
                            Arc.CopyTags(start, _endArc, Direction.Left);

                            if (start.End != null)
                            {
                                // Discard the endstate and replace it with the startArc
                                state = start.End;
                                start.End = null;
                                _endArc.End = state;
                                state = null;
                            }
                            if (start == end)
                            {
                                end = _endArc;
                            }
                        }
                        else
                        {
                            // Create a new state
                            state = _backend.CreateNewState(_rule);

                            // Connect the new state with the end arc
                            _endArc.End = state;
                        }
                    }
                    // connect the arcs
                    if (state != null)
                    {
                        start.Start = state;
                    }
                }
                _endArc = end;
            }
        }

        protected Backend _backend;
        protected Arc _startArc;
        protected Arc _endArc;
    }
}
