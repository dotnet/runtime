// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents the exploration of a symbolic regex as a symbolic NFA</summary>
    internal sealed class SymbolicNFA<S> where S : notnull
    {
        private readonly IBooleanAlgebra<S> _solver;
        private readonly Transition[] _transitionFunction;
        private readonly SymbolicRegexNode<S>[] _finalCondition;
        private readonly HashSet<int> _unexplored;
        private readonly SymbolicRegexNode<S>[] _nodes;

        private const int DeadendState = -1;
        private const int UnexploredState = -2;

        /// <summary>If true then some states have not been explored</summary>
        public bool IsIncomplete => _unexplored.Count > 0;

        private SymbolicNFA(IBooleanAlgebra<S> solver, Transition[] transitionFunction, HashSet<int> unexplored, SymbolicRegexNode<S>[] nodes)
        {
            Debug.Assert(transitionFunction.Length > 0 && nodes.Length == transitionFunction.Length);
            _solver = solver;
            _transitionFunction = transitionFunction;
            _finalCondition = new SymbolicRegexNode<S>[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                _finalCondition[i] = nodes[i].ExtractNullabilityTest();
            }
            _unexplored = unexplored;
            _nodes = nodes;
        }

        /// <summary>Total number of states, 0 is the initial state, states are numbered from 0 to StateCount-1</summary>
        public int StateCount => _transitionFunction.Length;

        /// <summary>If true then the state has not been explored</summary>
        public bool IsUnexplored(int state) => _transitionFunction[state]._leaf == UnexploredState;

        /// <summary>If true then the state has no outgoing transitions</summary>
        public bool IsDeadend(int state) => _transitionFunction[state]._leaf == DeadendState;

        /// <summary>If true then the state involves lazy loops or has no loops</summary>
        public bool IsLazy(int state) => _nodes[state].IsLazy;

        /// <summary>Returns true if the state is nullable in the given context</summary>
        public bool IsFinal(int state, uint context) => _finalCondition[state].IsNullableFor(context);

        /// <summary>Returns true if the state is nullable for some context</summary>
        public bool CanBeNullable(int state) => _finalCondition[state].CanBeNullable;

        /// <summary>Returns true if the state is nullable for all contexts</summary>
        public bool IsNullable(int state) => _finalCondition[state].IsNullable;

        /// <summary>Gets the underlying node of the state</summary>
        public SymbolicRegexNode<S> GetNode(int state) => _nodes[state];

        /// <summary>Enumerates all target states from the given source state</summary>
        /// <param name="sourceState">must be a an integer between 0 and StateCount-1</param>
        /// <param name="input">must be a value that acts as a minterm for the transitions emanating from the source state</param>
        /// <param name="context">reflects the immediate surrounding of the input and is used to determine nullability of anchors</param>
        public IEnumerable<int> EnumerateTargetStates(int sourceState, S input, uint context)
        {
            Debug.Assert(sourceState >= 0 && sourceState < _transitionFunction.Length);

            // First operate in a mode assuming no Union happens by finding the target leaf state if one exists
            Transition transition = _transitionFunction[sourceState];
            while (transition._kind != TransitionRegexKind.Union)
            {
                switch (transition._kind)
                {
                    case TransitionRegexKind.Leaf:
                        // deadend and unexplored are negative
                        if (transition._leaf >= 0)
                        {
                            Debug.Assert(transition._leaf < _transitionFunction.Length);
                            yield return transition._leaf;
                        }
                        // The single target (or no target) state was found, so exit the whole enumeration
                        yield break;

                    case TransitionRegexKind.Conditional:
                        Debug.Assert(transition._test is not null && transition._first is not null && transition._second is not null);
                        // Branch according to the input condition in relation to the test condition
                        if (_solver.IsSatisfiable(_solver.And(input, transition._test)))
                        {
                            // in a conditional transition input must be exclusive
                            Debug.Assert(!_solver.IsSatisfiable(_solver.And(input, _solver.Not(transition._test))));
                            transition = transition._first;
                        }
                        else
                        {
                            transition = transition._second;
                        }
                        break;

                    default:
                        Debug.Assert(transition._kind == TransitionRegexKind.Lookaround && transition._look is not null && transition._first is not null && transition._second is not null);
                        // Branch according to nullability of the lookaround condition in the given context
                        transition = transition._look.IsNullableFor(context) ?
                            transition._first :
                            transition._second;
                        break;
                }
            }

            // Continue operating in a mode where several target states can be yielded
            Debug.Assert(transition._first is not null && transition._second is not null);
            Stack<Transition> todo = new();
            todo.Push(transition._second);
            todo.Push(transition._first);
            while (todo.TryPop(out _))
            {
                switch (transition._kind)
                {
                    case TransitionRegexKind.Leaf:
                        // dead-end
                        if (transition._leaf >= 0)
                        {
                            Debug.Assert(transition._leaf < _transitionFunction.Length);
                            yield return transition._leaf;
                        }
                        break;

                    case TransitionRegexKind.Conditional:
                        Debug.Assert(transition._test is not null && transition._first is not null && transition._second is not null);
                        // Branch according to the input condition in relation to the test condition
                        if (_solver.IsSatisfiable(_solver.And(input, transition._test)))
                        {
                            // in a conditional transition input must be exclusive
                            Debug.Assert(!_solver.IsSatisfiable(_solver.And(input, _solver.Not(transition._test))));
                            todo.Push(transition._first);
                        }
                        else
                        {
                            todo.Push(transition._second);
                        }
                        break;

                    case TransitionRegexKind.Lookaround:
                        Debug.Assert(transition._look is not null && transition._first is not null && transition._second is not null);
                        // Branch according to nullability of the lookaround condition in the given context
                         todo.Push(transition._look.IsNullableFor(context) ? transition._first : transition._second);
                        break;

                    default:
                        Debug.Assert(transition._kind == TransitionRegexKind.Union && transition._first is not null && transition._second is not null);
                        todo.Push(transition._second);
                        todo.Push(transition._first);
                        break;
                }
            }
        }

        public IEnumerable<(S, SymbolicRegexNode<S>?, int)> EnumeratePaths(int sourceState) =>
            _transitionFunction[sourceState].EnumeratePaths(_solver, _solver.True);

        /// <summary>
        /// TODO: Explore an unexplored state on transition further.
        /// </summary>
        public static void ExploreState(int state) => new NotImplementedException();

        public static SymbolicNFA<S> Explore(SymbolicRegexNode<S> root, int bound)
        {
            (Dictionary<TransitionRegex<S>, Transition> cache,
             Dictionary<SymbolicRegexNode<S>, int> statemap,
             List<SymbolicRegexNode<S>> nodes,
             Stack<int> front) workState = (new(), new(), new(), new());

            workState.nodes.Add(root);
            workState.statemap[root] = 0;
            workState.front.Push(0);

            Dictionary<int, Transition> transitions = new();
            Stack<int> front = new();

            while (workState.front.Count > 0)
            {
                Debug.Assert(front.Count == 0);

                // Work Breadth-First in layers, swap front with workState.front
                Stack<int> tmp = front;
                front = workState.front;
                workState.front = tmp;

                // Process all the states in front first
                // Any new states detected in Convert are added to workState.front
                while (front.Count > 0 && (bound <= 0 || workState.nodes.Count < bound))
                {
                    int q = front.Pop();

                    // If q was on the front it must be associated with a node but not have a transition yet
                    Debug.Assert(q >= 0 && q < workState.nodes.Count &&  !transitions.ContainsKey(q));
                    transitions[q] = Convert(workState.nodes[q].CreateDerivative(), workState);
                }

                if (front.Count > 0)
                {
                    // The state bound was reached without completing the exploration so exit the loop
                    break;
                }
            }

            SymbolicRegexNode<S>[] nodes_array = workState.nodes.ToArray();

            // All states are numbered from 0 to nodes.Count-1
            Transition[] transition_array = new Transition[nodes_array.Length];
            foreach (KeyValuePair<int, SymbolicNFA<S>.Transition> entry in transitions)
            {
                transition_array[entry.Key] = entry.Value;
            }

            HashSet<int> unexplored = new(front);
            unexplored.UnionWith(workState.front);
            foreach (int q in unexplored)
            {
                transition_array[q] = Transition.s_unexplored;
            }

            // At this point no entry can be null in the transition array
            Debug.Assert(Array.TrueForAll(transition_array, tr => tr is not null));

            var nfa = new SymbolicNFA<S>(root._builder._solver, transition_array, unexplored, nodes_array);
            return nfa;
        }

        private static Transition Convert(TransitionRegex<S> tregex,
            (Dictionary<TransitionRegex<S>, Transition> cache,
             Dictionary<SymbolicRegexNode<S>, int> statemap,
             List<SymbolicRegexNode<S>> nodes,
             Stack<int> front) args)
        {
            Transition? transition;
            if (args.cache.TryGetValue(tregex, out transition))
            {
                return transition;
            }

            Stack<(TransitionRegex<S>, bool)> work = new();
            work.Push((tregex, false));

            while (work.TryPop(out (TransitionRegex<S>, bool) top))
            {
                TransitionRegex<S> tr = top.Item1;
                bool wasPushedSecondTime = top.Item2;
                if (wasPushedSecondTime)
                {
                    Debug.Assert(tr._kind != TransitionRegexKind.Leaf && tr._first is not null && tr._second is not null);
                    transition = new Transition(kind: tr._kind,
                        test: tr._test,
                        look: tr._node,
                        first: args.cache[tr._first],
                        second: args.cache[tr._second]);
                    args.cache[tr] = transition;
                }
                else
                {
                    switch (tr._kind)
                    {
                        case TransitionRegexKind.Leaf:
                            Debug.Assert(tr._node is not null);

                            if (tr._node.IsNothing)
                            {
                                args.cache[tr] = Transition.s_deadend;
                            }
                            else
                            {
                                int state;
                                if (!args.statemap.TryGetValue(tr._node, out state))
                                {
                                    state = args.nodes.Count;
                                    args.nodes.Add(tr._node);
                                    args.statemap[tr._node] = state;
                                    args.front.Push(state);
                                }
                                transition = new Transition(kind: TransitionRegexKind.Leaf, leaf: state);
                                args.cache[tr] = transition;
                            }
                            break;

                        default:
                            Debug.Assert(tr._first is not null && tr._second is not null);

                            // Push the tr for the second time
                            work.Push((tr, true));

                            // Push the branches also, unless they have been computed already
                            if (!args.cache.ContainsKey(tr._second))
                            {
                                work.Push((tr._second, false));
                            }

                            if (!args.cache.ContainsKey(tr._first))
                            {
                                work.Push((tr._first, false));
                            }

                            break;
                    }
                }
            }

            return args.cache[tregex];
        }

        /// <summary>Representation of transitions inside the parent class</summary>
        private sealed class Transition
        {
            public readonly TransitionRegexKind _kind;
            public readonly int _leaf;
            public readonly S? _test;
            public readonly SymbolicRegexNode<S>? _look;
            public readonly Transition? _first;
            public readonly Transition? _second;

            public static readonly Transition s_deadend = new Transition(TransitionRegexKind.Leaf, leaf: DeadendState);
            public static readonly Transition s_unexplored = new Transition(TransitionRegexKind.Leaf, leaf: UnexploredState);

            internal Transition(TransitionRegexKind kind, int leaf = 0, S? test = default(S), SymbolicRegexNode<S>? look = null, Transition? first = null, Transition? second = null)
            {
                _kind = kind;
                _leaf = leaf;
                _test = test;
                _look = look;
                _first = first;
                _second = second;
            }

            /// <summary>Enumerates all the paths in this transition excluding paths to dead-ends (and unexplored states if any)</summary>
            internal IEnumerable<(S, SymbolicRegexNode<S>?, int)> EnumeratePaths(IBooleanAlgebra<S> solver, S pathCondition)
            {
                switch (_kind)
                {
                    case TransitionRegexKind.Leaf:
                        // Omit any path that leads to a deadend or is unexplored
                        if (_leaf >= 0)
                        {
                            yield return (pathCondition, null, _leaf);
                        }
                        break;

                    case TransitionRegexKind.Union:
                        Debug.Assert(_first is not null && _second is not null);
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _first.EnumeratePaths(solver, pathCondition))
                        {
                            yield return path;
                        }
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _second.EnumeratePaths(solver, pathCondition))
                        {
                            yield return path;
                        }
                        break;

                    case TransitionRegexKind.Conditional:
                        Debug.Assert(_test is not null && _first is not null && _second is not null);
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _first.EnumeratePaths(solver, solver.And(pathCondition, _test)))
                        {
                            yield return path;
                        }
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _second.EnumeratePaths(solver, solver.And(pathCondition, solver.Not(_test))))
                        {
                            yield return path;
                        }
                        break;

                    default:
                        Debug.Assert(_kind is TransitionRegexKind.Lookaround && _look is not null && _first is not null && _second is not null);
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _first.EnumeratePaths(solver, pathCondition))
                        {
                            SymbolicRegexNode<S> nullabilityTest = path.Item2 is null ? _look : _look._builder.And(path.Item2, _look);
                            yield return (path.Item1, nullabilityTest, path.Item3);
                        }
                        foreach ((S, SymbolicRegexNode<S>?, int) path in _second.EnumeratePaths(solver, pathCondition))
                        {
                            // Complement the nullability test
                            SymbolicRegexNode<S> nullabilityTest = path.Item2 is null ? _look._builder.Not(_look) : _look._builder.And(path.Item2, _look._builder.Not(_look));
                            yield return (path.Item1, nullabilityTest, path.Item3);
                        }
                        break;
                }
            }
        }
    }
}
#endif
