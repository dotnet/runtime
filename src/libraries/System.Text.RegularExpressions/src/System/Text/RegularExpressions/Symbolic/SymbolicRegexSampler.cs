// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    internal sealed class SymbolicRegexSampler<S> where S : notnull
    {
        private Random _random;
        private SymbolicRegexNode<S> _root;
        /// <summary>The used random seed</summary>
        public int RandomSeed { get; private set; }
        private BDD _asciiWordCharacters;
        private BDD _asciiNonWordCharacters; // omits all characters before ' '
        private BDD _ascii;                  // omits all characters before ' '
        private ICharAlgebra<S> _solver;

        public SymbolicRegexSampler(SymbolicRegexNode<S> root, int randomseed, bool negative)
        {
            _root = negative ? root._builder.Not(root) : root;
            // Treat 0 as no seed and instead choose a random seed randomly
            RandomSeed = randomseed == 0 ? new Random().Next() : randomseed;
            _random = new Random(RandomSeed);
            _solver = root._builder._solver;
            CharSetSolver bddSolver = CharSetSolver.Instance;
            _asciiWordCharacters = bddSolver.Or(new BDD[] {
                bddSolver.RangeConstraint('A', 'Z'),
                bddSolver.RangeConstraint('a', 'z'),
                bddSolver.CharConstraint('_'),
                bddSolver.RangeConstraint('0', '9')});
            // Visible ASCII range for input character generation
            _ascii = bddSolver.RangeConstraint('\x20', '\x7E');
            _asciiNonWordCharacters = bddSolver.And(_ascii, bddSolver.Not(_asciiWordCharacters));
        }

        /// <summary>Generates up to k random strings accepted by the regex</summary>
        public IEnumerable<string> GenerateRandomMembers(int k)
        {
            for (int i = 0; i < k; i++)
            {
                // Holds the generated input so far
                StringBuilder input_so_far = new();

                // Initially there is no previous character
                // Here one could also consider previous characters for example for \b, \B, and ^ anchors
                // and initialize input_so_far accordingly
                uint prevCharKind = CharKind.BeginningEnd;

                // This flag is set to false in the unlikely situation that generation ends up in a dead-end
                bool generationSucceeded = true;

                // Current set of states reached initially contains just the root
                List<SymbolicRegexNode<S>> states = new();
                states.Add(_root);

                // Used for end suffixes
                List<string> possible_endings = new();

                List<SymbolicRegexNode<S>> nextStates = new();

                while (true)
                {
                    Debug.Assert(states.Count > 0);

                    if (CanBeFinal(states))
                    {
                        // Unconditionally final state or end of the input due to \Z anchor for example
                        if (IsFinal(states) || IsFinal(states, CharKind.Context(prevCharKind, CharKind.BeginningEnd)))
                        {
                            possible_endings.Add("");
                        }

                        // End of line due to end-of-line anchor
                        if (IsFinal(states, CharKind.Context(prevCharKind, CharKind.Newline)))
                        {
                            possible_endings.Add("\n");
                        }

                        // Related to wordborder due to \b or \B
                        if (IsFinal(states, CharKind.Context(prevCharKind, CharKind.WordLetter)))
                        {
                            possible_endings.Add(ChooseChar(_asciiWordCharacters).ToString());
                        }

                        // Related to wordborder due to \b or \B
                        if (IsFinal(states, CharKind.Context(prevCharKind, CharKind.General)))
                        {
                            possible_endings.Add(ChooseChar(_asciiNonWordCharacters).ToString());
                        }
                    }

                    // Choose to stop here based on a coin-toss
                    if (possible_endings.Count > 0 && ChooseRandomlyTrueOrFalse())
                    {
                        //Choose some suffix that allows some anchor (if any) to be nullable
                        input_so_far.Append(Choose(possible_endings));
                        break;
                    }

                    SymbolicRegexNode<S> state = Choose(states);
                    char c = '\0';
                    uint cKind = 0;
                    // Observe that state.CreateDerivative() can be a deadend
                    List<(S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>)> paths = new(state.CreateDerivative().EnumeratePaths(_solver.True));
                    if (paths.Count > 0)
                    {
                        (S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path = Choose(paths);
                        // Consider a random path from some random state in states and
                        // select a random member of the predicate on that path
                        c = ChooseChar(ToBDD(path.Item1));

                        // Map the character back into the corresponding character constraint of the solver
                        S c_pred = _solver.CharConstraint(c);

                        // Determine the character kind of c
                        cKind = IsNewline(c_pred) ? CharKind.Newline : (IsWordchar(c_pred) ? CharKind.WordLetter : CharKind.General);

                        // Construct the combined context of previous and c kind
                        uint context = CharKind.Context(prevCharKind, cKind);

                        // Step into the next set of states
                        nextStates.AddRange(Step(states, c_pred, context));
                    }

                    // In the case that there are no next states: stop here
                    if (nextStates.Count == 0)
                    {
                        if (possible_endings.Count > 0)
                        {
                            input_so_far.Append(Choose(possible_endings));
                        }
                        else
                        {
                            // Ending up here is unlikely but possible for example for infeasible patterns such as @"no\bway"
                            // or due to poor choice of c -- no anchor is enabled -- so this is a deadend
                            generationSucceeded = false;
                        }
                        break;
                    }

                    input_so_far.Append(c);
                    states.Clear();
                    possible_endings.Clear();
                    List<SymbolicRegexNode<S>> tmp = states;
                    states = nextStates;
                    nextStates = tmp;
                    prevCharKind = cKind;
                }

                if (generationSucceeded)
                {
                    yield return input_so_far.ToString();
                }
            }
        }

        private static IEnumerable<SymbolicRegexNode<S>> Step(List<SymbolicRegexNode<S>> states, S pred, uint context)
        {
            HashSet<SymbolicRegexNode<S>> seen = new();
            foreach (SymbolicRegexNode<S> state in states)
            {
                foreach ((S, SymbolicRegexNode<S>?, SymbolicRegexNode<S>) path in state.CreateDerivative().EnumeratePaths(pred))
                {
                    // Either there are no anchors or else check that the anchors are nullable in the given context
                    if (path.Item2 is null || path.Item2.IsNullableFor(context))
                    {
                        // Omit repetitions from the enumeration
                        if (seen.Add(path.Item3))
                        {
                            yield return path.Item3;
                        }
                    }
                }
            }
        }

        private BDD ToBDD(S pred) => _solver.ConvertToCharSet(pred);

        private T Choose<T>(IList<T> elems) => elems[_random.Next(elems.Count)];

        private char ChooseChar((uint, uint) pair) => (char)_random.Next((int)pair.Item1, (int)pair.Item2 + 1);

        private char ChooseChar(BDD bdd)
        {
            Debug.Assert(!bdd.IsEmpty);
            // Select characters from the visible ASCII range whenever possible
            BDD bdd1 = CharSetSolver.Instance.And(bdd, _ascii);
            return ChooseChar(Choose(CharSetSolver.ToRanges(bdd1.IsEmpty ? bdd : bdd1)));
        }

        private bool ChooseRandomlyTrueOrFalse() => _random.Next(100) < 50;
        /// <summary>Returns true if some state is unconditionally final</summary>

        private static bool IsFinal(List<SymbolicRegexNode<S>> states)
        {
            foreach (SymbolicRegexNode<S> state in states)
            {
                if (state.IsNullable)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns true if some state is final in the given context</summary>
        private static bool IsFinal(List<SymbolicRegexNode<S>> states, uint context)
        {
            foreach (SymbolicRegexNode<S> state in states)
            {
                if (state.IsNullableFor(context))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns true if some state can be final</summary>
        private static bool CanBeFinal(List<SymbolicRegexNode<S>> states)
        {
            foreach (SymbolicRegexNode<S> state in states)
            {
                if (state.CanBeNullable)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsWordchar(S pred) => _solver.IsSatisfiable(_solver.And(pred, _root._builder._wordLetterPredicateForAnchors));

        private bool IsNewline(S pred) => _solver.IsSatisfiable(_solver.And(pred, _root._builder._newLinePredicate));
    }
}
#endif
