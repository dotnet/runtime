// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions.Symbolic.Unicode;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary><see cref="RegexRunnerFactory"/> for symbolic regexes.</summary>
    internal sealed class SymbolicRegexRunnerFactory : RegexRunnerFactory
    {
        /// <summary>The unicode component, including the BDD algebra.</summary>
        internal static readonly UnicodeCategoryTheory<BDD> s_unicode = new UnicodeCategoryTheory<BDD>(new CharSetSolver());

        /// <summary>The matching engine, for 64 or fewer minterms. A SymbolicRegexMatcher of ulong or VB</summary>
        internal readonly ISymbolicRegexMatcher _matcher;

        /// <summary>Initializes the factory.</summary>
        public SymbolicRegexRunnerFactory(RegexCode code, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
        {
            // RightToLeft and ECMAScript are currently not supported in conjunction with NonBacktracking.
            if ((options & (RegexOptions.RightToLeft | RegexOptions.ECMAScript)) != 0)
            {
                throw new NotSupportedException(
                    SR.Format(SR.NotSupported_NonBacktrackingConflictingOption,
                        (options & RegexOptions.RightToLeft) != 0 ? nameof(RegexOptions.RightToLeft) : nameof(RegexOptions.ECMAScript)));
            }

            var converter = new RegexNodeToSymbolicConverter(s_unicode, culture);
            var solver = (CharSetSolver)s_unicode._solver;
            SymbolicRegexNode<BDD> root = converter.Convert(code.Tree.Root, topLevel: true);

            BDD[] minterms = root.ComputeMinterms();
            if (minterms.Length > 64)
            {
                // Use BV to represent a predicate
                var algBV = new BVAlgebra(solver, minterms);
                var builderBV = new SymbolicRegexBuilder<BV>(algBV)
                {
                    // The default constructor sets the following predicates to False; this update happens after the fact.
                    // It depends on whether anchors where used in the regex whether the predicates are actually different from False.
                    _wordLetterPredicateForAnchors = algBV.ConvertFromCharSet(solver, converter._builder._wordLetterPredicateForAnchors),
                    _newLinePredicate = algBV.ConvertFromCharSet(solver, converter._builder._newLinePredicate)
                };

                // Convert the BDD-based AST to BV-based AST
                SymbolicRegexNode<BV> rootBV = converter._builder.Transform(root, builderBV, bdd => builderBV._solver.ConvertFromCharSet(solver, bdd));
                _matcher = new SymbolicRegexMatcher<BV>(rootBV, code, solver, minterms, matchTimeout, culture);
            }
            else
            {
                // Use ulong to represent a predicate
                var alg64 = new BV64Algebra(solver, minterms);
                var builder64 = new SymbolicRegexBuilder<ulong>(alg64)
                {
                    // The default constructor sets the following predicates to False, this update happens after the fact
                    // It depends on whether anchors where used in the regex whether the predicates are actually different from False
                    _wordLetterPredicateForAnchors = alg64.ConvertFromCharSet(solver, converter._builder._wordLetterPredicateForAnchors),
                    _newLinePredicate = alg64.ConvertFromCharSet(solver, converter._builder._newLinePredicate)
                };

                // Convert the BDD-based AST to ulong-based AST
                SymbolicRegexNode<ulong> root64 = converter._builder.Transform(root, builder64, bdd => builder64._solver.ConvertFromCharSet(solver, bdd));
                _matcher = new SymbolicRegexMatcher<ulong>(root64, code, solver, minterms, matchTimeout, culture);
            }
        }

        /// <summary>Creates a <see cref="RegexRunner"/> object.</summary>
        protected internal override RegexRunner CreateInstance() => _matcher is SymbolicRegexMatcher<ulong> srmUInt64 ?
            new Runner<ulong>(srmUInt64) :
            new Runner<BV>((SymbolicRegexMatcher<BV>)_matcher);

        /// <summary>Runner type produced by this factory.</summary>
        /// <remarks>
        /// The wrapped <see cref="ISymbolicRegexMatcher"/> is itself thread-safe and can be shared across
        /// all runner instances, but the runner itself has state (e.g. for captures, positions, etc.)
        /// and must not be shared between concurrent uses.
        /// </remarks>
        private sealed class Runner<TSetType> : RegexRunner where TSetType : notnull
        {
            /// <summary>The matching engine.</summary>
            private readonly SymbolicRegexMatcher<TSetType> _matcher;

            internal Runner(SymbolicRegexMatcher<TSetType> matcher) => _matcher = matcher;

            protected override void InitTrackCount() { } // nop, no backtracking

            protected override bool FindFirstChar() => true; // The logic is all in Go.

            protected override void Go()
            {
                // Perform the match.
                SymbolicMatch pos = _matcher.FindMatch(quick, runtext!, runtextpos, runtextend);
                if (pos.Success)
                {
                    // If we successfully matched, capture the match, and then jump the current position to the end of the match.
                    int start = pos.Index;
                    int end = start + pos.Length;
                    Capture(0, start, end);
                    runtextpos = end;
                }
                else
                {
                    // If we failed to find a match in the entire remainder of the input, skip the current position to the end.
                    // The calling scan loop will then exit.
                    runtextpos = runtextend;
                }
            }
        }
    }
}
