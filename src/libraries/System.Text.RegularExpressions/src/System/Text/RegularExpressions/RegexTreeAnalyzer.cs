// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>Analyzes a <see cref="RegexTree"/> of <see cref="RegexNode"/>s to produce data on the tree structure, in particular in support of code generation.</summary>
    internal static class RegexTreeAnalyzer
    {
        /// <summary>Analyzes a <see cref="RegexInterpreterCode"/> to learn about the structure of the tree.</summary>
        public static AnalysisResults Analyze(RegexTree regexTree)
        {
            var results = new AnalysisResults(regexTree);
            results._complete = TryAnalyze(regexTree.Root, results, isAtomicByAncestor: true);
            return results;

            static bool TryAnalyze(RegexNode node, AnalysisResults results, bool isAtomicByAncestor)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    return false;
                }

                // Track whether we've seen any nodes with various options set.
                results._hasIgnoreCase |= (node.Options & RegexOptions.IgnoreCase) != 0;
                results._hasRightToLeft |= (node.Options & RegexOptions.RightToLeft) != 0;

                if (isAtomicByAncestor)
                {
                    // We've been told by our parent that we should be considered atomic, so add ourselves
                    // to the atomic collection.
                    results._isAtomicByAncestor.Add(node);
                }
                else
                {
                    // Certain kinds of nodes incur backtracking logic themselves: add them to the backtracking collection.
                    // We may later find that a node contains another that has backtracking; we'll add nodes based on that
                    // after examining the children.
                    switch (node.Kind)
                    {
                        case RegexNodeKind.Alternate:
                        case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.M != node.N:
                        case RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop or RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy when node.M != node.N:
                            (results._mayBacktrack ??= new HashSet<RegexNode>()).Add(node);
                            break;
                    }
                }

                // Update state for certain node types.
                bool isAtomicBySelf = false;
                switch (node.Kind)
                {
                    // Some node types add atomicity around what they wrap.  Set isAtomicBySelfOrParent to true for such nodes
                    // even if it was false upon entering the method.
                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.NegativeLookaround:
                    case RegexNodeKind.PositiveLookaround:
                        isAtomicBySelf = true;
                        break;

                    // Track any nodes that are themselves captures.
                    case RegexNodeKind.Capture:
                        results._containsCapture.Add(node);
                        break;
                }

                // Process each child.
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    RegexNode child = node.Child(i);

                    // Determine whether the child should be treated as atomic (whether anything
                    // can backtrack into it), which is influenced by whether this node (the child's
                    // parent) is considered atomic by itself or by its parent.
                    bool treatChildAsAtomic = (isAtomicByAncestor | isAtomicBySelf) && node.Kind switch
                    {
                        // If the parent is atomic, so is the child.  That's the whole purpose
                        // of the Atomic node, and lookarounds are also implicitly atomic.
                        RegexNodeKind.Atomic or RegexNodeKind.NegativeLookaround or RegexNodeKind.PositiveLookaround => true,

                        // Each branch is considered independently, so any atomicity applied to the alternation also applies
                        // to each individual branch.  This is true as well for conditionals.
                        RegexNodeKind.Alternate or RegexNodeKind.BackreferenceConditional or RegexNodeKind.ExpressionConditional => true,

                        // Captures don't impact atomicity: if the parent of a capture is atomic, the capture is also atomic.
                        RegexNodeKind.Capture => true,

                        // If the parent is a concatenation and this is the last node, any atomicity
                        // applying to the concatenation applies to this node, too.
                        RegexNodeKind.Concatenate => i == childCount - 1,

                        // For loops with a max iteration count of 1, they themselves can be considered
                        // atomic as can whatever they wrap, as they won't ever iterate more than once
                        // and thus we don't need to worry about one iteration consuming input destined
                        // for a subsequent iteration.
                        RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.N == 1 => true,

                        // For any other parent type, give up on trying to prove atomicity.
                        _ => false,
                    };

                    // Now analyze the child.
                    if (!TryAnalyze(child, results, treatChildAsAtomic))
                    {
                        return false;
                    }

                    // If the child contains captures, so too does this parent.
                    if (results._containsCapture.Contains(child))
                    {
                        results._containsCapture.Add(node);
                    }

                    // If the child might require backtracking into it, so too might the parent,
                    // unless the parent is itself considered atomic.  Here we don't consider parental
                    // atomicity, as we need to surface upwards to the parent whether any backtracking
                    // will be visible from this node to it.
                    if (!isAtomicBySelf && (results._mayBacktrack?.Contains(child) == true))
                    {
                        (results._mayBacktrack ??= new HashSet<RegexNode>()).Add(node);
                    }
                }

                // Successfully analyzed the node.
                return true;
            }
        }
    }

    /// <summary>Provides results of a tree analysis.</summary>
    internal sealed class AnalysisResults
    {
        /// <summary>Indicates whether the whole tree was successfully processed.</summary>
        /// <remarks>
        /// If it wasn't successfully processed, we have to assume the "worst", e.g. if it's
        /// false, we need to assume we didn't fully determine which nodes contain captures,
        /// and thus we need to assume they all do and not discard logic necessary to support
        /// captures.  It should be exceedingly rare that this is false.
        /// </remarks>
        internal bool _complete;

        /// <summary>Set of nodes that are considered to be atomic based on themselves or their ancestry.</summary>
        internal readonly HashSet<RegexNode> _isAtomicByAncestor = new(); // since the root is implicitly atomic, every tree will contain atomic-by-ancestor nodes
        /// <summary>Set of nodes that directly or indirectly contain capture groups.</summary>
        internal readonly HashSet<RegexNode> _containsCapture = new(); // the root is a capture, so this will always contain at least the root node
        /// <summary>Set of nodes that directly or indirectly contain backtracking constructs that aren't hidden internaly by atomic constructs.</summary>
        internal HashSet<RegexNode>? _mayBacktrack;
        /// <summary>Whether any node has <see cref="RegexOptions.IgnoreCase"/> set.</summary>
        internal bool _hasIgnoreCase;
        /// <summary>Whether any node has <see cref="RegexOptions.RightToLeft"/> set.</summary>
        internal bool _hasRightToLeft;

        /// <summary>Initializes the instance.</summary>
        /// <param name="regexTree">The code being analyzed.</param>
        internal AnalysisResults(RegexTree regexTree) => RegexTree = regexTree;

        /// <summary>Gets the code that was analyzed.</summary>
        public RegexTree RegexTree { get; }

        /// <summary>Gets whether a node is considered atomic based on its ancestry.</summary>
        /// <remarks>
        /// If the whole tree couldn't be examined, this returns false.  That could lead to additional
        /// code being output as nodes that could have been made atomic aren't, but functionally it's
        /// the safe choice.
        /// </remarks>
        public bool IsAtomicByAncestor(RegexNode node) => _isAtomicByAncestor.Contains(node);

        /// <summary>Gets whether a node directly or indirectly contains captures.</summary>
        /// <remarks>
        /// If the whole tree couldn't be examined, this returns true.  That could lead to additional
        /// code being emitted to deal with captures that can't occur, but functionally it's the
        /// safe choice.
        /// </remarks>
        public bool MayContainCapture(RegexNode node) => !_complete || _containsCapture.Contains(node);

        /// <summary>Gets whether a node is or directory or indirectly contains a backtracking construct that isn't hidden by an internal atomic construct.</summary>
        /// <remarks>
        /// In most code generation situations, we only need to know after we emit the child code whether
        /// the child may backtrack, and that we can see with 100% certainty.  This method is useful in situations
        /// where we need to predict without traversing the child at code generation time whether it may
        /// incur backtracking.  This method may have (few) false positives (return true when it could have
        /// returned false), but won't have any false negatives (return false when it should have returned true),
        /// meaning it might claim a node requires backtracking even if it doesn't, but it will always return
        /// true for any node that requires backtracking. In that vein, if the whole tree couldn't be examined,
        /// this returns true.
        /// </remarks>
        public bool MayBacktrack(RegexNode node) => !_complete || (_mayBacktrack?.Contains(node) ?? false);

        /// <summary>Gets whether a node might have <see cref="RegexOptions.IgnoreCase"/> set.</summary>
        /// <remarks>
        /// If the whole tree couldn't be examined, this returns true.  That could lead to additional
        /// code being emitted to support case-insensitivity in expressions that don't actually need
        /// it, but functionally it's the safe choice.
        /// </remarks>
        public bool HasIgnoreCase => !_complete || _hasIgnoreCase;

        /// <summary>Gets whether a node might have <see cref="RegexOptions.RightToLeft"/> set.</summary>
        /// <remarks>
        /// If the whole tree couldn't be examined, this returns true.  That could lead to additional
        /// code being emitted to support expressions that don't actually contain any RightToLeft
        /// nodes, but functionally it's the safe choice.
        /// </remarks>
        public bool HasRightToLeft => !_complete || _hasRightToLeft;
    }
}
