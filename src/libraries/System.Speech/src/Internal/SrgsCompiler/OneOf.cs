// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    internal class OneOf : ParseElementCollection, IOneOf
    {
        #region Constructors

        /// <summary>
        /// Process the 'one-of' element.
        /// </summary>
        public OneOf(Rule rule, Backend backend)
            : base(backend, rule)
        {
            // Create a start and end start.
            _startState = _backend.CreateNewState(rule);
            _endState = _backend.CreateNewState(rule);

            //Add before the start state an epsilon arc
            _startArc = _backend.EpsilonTransition(1.0f);
            _startArc.End = _startState;

            //Add after the end state an epsilon arc
            _endArc = _backend.EpsilonTransition(1.0f);
            _endArc.Start = _endState;
        }

        #endregion

        #region Internal Method

        /// <summary>
        /// Process the '/one-of' element.
        /// Connects all the arcs into an exit end point.
        ///
        /// Verify OneOf contains at least one child 'item'.
        /// </summary>
        void IElement.PostParse(IElement parentElement)
        {
            if (_startArc.End.OutArcs.IsEmpty)
            {
                XmlParser.ThrowSrgsException(SRID.EmptyOneOf);
            }

            // Remove the extraneous arc and state if possible at the start and end
            _startArc = TrimStart(_startArc, _backend);
            _endArc = TrimEnd(_endArc, _backend);

            // Connect the one-of to the parent
            base.PostParse((ParseElementCollection)parentElement);
        }

        #endregion

        #region Protected Method

        /// <summary>
        /// Adds a new arc to the one-of
        /// </summary>
        internal override void AddArc(Arc start, Arc end)
        {
            start = TrimStart(start, _backend);
            end = TrimEnd(end, _backend);

            State endStartState = end.Start;
            State startEndState = start.End;

            // Connect the previous arc with the 'start' set the insertion point
            if (start.IsEpsilonTransition && start.IsPropertylessTransition && startEndState != null && startEndState.InArcs.IsEmpty)
            {
                System.Diagnostics.Debug.Assert(start.End == startEndState);
                start.End = null;
                _backend.MoveOutputTransitionsAndDeleteState(startEndState, _startState);
            }
            else
            {
                start.Start = _startState;
            }

            // Connect with the epsilon transition at the end
            if (end.IsEpsilonTransition && end.IsPropertylessTransition && endStartState != null && endStartState.OutArcs.IsEmpty)
            {
                System.Diagnostics.Debug.Assert(end.Start == endStartState);
                end.Start = null;
                _backend.MoveInputTransitionsAndDeleteState(endStartState, _endState);
            }
            else
            {
                end.End = _endState;
            }
        }

        #endregion

        #region Protected Method

        private State _startState;
        private State _endState;

        #endregion
    }
}
