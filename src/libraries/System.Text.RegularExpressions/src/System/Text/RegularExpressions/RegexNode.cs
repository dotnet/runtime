// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>Represents a regex subexpression.</summary>
    internal sealed class RegexNode
    {
        /// <summary>Arbitrary number of repetitions of the same character when we'd prefer to represent that as a repeater of that character rather than a string.</summary>
        internal const int MultiVsRepeaterLimit = 64;

        /// <summary>The node's children.</summary>
        /// <remarks>null if no children, a <see cref="RegexNode"/> if one child, or a <see cref="List{RegexNode}"/> if multiple children.</remarks>
        private object? Children;

        /// <summary>The kind of expression represented by this node.</summary>
        public RegexNodeKind Kind { get; private set; }

        /// <summary>A string associated with the node.</summary>
        /// <remarks>For a <see cref="RegexNodeKind.Multi"/>, this is the string from the expression.  For an <see cref="IsSetFamily"/> node, this is the character class string from <see cref="RegexCharClass"/>.</remarks>
        public string? Str { get; private set; }

        /// <summary>The character associated with the node.</summary>
        /// <remarks>For a <see cref="IsOneFamily"/> or <see cref="IsNotoneFamily"/> node, the character from the expression.</remarks>
        public char Ch { get; private set; }

        /// <summary>The minimum number of iterations for a loop, or the capture group number for a capture or backreference.</summary>
        /// <remarks>No minimum is represented by 0. No capture group is represented by -1.</remarks>
        public int M { get; private set; }

        /// <summary>The maximum number of iterations for a loop, or the uncapture group number for a balancing group.</summary>
        /// <remarks>No upper bound is represented by <see cref="int.MaxValue"/>. No capture group is represented by -1.</remarks>
        public int N { get; private set; }

        /// <summary>The options associated with the node.</summary>
        public RegexOptions Options;

        /// <summary>The node's parent node in the tree.</summary>
        /// <remarks>
        /// During parsing, top-level nodes are also stacked onto a parse stack (a stack of trees) using <see cref="Parent"/>.
        /// After parsing, <see cref="Parent"/> is the node in the tree that has this node as or in <see cref="Children"/>.
        /// </remarks>
        public RegexNode? Parent;

        public RegexNode(RegexNodeKind kind, RegexOptions options)
        {
            Kind = kind;
            Options = options;
        }

        public RegexNode(RegexNodeKind kind, RegexOptions options, char ch)
        {
            Kind = kind;
            Options = options;
            Ch = ch;
        }

        public RegexNode(RegexNodeKind kind, RegexOptions options, string str)
        {
            Kind = kind;
            Options = options;
            Str = str;
        }

        public RegexNode(RegexNodeKind kind, RegexOptions options, int m)
        {
            Kind = kind;
            Options = options;
            M = m;
        }

        public RegexNode(RegexNodeKind kind, RegexOptions options, int m, int n)
        {
            Kind = kind;
            Options = options;
            M = m;
            N = n;
        }

        /// <summary>Creates a RegexNode representing a single character.</summary>
        /// <param name="ch">The character.</param>
        /// <param name="options">The node's options.</param>
        /// <param name="culture">The culture to use to perform any required transformations.</param>
        /// <param name="caseBehavior">The behavior to be used for case comparisons. If the value hasn't been set yet, it will get initialized in the first lookup.</param>
        /// <returns>The created RegexNode.  This might be a RegexNode.One or a RegexNode.Set.</returns>
        public static RegexNode CreateOneWithCaseConversion(char ch, RegexOptions options, CultureInfo? culture, ref RegexCaseBehavior caseBehavior)
        {
            // If the options specify case-insensitivity, we try to create a node that fully encapsulates that.
            if ((options & RegexOptions.IgnoreCase) != 0)
            {
                Debug.Assert(culture is not null);

                if (!RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(ch, culture, ref caseBehavior, out ReadOnlySpan<char> equivalences))
                {
                    // If we reach here, then we know that ch does not participate in case conversion, so we just
                    // create a One node with it and strip out the IgnoreCase option.
                    return new RegexNode(RegexNodeKind.One, options & ~RegexOptions.IgnoreCase, ch);
                }

                // If it does participate in case conversion, then transform the Node into a set with
                // all possible valid values and remove the IgnoreCase option to make the node case-sensitive.
                string stringSet = RegexCharClass.CharsToStringClass(equivalences);
                return new RegexNode(RegexNodeKind.Set, options & ~RegexOptions.IgnoreCase, stringSet);
            }

            // Create a One node for the character.
            return new RegexNode(RegexNodeKind.One, options, ch);
        }

        /// <summary>Reverses all children of a concatenation when in RightToLeft mode.</summary>
        public RegexNode ReverseConcatenationIfRightToLeft()
        {
            if ((Options & RegexOptions.RightToLeft) != 0 &&
                Kind == RegexNodeKind.Concatenate &&
                ChildCount() > 1)
            {
                ((List<RegexNode>)Children!).Reverse();
            }

            return this;
        }

        /// <summary>
        /// Pass type as OneLazy or OneLoop
        /// </summary>
        private void MakeRep(RegexNodeKind kind, int min, int max)
        {
            Kind += kind - RegexNodeKind.One;
            M = min;
            N = max;
        }

        private void MakeLoopAtomic()
        {
            switch (Kind)
            {
                case RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop:
                    // For loops, we simply change the Type to the atomic variant.
                    // Atomic greedy loops should consume as many values as they can.
                    Kind += RegexNodeKind.Oneloopatomic - RegexNodeKind.Oneloop;
                    break;

                case RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy:
                    // For lazy, we not only change the Type, we also lower the max number of iterations
                    // to the minimum number of iterations, creating a repeater, as they should end up
                    // matching as little as possible.
                    Kind += RegexNodeKind.Oneloopatomic - RegexNodeKind.Onelazy;
                    N = M;
                    if (N == 0)
                    {
                        // If moving the max to be the same as the min dropped it to 0, there's no
                        // work to be done for this node, and we can make it Empty.
                        Kind = RegexNodeKind.Empty;
                        Str = null;
                        Ch = '\0';
                    }
                    else if (Kind == RegexNodeKind.Oneloopatomic && N is >= 2 and <= MultiVsRepeaterLimit)
                    {
                        // If this is now a One repeater with a small enough length,
                        // make it a Multi instead, as they're better optimized down the line.
                        Kind = RegexNodeKind.Multi;
                        Str = new string(Ch, N);
                        Ch = '\0';
                        M = N = 0;
                    }
                    break;

                default:
                    Debug.Fail($"Unexpected type: {Kind}");
                    break;
            }
        }

#if DEBUG
        /// <summary>Validate invariants the rest of the implementation relies on for processing fully-built trees.</summary>
        [Conditional("DEBUG")]
        private void ValidateFinalTreeInvariants()
        {
            Debug.Assert(Kind == RegexNodeKind.Capture, "Every generated tree should begin with a capture node");

            var toExamine = new Stack<RegexNode>();
            toExamine.Push(this);
            while (toExamine.Count > 0)
            {
                RegexNode node = toExamine.Pop();

                // Add all children to be examined
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    RegexNode child = node.Child(i);
                    Debug.Assert(child.Parent == node, $"{child.Describe()} missing reference to parent {node.Describe()}");

                    toExamine.Push(child);
                }

                // Validate that we never see certain node types.
                Debug.Assert(Kind != RegexNodeKind.Group, "All Group nodes should have been removed.");

                // Validate node types and expected child counts.
                switch (node.Kind)
                {
                    case RegexNodeKind.Group:
                        Debug.Fail("All Group nodes should have been removed.");
                        break;

                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.Empty:
                    case RegexNodeKind.End:
                    case RegexNodeKind.EndZ:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.Multi:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.NonECMABoundary:
                    case RegexNodeKind.Nothing:
                    case RegexNodeKind.Notone:
                    case RegexNodeKind.Notonelazy:
                    case RegexNodeKind.Notoneloop:
                    case RegexNodeKind.Notoneloopatomic:
                    case RegexNodeKind.One:
                    case RegexNodeKind.Onelazy:
                    case RegexNodeKind.Oneloop:
                    case RegexNodeKind.Oneloopatomic:
                    case RegexNodeKind.Backreference:
                    case RegexNodeKind.Set:
                    case RegexNodeKind.Setlazy:
                    case RegexNodeKind.Setloop:
                    case RegexNodeKind.Setloopatomic:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.UpdateBumpalong:
                        Debug.Assert(childCount == 0, $"Expected zero children for {node.Kind}, got {childCount}.");
                        break;

                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.Capture:
                    case RegexNodeKind.Lazyloop:
                    case RegexNodeKind.Loop:
                    case RegexNodeKind.NegativeLookaround:
                    case RegexNodeKind.PositiveLookaround:
                        Debug.Assert(childCount == 1, $"Expected one and only one child for {node.Kind}, got {childCount}.");
                        break;

                    case RegexNodeKind.BackreferenceConditional:
                        Debug.Assert(childCount == 2, $"Expected two children for {node.Kind}, got {childCount}");
                        break;

                    case RegexNodeKind.ExpressionConditional:
                        Debug.Assert(childCount == 3, $"Expected three children for {node.Kind}, got {childCount}");
                        break;

                    case RegexNodeKind.Concatenate:
                    case RegexNodeKind.Alternate:
                        Debug.Assert(childCount >= 2, $"Expected at least two children for {node.Kind}, got {childCount}.");
                        break;

                    default:
                        Debug.Fail($"Unexpected node type: {node.Kind}");
                        break;
                }

                // Validate node configuration.
                switch (node.Kind)
                {
                    case RegexNodeKind.Multi:
                        Debug.Assert(node.Str is not null, "Expect non-null multi string");
                        Debug.Assert(node.Str.Length >= 2, $"Expected {node.Str} to be at least two characters");
                        break;

                    case RegexNodeKind.Set:
                    case RegexNodeKind.Setloop:
                    case RegexNodeKind.Setloopatomic:
                    case RegexNodeKind.Setlazy:
                        Debug.Assert(!string.IsNullOrEmpty(node.Str), $"Expected non-null, non-empty string for {node.Kind}.");
                        break;

                    default:
                        Debug.Assert(node.Str is null, $"Expected null string for {node.Kind}, got \"{node.Str}\".");
                        break;
                }

                // Validate only Backreference nodes have IgnoreCase Option
                switch (node.Kind)
                {
                    case RegexNodeKind.Backreference:
                        break;

                    default:
                        Debug.Assert((node.Options & RegexOptions.IgnoreCase) == 0, $"{node.Kind} node should not have RegexOptions.IgnoreCase");
                        break;
                }
            }
        }
#endif

        /// <summary>Performs additional optimizations on an entire tree prior to being used.</summary>
        /// <remarks>
        /// Some optimizations are performed by the parser while parsing, and others are performed
        /// as nodes are being added to the tree.  The optimizations here expect the tree to be fully
        /// formed, as they inspect relationships between nodes that may not have been in place as
        /// individual nodes were being processed/added to the tree.
        /// </remarks>
        internal RegexNode FinalOptimize()
        {
            RegexNode rootNode = this;
            Debug.Assert(rootNode.Kind == RegexNodeKind.Capture);
            Debug.Assert(rootNode.Parent is null);
            Debug.Assert(rootNode.ChildCount() == 1);

            // Only apply optimization when LTR to avoid needing additional code for the much rarer RTL case.
            // Also only apply these optimizations when not using NonBacktracking, as these optimizations are
            // all about avoiding things that are impactful for the backtracking engines but nops for non-backtracking.
            if ((Options & (RegexOptions.RightToLeft | RegexOptions.NonBacktracking)) == 0)
            {
                // Optimization: eliminate backtracking for loops.
                // For any single-character loop (Oneloop, Notoneloop, Setloop), see if we can automatically convert
                // that into its atomic counterpart (Oneloopatomic, Notoneloopatomic, Setloopatomic) based on what
                // comes after it in the expression tree.
                rootNode.FindAndMakeLoopsAtomic();

                // Optimization: backtracking removal at expression end.
                // If we find backtracking construct at the end of the regex, we can instead make it non-backtracking,
                // since nothing would ever backtrack into it anyway.  Doing this then makes the construct available
                // to implementations that don't support backtracking.
                rootNode.EliminateEndingBacktracking();

                // Optimization: unnecessary re-processing of starting loops.
                // If an expression is guaranteed to begin with a single-character unbounded loop that isn't part of an alternation (in which case it
                // wouldn't be guaranteed to be at the beginning) or a capture (in which case a back reference could be influenced by its length), then we
                // can update the tree with a temporary node to indicate that the implementation should use that node's ending position in the input text
                // as the next starting position at which to start the next match. This avoids redoing matches we've already performed, e.g. matching
                // "\w+@dot.net" against "is this a valid address@dot.net", the \w+ will initially match the "is" and then will fail to match the "@".
                // Rather than bumping the scan loop by 1 and trying again to match at the "s", we can instead start at the " ".  For functional correctness
                // we can only consider unbounded loops, as to be able to start at the end of the loop we need the loop to have consumed all possible matches;
                // otherwise, you could end up with a pattern like "a{1,3}b" matching against "aaaabc", which should match, but if we pre-emptively stop consuming
                // after the first three a's and re-start from that position, we'll end up failing the match even though it should have succeeded.  We can also
                // apply this optimization to non-atomic loops: even though backtracking could be necessary, such backtracking would be handled within the processing
                // of a single starting position.  Lazy loops similarly benefit, as a failed match will result in exploring the exact same search space as with
                // a greedy loop, just in the opposite order (and a successful match will overwrite the bumpalong position); we need to avoid atomic lazy loops,
                // however, as they will only end up as a repeater for the minimum length and thus will effectively end up with a non-infinite upper bound, which
                // we've already outlined is problematic.
                {
                    RegexNode node = rootNode.Child(0); // skip implicit root capture node
                    bool atomicByAncestry = true; // the root is implicitly atomic because nothing comes after it (same for the implicit root capture)
                    while (true)
                    {
                        switch (node.Kind)
                        {
                            case RegexNodeKind.Atomic:
                                node = node.Child(0);
                                continue;

                            case RegexNodeKind.Concatenate:
                                atomicByAncestry = false;
                                node = node.Child(0);
                                continue;

                            case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when node.N == int.MaxValue:
                            case RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy when node.N == int.MaxValue && !atomicByAncestry:
                                if (node.Parent is { Kind: RegexNodeKind.Concatenate } parent)
                                {
                                    parent.InsertChild(1, new RegexNode(RegexNodeKind.UpdateBumpalong, node.Options));
                                }
                                break;
                        }

                        break;
                    }
                }
            }

            // Done optimizing.  Return the final tree.
#if DEBUG
            rootNode.ValidateFinalTreeInvariants();
#endif
            return rootNode;
        }

        /// <summary>Converts nodes at the end of the node tree to be atomic.</summary>
        /// <remarks>
        /// The correctness of this optimization depends on nothing being able to backtrack into
        /// the provided node.  That means it must be at the root of the overall expression, or
        /// it must be an Atomic node that nothing will backtrack into by the very nature of Atomic.
        /// </remarks>
        private void EliminateEndingBacktracking()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack() ||
                (Options & (RegexOptions.RightToLeft | RegexOptions.NonBacktracking)) != 0)
            {
                // If we can't recur further, just stop optimizing.
                // We haven't done the work to validate this is correct for RTL.
                // And NonBacktracking doesn't support atomic groups and doesn't have backtracking to be eliminated.
                return;
            }

            // Walk the tree starting from the current node.
            RegexNode node = this;
            while (true)
            {
                switch (node.Kind)
                {
                    // {One/Notone/Set}loops can be upgraded to {One/Notone/Set}loopatomic nodes, e.g. [abc]* => (?>[abc]*).
                    // And {One/Notone/Set}lazys can similarly be upgraded to be atomic, which really makes them into repeaters
                    // or even empty nodes.
                    case RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop:
                    case RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy:
                        node.MakeLoopAtomic();
                        break;

                    // Just because a particular node is atomic doesn't mean all its descendants are.
                    // Process them as well. Lookarounds are implicitly atomic.
                    case RegexNodeKind.Atomic:
                    case RegexNodeKind.PositiveLookaround:
                    case RegexNodeKind.NegativeLookaround:
                        node = node.Child(0);
                        continue;

                    // For Capture and Concatenate, we just recur into their last child (only child in the case
                    // of Capture).  However, if the child is an alternation or loop, we can also make the
                    // node itself atomic by wrapping it in an Atomic node. Since we later check to see whether a
                    // node is atomic based on its parent or grandparent, we don't bother wrapping such a node in
                    // an Atomic one if its grandparent is already Atomic.
                    // e.g. [xyz](?:abc|def) => [xyz](?>abc|def)
                    case RegexNodeKind.Capture:
                    case RegexNodeKind.Concatenate:
                        RegexNode existingChild = node.Child(node.ChildCount() - 1);
                        if ((existingChild.Kind is RegexNodeKind.Alternate or RegexNodeKind.BackreferenceConditional or RegexNodeKind.ExpressionConditional or RegexNodeKind.Loop or RegexNodeKind.Lazyloop) &&
                            (node.Parent is null || node.Parent.Kind != RegexNodeKind.Atomic)) // validate grandparent isn't atomic
                        {
                            var atomic = new RegexNode(RegexNodeKind.Atomic, existingChild.Options);
                            atomic.AddChild(existingChild);
                            node.ReplaceChild(node.ChildCount() - 1, atomic);
                        }
                        node = existingChild;
                        continue;

                    // For alternate, we can recur into each branch separately.  We use this iteration for the first branch.
                    // Conditionals are just like alternations in this regard.
                    // e.g. abc*|def* => ab(?>c*)|de(?>f*)
                    case RegexNodeKind.Alternate:
                    case RegexNodeKind.BackreferenceConditional:
                    case RegexNodeKind.ExpressionConditional:
                        {
                            int branches = node.ChildCount();
                            for (int i = 1; i < branches; i++)
                            {
                                node.Child(i).EliminateEndingBacktracking();
                            }

                            if (node.Kind != RegexNodeKind.ExpressionConditional) // ReduceExpressionConditional will have already applied ending backtracking removal
                            {
                                node = node.Child(0);
                                continue;
                            }
                        }
                        break;

                    // For {Lazy}Loop, we search to see if there's a viable last expression, and iff there
                    // is we recur into processing it.  Also, as with the single-char lazy loops, LazyLoop
                    // can have its max iteration count dropped to its min iteration count, as there's no
                    // reason for it to match more than the minimal at the end; that in turn makes it a
                    // repeater, which results in better code generation.
                    // e.g. (?:abc*)* => (?:ab(?>c*))*
                    // e.g. (abc*?)+? => (ab){1}
                    case RegexNodeKind.Lazyloop:
                        node.N = node.M;
                        goto case RegexNodeKind.Loop;
                    case RegexNodeKind.Loop:
                        {
                            if (node.N == 1)
                            {
                                // If the loop has a max iteration count of 1 (e.g. it's an optional node),
                                // there's no possibility for conflict between multiple iterations, so
                                // we can process it.
                                node = node.Child(0);
                                continue;
                            }

                            RegexNode? loopDescendent = node.FindLastExpressionInLoopForAutoAtomic();
                            if (loopDescendent != null)
                            {
                                node = loopDescendent;
                                continue; // loop around to process node
                            }
                        }
                        break;
                }

                break;
            }
        }

        /// <summary>
        /// Removes redundant nodes from the subtree, and returns an optimized subtree.
        /// </summary>
        internal RegexNode Reduce()
        {
            // Remove IgnoreCase option from everything except a Backreference
            switch (Kind)
            {
                default:
                    // No effect
                    Options &= ~RegexOptions.IgnoreCase;
                    break;

                case RegexNodeKind.Backreference:
                    // Still meaningful
                    break;
            }

            return Kind switch
            {
                RegexNodeKind.Alternate => ReduceAlternation(),
                RegexNodeKind.Atomic => ReduceAtomic(),
                RegexNodeKind.Concatenate => ReduceConcatenation(),
                RegexNodeKind.Group => ReduceGroup(),
                RegexNodeKind.Loop or RegexNodeKind.Lazyloop => ReduceLoops(),
                RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround => ReduceLookaround(),
                RegexNodeKind.Set or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy => ReduceSet(),
                RegexNodeKind.ExpressionConditional => ReduceExpressionConditional(),
                RegexNodeKind.BackreferenceConditional => ReduceBackreferenceConditional(),
                _ => this,
            };
        }

        /// <summary>Remove an unnecessary Concatenation or Alternation node</summary>
        /// <remarks>
        /// Simple optimization for a concatenation or alternation:
        /// - if the node has only one child, use it instead
        /// - if the node has zero children, turn it into an empty with Nothing for an alternation or Empty for a concatenation
        /// </remarks>
        private RegexNode ReplaceNodeIfUnnecessary()
        {
            Debug.Assert(Kind is RegexNodeKind.Alternate or RegexNodeKind.Concatenate);
            return ChildCount() switch
            {
                0 => new RegexNode(Kind == RegexNodeKind.Alternate ? RegexNodeKind.Nothing : RegexNodeKind.Empty, Options),
                1 => Child(0),
                _ => this,
            };
        }

        /// <summary>Remove all non-capturing groups.</summary>
        /// <remark>
        /// Simple optimization: once parsed into a tree, non-capturing groups
        /// serve no function, so strip them out.
        /// e.g. (?:(?:(?:abc))) => abc
        /// </remark>
        private RegexNode ReduceGroup()
        {
            Debug.Assert(Kind == RegexNodeKind.Group);

            RegexNode u = this;
            while (u.Kind == RegexNodeKind.Group)
            {
                Debug.Assert(u.ChildCount() == 1);
                u = u.Child(0);
            }

            return u;
        }

        /// <summary>
        /// Remove unnecessary atomic nodes, and make appropriate descendents of the atomic node themselves atomic.
        /// </summary>
        /// <remarks>
        /// e.g. (?>(?>(?>a*))) => (?>a*)
        /// e.g. (?>(abc*)*) => (?>(abc(?>c*))*)
        /// </remarks>
        private RegexNode ReduceAtomic()
        {
            // RegexOptions.NonBacktracking doesn't support atomic groups, so when that option
            // is set we don't want to create atomic groups where they weren't explicitly authored.
            if ((Options & RegexOptions.NonBacktracking) != 0)
            {
                return this;
            }

            Debug.Assert(Kind == RegexNodeKind.Atomic);
            Debug.Assert(ChildCount() == 1);

            RegexNode atomic = this;
            RegexNode child = Child(0);
            while (child.Kind == RegexNodeKind.Atomic)
            {
                atomic = child;
                child = atomic.Child(0);
            }

            switch (child.Kind)
            {
                // If the child is empty/nothing, there's nothing to be made atomic so the Atomic
                // node can simply be removed.
                case RegexNodeKind.Empty:
                case RegexNodeKind.Nothing:
                    return child;

                // If the child is already atomic, we can just remove the atomic node.
                case RegexNodeKind.Oneloopatomic:
                case RegexNodeKind.Notoneloopatomic:
                case RegexNodeKind.Setloopatomic:
                    return child;

                // If an atomic subexpression contains only a {one/notone/set}{loop/lazy},
                // change it to be an {one/notone/set}loopatomic and remove the atomic node.
                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Setloop:
                case RegexNodeKind.Onelazy:
                case RegexNodeKind.Notonelazy:
                case RegexNodeKind.Setlazy:
                    child.MakeLoopAtomic();
                    return child;

                // Alternations have a variety of possible optimizations that can be applied
                // iff they're atomic.
                case RegexNodeKind.Alternate:
                    if ((Options & RegexOptions.RightToLeft) == 0)
                    {
                        List<RegexNode>? branches = child.Children as List<RegexNode>;
                        Debug.Assert(branches is not null && branches.Count != 0);

                        // If an alternation is atomic and its first branch is Empty, the whole thing
                        // is a nop, as Empty will match everything trivially, and no backtracking
                        // into the node will be performed, making the remaining branches irrelevant.
                        if (branches[0].Kind == RegexNodeKind.Empty)
                        {
                            return new RegexNode(RegexNodeKind.Empty, child.Options);
                        }

                        // Similarly, we can trim off any branches after an Empty, as they'll never be used.
                        // An Empty will match anything, and thus branches after that would only be used
                        // if we backtracked into it and advanced passed the Empty after trying the Empty...
                        // but if the alternation is atomic, such backtracking won't happen.
                        for (int i = 1; i < branches.Count - 1; i++)
                        {
                            if (branches[i].Kind == RegexNodeKind.Empty)
                            {
                                branches.RemoveRange(i + 1, branches.Count - (i + 1));
                                break;
                            }
                        }

                        // If an alternation is atomic, we won't ever backtrack back into it, which
                        // means order matters but not repetition.  With backtracking, it would be incorrect
                        // to convert an expression like "hi|there|hello" into "hi|hello|there", as doing
                        // so could then change the order of results if we matched "hi" and then failed
                        // based on what came after it, and both "hello" and "there" could be successful
                        // with what came later.  But without backtracking, we can reorder "hi|there|hello"
                        // to instead be "hi|hello|there", as "hello" and "there" can't match the same text,
                        // and once this atomic alternation has matched, we won't try another branch. This
                        // reordering is valuable as it then enables further optimizations, e.g.
                        // "hi|there|hello" => "hi|hello|there" => "h(?:i|ello)|there", which means we only
                        // need to check the 'h' once in case it's not an 'h', and it's easier to employ different
                        // code gen that, for example, switches on first character of the branches, enabling faster
                        // choice of branch without always having to walk through each.
                        bool reordered = false;
                        for (int start = 0; start < branches.Count; start++)
                        {
                            // Get the node that may start our range.  If it's a one, multi, or concat of those, proceed.
                            RegexNode startNode = branches[start];
                            if (startNode.FindBranchOneOrMultiStart() is null)
                            {
                                continue;
                            }

                            // Find the contiguous range of nodes from this point that are similarly one, multi, or concat of those.
                            int endExclusive = start + 1;
                            while (endExclusive < branches.Count && branches[endExclusive].FindBranchOneOrMultiStart() is not null)
                            {
                                endExclusive++;
                            }

                            // If there's at least 3, there may be something to reorder (we won't reorder anything
                            // before the starting position, and so only 2 items is considered ordered).
                            if (endExclusive - start >= 3)
                            {
                                int compare = start;
                                while (compare < endExclusive)
                                {
                                    // Get the starting character
                                    char c = branches[compare].FindBranchOneOrMultiStart()!.FirstCharOfOneOrMulti();

                                    // Move compare to point to the last branch that has the same starting value.
                                    while (compare < endExclusive && branches[compare].FindBranchOneOrMultiStart()!.FirstCharOfOneOrMulti() == c)
                                    {
                                        compare++;
                                    }

                                    // Compare now points to the first node that doesn't match the starting node.
                                    // If we've walked off our range, there's nothing left to reorder.
                                    if (compare < endExclusive)
                                    {
                                        // There may be something to reorder.  See if there are any other nodes that begin with the same character.
                                        for (int next = compare + 1; next < endExclusive; next++)
                                        {
                                            RegexNode nextChild = branches[next];
                                            if (nextChild.FindBranchOneOrMultiStart()!.FirstCharOfOneOrMulti() == c)
                                            {
                                                branches.RemoveAt(next);
                                                branches.Insert(compare++, nextChild);
                                                reordered = true;
                                            }
                                        }
                                    }
                                }
                            }

                            // Move to the end of the range we've now explored. endExclusive is not a viable
                            // starting position either, and the start++ for the loop will thus take us to
                            // the next potential place to start a range.
                            start = endExclusive;
                        }

                        // If anything was reordered, there may be new optimization opportunities inside
                        // of the alternation, so reduce it again.
                        if (reordered)
                        {
                            atomic.ReplaceChild(0, child);
                            child = atomic.Child(0);
                        }
                    }
                    goto default;

                // For everything else, try to reduce ending backtracking of the last contained expression.
                default:
                    child.EliminateEndingBacktracking();
                    return atomic;
            }
        }

        /// <summary>Combine nested loops where applicable.</summary>
        /// <remarks>
        /// Nested repeaters just get multiplied with each other if they're not too lumpy.
        /// Other optimizations may have also resulted in {Lazy}loops directly containing
        /// sets, ones, and notones, in which case they can be transformed into the corresponding
        /// individual looping constructs.
        /// </remarks>
        private RegexNode ReduceLoops()
        {
            Debug.Assert(Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop);

            RegexNode u = this;
            RegexNodeKind kind = Kind;

            int min = M;
            int max = N;

            while (u.ChildCount() > 0)
            {
                RegexNode child = u.Child(0);

                // multiply reps of the same type only
                if (child.Kind != kind)
                {
                    bool valid = false;
                    if (kind == RegexNodeKind.Loop)
                    {
                        switch (child.Kind)
                        {
                            case RegexNodeKind.Oneloop:
                            case RegexNodeKind.Oneloopatomic:
                            case RegexNodeKind.Notoneloop:
                            case RegexNodeKind.Notoneloopatomic:
                            case RegexNodeKind.Setloop:
                            case RegexNodeKind.Setloopatomic:
                                valid = true;
                                break;
                        }
                    }
                    else // type == Lazyloop
                    {
                        switch (child.Kind)
                        {
                            case RegexNodeKind.Onelazy:
                            case RegexNodeKind.Notonelazy:
                            case RegexNodeKind.Setlazy:
                                valid = true;
                                break;
                        }
                    }

                    if (!valid)
                    {
                        break;
                    }
                }

                // child can be too lumpy to blur, e.g., (a {100,105}) {3} or (a {2,})?
                // [but things like (a {2,})+ are not too lumpy...]
                if (u.M == 0 && child.M > 1 || child.N < child.M * 2)
                {
                    break;
                }

                u = child;

                if (u.M > 0)
                {
                    u.M = min = ((int.MaxValue - 1) / u.M < min) ? int.MaxValue : u.M * min;
                }

                if (u.N > 0)
                {
                    u.N = max = ((int.MaxValue - 1) / u.N < max) ? int.MaxValue : u.N * max;
                }
            }

            if (min == int.MaxValue)
            {
                return new RegexNode(RegexNodeKind.Nothing, Options);
            }

            // If the Loop or Lazyloop now only has one child node and its a Set, One, or Notone,
            // reduce to just Setloop/lazy, Oneloop/lazy, or Notoneloop/lazy.  The parser will
            // generally have only produced the latter, but other reductions could have exposed
            // this.
            if (u.ChildCount() == 1)
            {
                RegexNode child = u.Child(0);
                switch (child.Kind)
                {
                    case RegexNodeKind.One:
                    case RegexNodeKind.Notone:
                    case RegexNodeKind.Set:
                        child.MakeRep(u.Kind == RegexNodeKind.Lazyloop ? RegexNodeKind.Onelazy : RegexNodeKind.Oneloop, u.M, u.N);
                        u = child;
                        break;
                }
            }

            return u;
        }

        /// <summary>
        /// Reduces set-related nodes to simpler one-related and notone-related nodes, where applicable.
        /// </summary>
        /// <remarks>
        /// e.g.
        /// [a] => a
        /// [a]* => a*
        /// [a]*? => a*?
        /// (?>[a]*) => (?>a*)
        /// [^a] => ^a
        /// []* => Nothing
        /// </remarks>
        private RegexNode ReduceSet()
        {
            // Extract empty-set, one, and not-one case as special
            Debug.Assert(Kind is RegexNodeKind.Set or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy);
            Debug.Assert(!string.IsNullOrEmpty(Str));

            if (RegexCharClass.IsEmpty(Str))
            {
                Kind = RegexNodeKind.Nothing;
                Str = null;
            }
            else if (RegexCharClass.IsSingleton(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Kind =
                    Kind == RegexNodeKind.Set ? RegexNodeKind.One :
                    Kind == RegexNodeKind.Setloop ? RegexNodeKind.Oneloop :
                    Kind == RegexNodeKind.Setloopatomic ? RegexNodeKind.Oneloopatomic :
                    RegexNodeKind.Onelazy;
            }
            else if (RegexCharClass.IsSingletonInverse(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Kind =
                    Kind == RegexNodeKind.Set ? RegexNodeKind.Notone :
                    Kind == RegexNodeKind.Setloop ? RegexNodeKind.Notoneloop :
                    Kind == RegexNodeKind.Setloopatomic ? RegexNodeKind.Notoneloopatomic :
                    RegexNodeKind.Notonelazy;
            }

            // Normalize some well-known sets
            switch (Str)
            {
                // Different ways of saying "match anything"
                case RegexCharClass.WordNotWordClass:
                case RegexCharClass.NotWordWordClass:
                case RegexCharClass.DigitNotDigitClass:
                case RegexCharClass.NotDigitDigitClass:
                case RegexCharClass.SpaceNotSpaceClass:
                case RegexCharClass.NotSpaceSpaceClass:
                    Str = RegexCharClass.AnyClass;
                    break;
            }

            return this;
        }

        /// <summary>Optimize an alternation.</summary>
        private RegexNode ReduceAlternation()
        {
            Debug.Assert(Kind == RegexNodeKind.Alternate);

            switch (ChildCount())
            {
                case 0:
                    return new RegexNode(RegexNodeKind.Nothing, Options);

                case 1:
                    return Child(0);

                default:
                    ReduceSingleLetterAndNestedAlternations();
                    RegexNode node = ReplaceNodeIfUnnecessary();
                    if (node.Kind == RegexNodeKind.Alternate)
                    {
                        node = ExtractCommonPrefixText(node);
                        if (node.Kind == RegexNodeKind.Alternate)
                        {
                            node = ExtractCommonPrefixOneNotoneSet(node);
                            if (node.Kind == RegexNodeKind.Alternate)
                            {
                                node = RemoveRedundantEmptiesAndNothings(node);
                            }
                        }
                    }
                    return node;
            }

            // This function performs two optimizations:
            // - Single-letter alternations can be replaced by faster set specifications
            //   e.g. "a|b|c|def|g|h" -> "[a-c]|def|[gh]"
            // - Nested alternations with no intervening operators can be flattened:
            //   e.g. "apple|(?:orange|pear)|grape" -> "apple|orange|pear|grape"
            void ReduceSingleLetterAndNestedAlternations()
            {
                bool wasLastSet = false;
                bool lastNodeCannotMerge = false;
                RegexOptions optionsLast = 0;
                RegexOptions optionsAt;
                int i;
                int j;
                RegexNode at;
                RegexNode prev;

                List<RegexNode> children = (List<RegexNode>)Children!;
                for (i = 0, j = 0; i < children.Count; i++, j++)
                {
                    at = children[i];

                    if (j < i)
                        children[j] = at;

                    while (true)
                    {
                        if (at.Kind == RegexNodeKind.Alternate)
                        {
                            if (at.Children is List<RegexNode> atChildren)
                            {
                                for (int k = 0; k < atChildren.Count; k++)
                                {
                                    atChildren[k].Parent = this;
                                }
                                children.InsertRange(i + 1, atChildren);
                            }
                            else
                            {
                                RegexNode atChild = (RegexNode)at.Children!;
                                atChild.Parent = this;
                                children.Insert(i + 1, atChild);
                            }
                            j--;
                        }
                        else if (at.Kind is RegexNodeKind.Set or RegexNodeKind.One)
                        {
                            // Cannot merge sets if L or I options differ, or if either are negated.
                            optionsAt = at.Options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                            if (at.Kind == RegexNodeKind.Set)
                            {
                                if (!wasLastSet || optionsLast != optionsAt || lastNodeCannotMerge || !RegexCharClass.IsMergeable(at.Str!))
                                {
                                    wasLastSet = true;
                                    lastNodeCannotMerge = !RegexCharClass.IsMergeable(at.Str!);
                                    optionsLast = optionsAt;
                                    break;
                                }
                            }
                            else if (!wasLastSet || optionsLast != optionsAt || lastNodeCannotMerge)
                            {
                                wasLastSet = true;
                                lastNodeCannotMerge = false;
                                optionsLast = optionsAt;
                                break;
                            }

                            // The last node was a Set or a One, we're a Set or One and our options are the same.
                            // Merge the two nodes.
                            j--;
                            prev = children[j];

                            RegexCharClass prevCharClass;
                            if (prev.Kind == RegexNodeKind.One)
                            {
                                prevCharClass = new RegexCharClass();
                                prevCharClass.AddChar(prev.Ch);
                            }
                            else
                            {
                                prevCharClass = RegexCharClass.Parse(prev.Str!);
                            }

                            if (at.Kind == RegexNodeKind.One)
                            {
                                prevCharClass.AddChar(at.Ch);
                            }
                            else
                            {
                                RegexCharClass atCharClass = RegexCharClass.Parse(at.Str!);
                                prevCharClass.AddCharClass(atCharClass);
                            }

                            prev.Kind = RegexNodeKind.Set;
                            prev.Str = prevCharClass.ToStringClass();
                            if ((prev.Options & RegexOptions.IgnoreCase) != 0)
                            {
                                prev.Options &= ~RegexOptions.IgnoreCase;
                            }
                        }
                        else if (at.Kind == RegexNodeKind.Nothing)
                        {
                            j--;
                        }
                        else
                        {
                            wasLastSet = false;
                            lastNodeCannotMerge = false;
                        }
                        break;
                    }
                }

                if (j < i)
                {
                    children.RemoveRange(j, i - j);
                }
            }

            // This function optimizes out prefix nodes from alternation branches that are
            // the same across multiple contiguous branches.
            // e.g. \w12|\d34|\d56|\w78|\w90 => \w12|\d(?:34|56)|\w(?:78|90)
            static RegexNode ExtractCommonPrefixOneNotoneSet(RegexNode alternation)
            {
                Debug.Assert(alternation.Kind == RegexNodeKind.Alternate);
                Debug.Assert(alternation.Children is List<RegexNode> { Count: >= 2 });
                var children = (List<RegexNode>)alternation.Children;

                // Only process left-to-right prefixes.
                if ((alternation.Options & RegexOptions.RightToLeft) != 0)
                {
                    return alternation;
                }

                // Only handle the case where each branch is a concatenation
                foreach (RegexNode child in children)
                {
                    if (child.Kind != RegexNodeKind.Concatenate || child.ChildCount() < 2)
                    {
                        return alternation;
                    }
                }

                for (int startingIndex = 0; startingIndex < children.Count - 1; startingIndex++)
                {
                    Debug.Assert(children[startingIndex].Children is List<RegexNode> { Count: >= 2 });

                    // Only handle the case where each branch begins with the same One, Notone, or Set (individual or loop).
                    // Note that while we can do this for individual characters, fixed length loops, and atomic loops, doing
                    // it for non-atomic variable length loops could change behavior as each branch could otherwise have a
                    // different number of characters consumed by the loop based on what's after it.
                    RegexNode required = children[startingIndex].Child(0);
                    switch (required.Kind)
                    {
                        case RegexNodeKind.One or RegexNodeKind.Notone or RegexNodeKind.Set:
                        case RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloopatomic:
                        case RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop or RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy when required.M == required.N:
                            break;

                        default:
                            continue;
                    }

                    // Only handle the case where each branch begins with the exact same node value
                    int endingIndex = startingIndex + 1;
                    for (; endingIndex < children.Count; endingIndex++)
                    {
                        RegexNode other = children[endingIndex].Child(0);
                        if (required.Kind != other.Kind ||
                            required.Options != other.Options ||
                            required.M != other.M ||
                            required.N != other.N ||
                            required.Ch != other.Ch ||
                            required.Str != other.Str)
                        {
                            break;
                        }
                    }

                    if (endingIndex - startingIndex <= 1)
                    {
                        // Nothing to extract from this starting index.
                        continue;
                    }

                    // Remove the prefix node from every branch, adding it to a new alternation
                    var newAlternate = new RegexNode(RegexNodeKind.Alternate, alternation.Options);
                    for (int i = startingIndex; i < endingIndex; i++)
                    {
                        ((List<RegexNode>)children[i].Children!).RemoveAt(0);
                        newAlternate.AddChild(children[i]);
                    }

                    // If this alternation is wrapped as atomic, we need to do the same for the new alternation.
                    if (alternation.Parent is RegexNode { Kind: RegexNodeKind.Atomic } parent)
                    {
                        var atomic = new RegexNode(RegexNodeKind.Atomic, alternation.Options);
                        atomic.AddChild(newAlternate);
                        newAlternate = atomic;
                    }

                    // Now create a concatenation of the prefix node with the new alternation for the combined
                    // branches, and replace all of the branches in this alternation with that new concatenation.
                    var newConcat = new RegexNode(RegexNodeKind.Concatenate, alternation.Options);
                    newConcat.AddChild(required);
                    newConcat.AddChild(newAlternate);
                    alternation.ReplaceChild(startingIndex, newConcat);
                    children.RemoveRange(startingIndex + 1, endingIndex - startingIndex - 1);
                }

                return alternation.ReplaceNodeIfUnnecessary();
            }

            // Removes unnecessary Empty and Nothing nodes from the alternation. A Nothing will never
            // match, so it can be removed entirely, and an Empty can be removed if there's a previous
            // Empty in the alternation: it's an extreme case of just having a repeated branch in an
            // alternation, and while we don't check for all duplicates, checking for empty is easy.
            static RegexNode RemoveRedundantEmptiesAndNothings(RegexNode node)
            {
                Debug.Assert(node.Kind == RegexNodeKind.Alternate);
                Debug.Assert(node.ChildCount() >= 2);
                var children = (List<RegexNode>)node.Children!;

                int i = 0, j = 0;
                bool seenEmpty = false;
                while (i < children.Count)
                {
                    RegexNode child = children[i];
                    switch (child.Kind)
                    {
                        case RegexNodeKind.Empty when !seenEmpty:
                            seenEmpty = true;
                            goto default;

                        case RegexNodeKind.Empty:
                        case RegexNodeKind.Nothing:
                            i++;
                            break;

                        default:
                            children[j] = children[i];
                            i++;
                            j++;
                            break;
                    }
                }

                children.RemoveRange(j, children.Count - j);
                return node.ReplaceNodeIfUnnecessary();
            }

            // Analyzes all the branches of the alternation for text that's identical at the beginning
            // of every branch.  That text is then pulled out into its own one or multi node in a
            // concatenation with the alternation (whose branches are updated to remove that prefix).
            // This is valuable for a few reasons.  One, it exposes potentially more text to the
            // expression prefix analyzer used to influence FindFirstChar.  Second, it exposes more
            // potential alternation optimizations, e.g. if the same prefix is followed in two branches
            // by sets that can be merged.  Third, it reduces the amount of duplicated comparisons required
            // if we end up backtracking into subsequent branches.
            // e.g. abc|ade => a(?bc|de)
            static RegexNode ExtractCommonPrefixText(RegexNode alternation)
            {
                Debug.Assert(alternation.Kind == RegexNodeKind.Alternate);
                Debug.Assert(alternation.Children is List<RegexNode> { Count: >= 2 });
                var children = (List<RegexNode>)alternation.Children;

                // To keep things relatively simple, we currently only handle:
                // - Left to right (e.g. we don't process alternations in lookbehinds)
                // - Branches that are one or multi nodes, or that are concatenations beginning with one or multi nodes.
                // - All branches having the same options.

                // Only extract left-to-right prefixes.
                if ((alternation.Options & RegexOptions.RightToLeft) != 0)
                {
                    return alternation;
                }

                Span<char> scratchChar = stackalloc char[1];
                ReadOnlySpan<char> startingSpan = stackalloc char[0];
                for (int startingIndex = 0; startingIndex < children.Count - 1; startingIndex++)
                {
                    // Process the first branch to get the maximum possible common string.
                    RegexNode? startingNode = children[startingIndex].FindBranchOneOrMultiStart();
                    if (startingNode is null)
                    {
                        return alternation;
                    }

                    RegexOptions startingNodeOptions = startingNode.Options;
                    startingSpan = startingNode.Str.AsSpan();
                    if (startingNode.Kind == RegexNodeKind.One)
                    {
                        scratchChar[0] = startingNode.Ch;
                        startingSpan = scratchChar;
                    }
                    Debug.Assert(startingSpan.Length > 0);

                    // Now compare the rest of the branches against it.
                    int endingIndex = startingIndex + 1;
                    for (; endingIndex < children.Count; endingIndex++)
                    {
                        // Get the starting node of the next branch.
                        startingNode = children[endingIndex].FindBranchOneOrMultiStart();
                        if (startingNode is null || startingNode.Options != startingNodeOptions)
                        {
                            break;
                        }

                        // See if the new branch's prefix has a shared prefix with the current one.
                        // If it does, shorten to that; if it doesn't, bail.
                        if (startingNode.Kind == RegexNodeKind.One)
                        {
                            if (startingSpan[0] != startingNode.Ch)
                            {
                                break;
                            }

                            if (startingSpan.Length != 1)
                            {
                                startingSpan = startingSpan.Slice(0, 1);
                            }
                        }
                        else
                        {
                            Debug.Assert(startingNode.Kind == RegexNodeKind.Multi);
                            Debug.Assert(startingNode.Str!.Length > 0);

                            int minLength = Math.Min(startingSpan.Length, startingNode.Str.Length);
                            int c = 0;
                            while (c < minLength && startingSpan[c] == startingNode.Str[c]) c++;
                            if (c == 0)
                            {
                                break;
                            }

                            startingSpan = startingSpan.Slice(0, c);
                        }
                    }

                    // When we get here, we have a starting string prefix shared by all branches
                    // in the range [startingIndex, endingIndex).
                    if (endingIndex - startingIndex <= 1)
                    {
                        // There's nothing to consolidate for this starting node.
                        continue;
                    }

                    // We should be able to consolidate something for the nodes in the range [startingIndex, endingIndex).
                    Debug.Assert(startingSpan.Length > 0);

                    // Create a new node of the form:
                    //     Concatenation(prefix, Alternation(each | node | with | prefix | removed))
                    // that replaces all these branches in this alternation.

                    var prefix = startingSpan.Length == 1 ?
                        new RegexNode(RegexNodeKind.One, startingNodeOptions, startingSpan[0]) :
                        new RegexNode(RegexNodeKind.Multi, startingNodeOptions, startingSpan.ToString());
                    var newAlternate = new RegexNode(RegexNodeKind.Alternate, startingNodeOptions);
                    for (int i = startingIndex; i < endingIndex; i++)
                    {
                        RegexNode branch = children[i];
                        ProcessOneOrMulti(branch.Kind == RegexNodeKind.Concatenate ? branch.Child(0) : branch, startingSpan);
                        branch = branch.Reduce();
                        newAlternate.AddChild(branch);

                        // Remove the starting text from the one or multi node.  This may end up changing
                        // the type of the node to be Empty if the starting text matches the node's full value.
                        static void ProcessOneOrMulti(RegexNode node, ReadOnlySpan<char> startingSpan)
                        {
                            if (node.Kind == RegexNodeKind.One)
                            {
                                Debug.Assert(startingSpan.Length == 1);
                                Debug.Assert(startingSpan[0] == node.Ch);
                                node.Kind = RegexNodeKind.Empty;
                                node.Ch = '\0';
                            }
                            else
                            {
                                Debug.Assert(node.Kind == RegexNodeKind.Multi);
                                Debug.Assert(node.Str.AsSpan().StartsWith(startingSpan, StringComparison.Ordinal));
                                if (node.Str!.Length == startingSpan.Length)
                                {
                                    node.Kind = RegexNodeKind.Empty;
                                    node.Str = null;
                                }
                                else if (node.Str.Length - 1 == startingSpan.Length)
                                {
                                    node.Kind = RegexNodeKind.One;
                                    node.Ch = node.Str[node.Str.Length - 1];
                                    node.Str = null;
                                }
                                else
                                {
                                    node.Str = node.Str.Substring(startingSpan.Length);
                                }
                            }
                        }
                    }

                    if (alternation.Parent is RegexNode parent && parent.Kind == RegexNodeKind.Atomic)
                    {
                        var atomic = new RegexNode(RegexNodeKind.Atomic, startingNodeOptions);
                        atomic.AddChild(newAlternate);
                        newAlternate = atomic;
                    }

                    var newConcat = new RegexNode(RegexNodeKind.Concatenate, startingNodeOptions);
                    newConcat.AddChild(prefix);
                    newConcat.AddChild(newAlternate);
                    alternation.ReplaceChild(startingIndex, newConcat);
                    children.RemoveRange(startingIndex + 1, endingIndex - startingIndex - 1);
                }

                return alternation.ChildCount() == 1 ? alternation.Child(0) : alternation;
            }
        }

        /// <summary>
        /// Finds the starting one or multi of the branch, if it has one; otherwise, returns null.
        /// For simplicity, this only considers branches that are One or Multi, or a Concatenation
        /// beginning with a One or Multi.  We don't traverse more than one level to avoid the
        /// complication of then having to later update that hierarchy when removing the prefix,
        /// but it could be done in the future if proven beneficial enough.
        /// </summary>
        public RegexNode? FindBranchOneOrMultiStart()
        {
            RegexNode branch = Kind == RegexNodeKind.Concatenate ? Child(0) : this;
            return branch.Kind is RegexNodeKind.One or RegexNodeKind.Multi ? branch : null;
        }

        /// <summary>Same as <see cref="FindBranchOneOrMultiStart"/> but also for Sets.</summary>
        public RegexNode? FindBranchOneMultiOrSetStart()
        {
            RegexNode branch = Kind == RegexNodeKind.Concatenate ? Child(0) : this;
            return branch.Kind is RegexNodeKind.One or RegexNodeKind.Multi or RegexNodeKind.Set ? branch : null;
        }

        /// <summary>Gets the character that begins a One or Multi.</summary>
        public char FirstCharOfOneOrMulti()
        {
            Debug.Assert(Kind is RegexNodeKind.One or RegexNodeKind.Multi);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);
            return Kind == RegexNodeKind.One ? Ch : Str![0];
        }

        /// <summary>Finds the guaranteed beginning literal(s) of the node, or null if none exists.</summary>
        public (char Char, string? String, string? SetChars)? FindStartingLiteral(int maxSetCharacters = 5) // 5 is max optimized by IndexOfAny today
        {
            Debug.Assert(maxSetCharacters >= 0 && maxSetCharacters <= 128, $"{nameof(maxSetCharacters)} == {maxSetCharacters} should be small enough to be stack allocated.");

            RegexNode? node = this;
            while (true)
            {
                if (node is not null && (node.Options & RegexOptions.RightToLeft) == 0)
                {
                    switch (node.Kind)
                    {
                        case RegexNodeKind.One:
                        case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy when node.M > 0:
                            if ((node.Options & RegexOptions.IgnoreCase) == 0 || !RegexCharClass.ParticipatesInCaseConversion(node.Ch))
                            {
                                return (node.Ch, null, null);
                            }
                            break;

                        case RegexNodeKind.Multi:
                            if ((node.Options & RegexOptions.IgnoreCase) == 0 || !RegexCharClass.ParticipatesInCaseConversion(node.Str.AsSpan()))
                            {
                                return ('\0', node.Str, null);
                            }
                            break;

                        case RegexNodeKind.Set:
                        case RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy when node.M > 0:
                            Span<char> setChars = stackalloc char[maxSetCharacters];
                            int numChars;
                            if (!RegexCharClass.IsNegated(node.Str!) &&
                                (numChars = RegexCharClass.GetSetChars(node.Str!, setChars)) != 0)
                            {
                                setChars = setChars.Slice(0, numChars);
                                if ((node.Options & RegexOptions.IgnoreCase) == 0 || !RegexCharClass.ParticipatesInCaseConversion(setChars))
                                {
                                    return ('\0', null, setChars.ToString());
                                }
                            }
                            break;

                        case RegexNodeKind.Atomic:
                        case RegexNodeKind.Concatenate:
                        case RegexNodeKind.Capture:
                        case RegexNodeKind.Group:
                        case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when node.M > 0:
                        case RegexNodeKind.PositiveLookaround:
                            node = node.Child(0);
                            continue;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Optimizes a concatenation by coalescing adjacent characters and strings,
        /// coalescing adjacent loops, converting loops to be atomic where applicable,
        /// and removing the concatenation itself if it's unnecessary.
        /// </summary>
        private RegexNode ReduceConcatenation()
        {
            Debug.Assert(Kind == RegexNodeKind.Concatenate);

            // If the concat node has zero or only one child, get rid of the concat.
            switch (ChildCount())
            {
                case 0:
                    return new RegexNode(RegexNodeKind.Empty, Options);
                case 1:
                    return Child(0);
            }

            // If any node in the concatenation is a Nothing, the concatenation itself is a Nothing.
            int childCount = ChildCount();
            for (int i = 0; i < childCount; i++)
            {
                RegexNode child = Child(i);
                if (child.Kind == RegexNodeKind.Nothing)
                {
                    return child;
                }
            }

            // Coalesce adjacent loops.  This helps to minimize work done by the interpreter, minimize code gen,
            // and also help to reduce catastrophic backtracking.
            ReduceConcatenationWithAdjacentLoops();

            // Coalesce adjacent characters/strings.  This is done after the adjacent loop coalescing so that
            // a One adjacent to both a Multi and a Loop prefers being folded into the Loop rather than into
            // the Multi.  Doing so helps with auto-atomicity when it's later applied.
            ReduceConcatenationWithAdjacentStrings();

            // If the concatenation is now empty, return an empty node, or if it's got a single child, return that child.
            // Otherwise, return this.
            return ReplaceNodeIfUnnecessary();
        }

        /// <summary>
        /// Combine adjacent characters/strings.
        /// e.g. (?:abc)(?:def) -> abcdef
        /// </summary>
        private void ReduceConcatenationWithAdjacentStrings()
        {
            Debug.Assert(Kind == RegexNodeKind.Concatenate);
            Debug.Assert(Children is List<RegexNode>);

            bool wasLastString = false;
            RegexOptions optionsLast = 0;
            int i, j;

            List<RegexNode> children = (List<RegexNode>)Children!;
            for (i = 0, j = 0; i < children.Count; i++, j++)
            {
                RegexNode at = children[i];

                if (j < i)
                {
                    children[j] = at;
                }

                if (at.Kind == RegexNodeKind.Concatenate &&
                    ((at.Options & RegexOptions.RightToLeft) == (Options & RegexOptions.RightToLeft)))
                {
                    if (at.Children is List<RegexNode> atChildren)
                    {
                        for (int k = 0; k < atChildren.Count; k++)
                        {
                            atChildren[k].Parent = this;
                        }
                        children.InsertRange(i + 1, atChildren);
                    }
                    else
                    {
                        RegexNode atChild = (RegexNode)at.Children!;
                        atChild.Parent = this;
                        children.Insert(i + 1, atChild);
                    }
                    j--;
                }
                else if (at.Kind is RegexNodeKind.Multi or RegexNodeKind.One)
                {
                    // Cannot merge strings if L or I options differ
                    RegexOptions optionsAt = at.Options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                    if (!wasLastString || optionsLast != optionsAt)
                    {
                        wasLastString = true;
                        optionsLast = optionsAt;
                        continue;
                    }

                    RegexNode prev = children[--j];

                    if (prev.Kind == RegexNodeKind.One)
                    {
                        prev.Kind = RegexNodeKind.Multi;
                        prev.Str = prev.Ch.ToString();
                    }

                    if ((optionsAt & RegexOptions.RightToLeft) == 0)
                    {
                        prev.Str = (at.Kind == RegexNodeKind.One) ? $"{prev.Str}{at.Ch}" : prev.Str + at.Str;
                    }
                    else
                    {
                        prev.Str = (at.Kind == RegexNodeKind.One) ? $"{at.Ch}{prev.Str}" : at.Str + prev.Str;
                    }
                }
                else if (at.Kind == RegexNodeKind.Empty)
                {
                    j--;
                }
                else
                {
                    wasLastString = false;
                }
            }

            if (j < i)
            {
                children.RemoveRange(j, i - j);
            }
        }

        /// <summary>
        /// Combine adjacent loops.
        /// e.g. a*a*a* => a*
        /// e.g. a+ab => a{2,}b
        /// </summary>
        private void ReduceConcatenationWithAdjacentLoops()
        {
            Debug.Assert(Kind == RegexNodeKind.Concatenate);
            Debug.Assert(Children is List<RegexNode>);

            var children = (List<RegexNode>)Children!;
            int current = 0, next = 1, nextSave = 1;

            while (next < children.Count)
            {
                RegexNode currentNode = children[current];
                RegexNode nextNode = children[next];

                if (currentNode.Options == nextNode.Options)
                {
                    static bool CanCombineCounts(int nodeMin, int nodeMax, int nextMin, int nextMax)
                    {
                        // We shouldn't have an infinite minimum; bail if we find one. Also check for the
                        // degenerate case where we'd make the min overflow or go infinite when it wasn't already.
                        if (nodeMin == int.MaxValue ||
                            nextMin == int.MaxValue ||
                            (uint)nodeMin + (uint)nextMin >= int.MaxValue)
                        {
                            return false;
                        }

                        // Similar overflow / go infinite check for max (which can be infinite).
                        if (nodeMax != int.MaxValue &&
                            nextMax != int.MaxValue &&
                            (uint)nodeMax + (uint)nextMax >= int.MaxValue)
                        {
                            return false;
                        }

                        return true;
                    }

                    switch (currentNode.Kind)
                    {
                        // Coalescing a loop with its same type
                        case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy when nextNode.Kind == currentNode.Kind && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy when nextNode.Kind == currentNode.Kind && currentNode.Str == nextNode.Str:
                            if (CanCombineCounts(currentNode.M, currentNode.N, nextNode.M, nextNode.N))
                            {
                                currentNode.M += nextNode.M;
                                if (currentNode.N != int.MaxValue)
                                {
                                    currentNode.N = nextNode.N == int.MaxValue ? int.MaxValue : currentNode.N + nextNode.N;
                                }
                                next++;
                                continue;
                            }
                            break;

                        // Coalescing a loop with an additional item of the same type
                        case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy when nextNode.Kind == RegexNodeKind.One && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy when nextNode.Kind == RegexNodeKind.Notone && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy when nextNode.Kind == RegexNodeKind.Set && currentNode.Str == nextNode.Str:
                            if (CanCombineCounts(currentNode.M, currentNode.N, 1, 1))
                            {
                                currentNode.M++;
                                if (currentNode.N != int.MaxValue)
                                {
                                    currentNode.N++;
                                }
                                next++;
                                continue;
                            }
                            break;

                        // Coalescing a loop with a subsequent string
                        case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy when nextNode.Kind == RegexNodeKind.Multi && currentNode.Ch == nextNode.Str![0]:
                            {
                                // Determine how many of the multi's characters can be combined.
                                // We already checked for the first, so we know it's at least one.
                                int matchingCharsInMulti = 1;
                                while (matchingCharsInMulti < nextNode.Str.Length && currentNode.Ch == nextNode.Str[matchingCharsInMulti])
                                {
                                    matchingCharsInMulti++;
                                }

                                if (CanCombineCounts(currentNode.M, currentNode.N, matchingCharsInMulti, matchingCharsInMulti))
                                {
                                    // Update the loop's bounds to include those characters from the multi
                                    currentNode.M += matchingCharsInMulti;
                                    if (currentNode.N != int.MaxValue)
                                    {
                                        currentNode.N += matchingCharsInMulti;
                                    }

                                    // If it was the full multi, skip/remove the multi and continue processing this loop.
                                    if (nextNode.Str.Length == matchingCharsInMulti)
                                    {
                                        next++;
                                        continue;
                                    }

                                    // Otherwise, trim the characters from the multiple that were absorbed into the loop.
                                    // If it now only has a single character, it becomes a One.
                                    Debug.Assert(matchingCharsInMulti < nextNode.Str.Length);
                                    if (nextNode.Str.Length - matchingCharsInMulti == 1)
                                    {
                                        nextNode.Kind = RegexNodeKind.One;
                                        nextNode.Ch = nextNode.Str[nextNode.Str.Length - 1];
                                        nextNode.Str = null;
                                    }
                                    else
                                    {
                                        nextNode.Str = nextNode.Str.Substring(matchingCharsInMulti);
                                    }
                                }
                            }
                            break;

                        // NOTE: We could add support for coalescing a string with a subsequent loop, but the benefits of that
                        // are limited. Pulling a subsequent string's prefix back into the loop helps with making the loop atomic,
                        // but if the loop is after the string, pulling the suffix of the string forward into the loop may actually
                        // be a deoptimization as those characters could end up matching more slowly as part of loop matching.

                        // Coalescing an individual item with a loop.
                        case RegexNodeKind.One when (nextNode.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy) && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Notone when (nextNode.Kind is RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy) && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Set when (nextNode.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy) && currentNode.Str == nextNode.Str:
                            if (CanCombineCounts(1, 1, nextNode.M, nextNode.N))
                            {
                                currentNode.Kind = nextNode.Kind;
                                currentNode.M = nextNode.M + 1;
                                currentNode.N = nextNode.N == int.MaxValue ? int.MaxValue : nextNode.N + 1;
                                next++;
                                continue;
                            }
                            break;

                        // Coalescing an individual item with another individual item.
                        // We don't coalesce adjacent One nodes into a Oneloop as we'd rather they be joined into a Multi.
                        case RegexNodeKind.Notone when nextNode.Kind == currentNode.Kind && currentNode.Ch == nextNode.Ch:
                        case RegexNodeKind.Set when nextNode.Kind == RegexNodeKind.Set && currentNode.Str == nextNode.Str:
                            currentNode.MakeRep(RegexNodeKind.Oneloop, 2, 2);
                            next++;
                            continue;
                    }
                }

                children[nextSave++] = children[next];
                current = next;
                next++;
            }

            if (nextSave < children.Count)
            {
                children.RemoveRange(nextSave, children.Count - nextSave);
            }
        }

        /// <summary>
        /// Finds {one/notone/set}loop nodes in the concatenation that can be automatically upgraded
        /// to {one/notone/set}loopatomic nodes.  Such changes avoid potential useless backtracking.
        /// e.g. A*B (where sets A and B don't overlap) => (?>A*)B.
        /// </summary>
        private void FindAndMakeLoopsAtomic()
        {
            Debug.Assert((Options & RegexOptions.NonBacktracking) == 0, "Atomic groups aren't supported and don't help performance with NonBacktracking");

            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we're too deep on the stack, give up optimizing further.
                return;
            }

            if ((Options & RegexOptions.RightToLeft) != 0)
            {
                // RTL is so rare, we don't need to spend additional time/code optimizing for it.
                return;
            }

            // For all node types that have children, recur into each of those children.
            int childCount = ChildCount();
            if (childCount != 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Child(i).FindAndMakeLoopsAtomic();
                }
            }

            // If this isn't a concatenation, nothing more to do.
            if (Kind is not RegexNodeKind.Concatenate)
            {
                return;
            }

            // This is a concatenation.  Iterate through each pair of nodes in the concatenation seeing whether we can
            // make the first node (or its right-most child) atomic based on the second node (or its left-most child).
            Debug.Assert(Children is List<RegexNode>);
            var children = (List<RegexNode>)Children;
            for (int i = 0; i < childCount - 1; i++)
            {
                ProcessNode(children[i], children[i + 1]);

                static void ProcessNode(RegexNode node, RegexNode subsequent)
                {
                    if (!StackHelper.TryEnsureSufficientExecutionStack())
                    {
                        // If we can't recur further, just stop optimizing.
                        return;
                    }

                    // Skip down the node past irrelevant nodes.
                    while (true)
                    {
                        // We can always recur into captures and into the last node of concatenations.
                        if (node.Kind is RegexNodeKind.Capture or RegexNodeKind.Concatenate)
                        {
                            node = node.Child(node.ChildCount() - 1);
                            continue;
                        }

                        // For loops with at least one guaranteed iteration, we can recur into them, but
                        // we need to be careful not to just always do so; the ending node of a loop can only
                        // be made atomic if what comes after the loop but also the beginning of the loop are
                        // compatible for the optimization.
                        if (node.Kind == RegexNodeKind.Loop)
                        {
                            RegexNode? loopDescendent = node.FindLastExpressionInLoopForAutoAtomic();
                            if (loopDescendent != null)
                            {
                                node = loopDescendent;
                                continue;
                            }
                        }

                        // Can't skip any further.
                        break;
                    }

                    // If the node can be changed to atomic based on what comes after it, do so.
                    switch (node.Kind)
                    {
                        case RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop when CanBeMadeAtomic(node, subsequent, allowSubsequentIteration: true):
                            node.MakeLoopAtomic();
                            break;

                        case RegexNodeKind.Alternate or RegexNodeKind.BackreferenceConditional or RegexNodeKind.ExpressionConditional:
                            // In the case of alternation, we can't change the alternation node itself
                            // based on what comes after it (at least not with more complicated analysis
                            // that factors in all branches together), but we can look at each individual
                            // branch, and analyze ending loops in each branch individually to see if they
                            // can be made atomic.  Then if we do end up backtracking into the alternation,
                            // we at least won't need to backtrack into that loop.  The same is true for
                            // conditionals, though we don't want to process the condition expression
                            // itself, as it's already considered atomic and handled as part of ReduceExpressionConditional.
                            {
                                int alternateBranches = node.ChildCount();
                                for (int b = node.Kind == RegexNodeKind.ExpressionConditional ? 1 : 0; b < alternateBranches; b++)
                                {
                                    ProcessNode(node.Child(b), subsequent);
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Recurs into the last expression of a loop node, looking to see if it can find a node
        /// that could be made atomic _assuming_ the conditions exist for it with the loop's ancestors.
        /// </summary>
        /// <returns>The found node that should be explored further for auto-atomicity; null if it doesn't exist.</returns>
        private RegexNode? FindLastExpressionInLoopForAutoAtomic()
        {
            RegexNode node = this;

            Debug.Assert(node.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop);

            // Start by looking at the loop's sole child.
            node = node.Child(0);

            // Skip past captures.
            while (node.Kind == RegexNodeKind.Capture)
            {
                node = node.Child(0);
            }

            // If the loop's body is a concatenate, we can skip to its last child iff that
            // last child doesn't conflict with the first child, since this whole concatenation
            // could be repeated, such that the first node ends up following the last.  For
            // example, in the expression (a+[def])*, the last child is [def] and the first is
            // a+, which can't possibly overlap with [def].  In contrast, if we had (a+[ade])*,
            // [ade] could potentially match the starting 'a'.
            if (node.Kind == RegexNodeKind.Concatenate)
            {
                int concatCount = node.ChildCount();
                RegexNode lastConcatChild = node.Child(concatCount - 1);
                if (CanBeMadeAtomic(lastConcatChild, node.Child(0), allowSubsequentIteration: false))
                {
                    return lastConcatChild;
                }
            }

            // Otherwise, the loop has nothing that can participate in auto-atomicity.
            return null;
        }

        /// <summary>Optimizations for positive and negative lookaheads/behinds.</summary>
        private RegexNode ReduceLookaround()
        {
            Debug.Assert(Kind is RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround);
            Debug.Assert(ChildCount() == 1);

            // A lookaround is a zero-width atomic assertion.
            // As it's atomic, nothing will backtrack into it, and we can
            // eliminate any ending backtracking from it.
            EliminateEndingBacktracking();

            // A positive lookaround wrapped around an empty is a nop, and we can reduce it
            // to simply Empty.  A developer typically doesn't write this, but rather it evolves
            // due to optimizations resulting in empty.

            // A negative lookaround wrapped around an empty child, i.e. (?!), is
            // sometimes used as a way to insert a guaranteed no-match into the expression,
            // often as part of a conditional. We can reduce it to simply Nothing.

            if (Child(0).Kind == RegexNodeKind.Empty)
            {
                Kind = Kind == RegexNodeKind.PositiveLookaround ? RegexNodeKind.Empty : RegexNodeKind.Nothing;
                Children = null;
            }

            return this;
        }

        /// <summary>Optimizations for backreference conditionals.</summary>
        private RegexNode ReduceBackreferenceConditional()
        {
            Debug.Assert(Kind == RegexNodeKind.BackreferenceConditional);
            Debug.Assert(ChildCount() is 1 or 2);

            // This isn't so much an optimization as it is changing the tree for consistency. We want
            // all engines to be able to trust that every backreference conditional will have two children,
            // even though it's optional in the syntax.  If it's missing a "not matched" branch,
            // we add one that will match empty.
            if (ChildCount() == 1)
            {
                AddChild(new RegexNode(RegexNodeKind.Empty, Options));
            }

            return this;
        }

        /// <summary>Optimizations for expression conditionals.</summary>
        private RegexNode ReduceExpressionConditional()
        {
            Debug.Assert(Kind == RegexNodeKind.ExpressionConditional);
            Debug.Assert(ChildCount() is 2 or 3);

            // This isn't so much an optimization as it is changing the tree for consistency. We want
            // all engines to be able to trust that every expression conditional will have three children,
            // even though it's optional in the syntax.  If it's missing a "not matched" branch,
            // we add one that will match empty.
            if (ChildCount() == 2)
            {
                AddChild(new RegexNode(RegexNodeKind.Empty, Options));
            }

            // It's common for the condition to be an explicit positive lookahead, as specifying
            // that eliminates any ambiguity in syntax as to whether the expression is to be matched
            // as an expression or to be a reference to a capture group.  After parsing, however,
            // there's no ambiguity, and we can remove an extra level of positive lookahead, as the
            // engines need to treat the condition as a zero-width positive, atomic assertion regardless.
            RegexNode condition = Child(0);
            if (condition.Kind == RegexNodeKind.PositiveLookaround && (condition.Options & RegexOptions.RightToLeft) == 0)
            {
                ReplaceChild(0, condition.Child(0));
            }

            // We can also eliminate any ending backtracking in the condition, as the condition
            // is considered to be a positive lookahead, which is an atomic zero-width assertion.
            condition = Child(0);
            condition.EliminateEndingBacktracking();

            return this;
        }

        /// <summary>
        /// Determines whether node can be switched to an atomic loop.  Subsequent is the node
        /// immediately after 'node'.
        /// </summary>
        private static bool CanBeMadeAtomic(RegexNode node, RegexNode subsequent, bool allowSubsequentIteration)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, just stop optimizing.
                return false;
            }

            // In most case, we'll simply check the node against whatever subsequent is.  However, in case
            // subsequent ends up being a loop with a min bound of 0, we'll also need to evaluate the node
            // against whatever comes after subsequent.  In that case, we'll walk the tree to find the
            // next subsequent, and we'll loop around against to perform the comparison again.
            while (true)
            {
                // Skip the successor down to the closest node that's guaranteed to follow it.
                int childCount;
                while ((childCount = subsequent.ChildCount()) > 0)
                {
                    Debug.Assert(subsequent.Kind != RegexNodeKind.Group);
                    switch (subsequent.Kind)
                    {
                        case RegexNodeKind.Concatenate:
                        case RegexNodeKind.Capture:
                        case RegexNodeKind.Atomic:
                        case RegexNodeKind.PositiveLookaround when (subsequent.Options & RegexOptions.RightToLeft) == 0: // only lookaheads, not lookbehinds (represented as RTL PositiveLookaround nodes)
                        case RegexNodeKind.Loop or RegexNodeKind.Lazyloop when subsequent.M > 0:
                            subsequent = subsequent.Child(0);
                            continue;
                    }

                    break;
                }

                // If the current node's options don't match the subsequent node, then we cannot make it atomic.
                // This applies to RightToLeft for lookbehinds, as well as patterns that enable/disable global flags in the middle of the pattern.
                if (node.Options != subsequent.Options)
                {
                    return false;
                }

                // If the successor is an alternation, all of its children need to be evaluated, since any of them
                // could come after this node.  If any of them fail the optimization, then the whole node fails.
                // This applies to expression conditionals as well, as long as they have both a yes and a no branch (if there's
                // only a yes branch, we'd need to also check whatever comes after the conditional).  It doesn't apply to
                // backreference conditionals, as the condition itself is unknown statically and could overlap with the
                // loop being considered for atomicity.
                switch (subsequent.Kind)
                {
                    case RegexNodeKind.Alternate:
                    case RegexNodeKind.ExpressionConditional when childCount == 3: // condition, yes, and no branch
                        for (int i = 0; i < childCount; i++)
                        {
                            if (!CanBeMadeAtomic(node, subsequent.Child(i), allowSubsequentIteration))
                            {
                                return false;
                            }
                        }
                        return true;
                }

                // If this node is a {one/notone/set}loop, see if it overlaps with its successor in the concatenation.
                // If it doesn't, then we can upgrade it to being a {one/notone/set}loopatomic.
                // Doing so avoids unnecessary backtracking.
                switch (node.Kind)
                {
                    case RegexNodeKind.Oneloop:
                        switch (subsequent.Kind)
                        {
                            case RegexNodeKind.One when node.Ch != subsequent.Ch:
                            case RegexNodeKind.Notone when node.Ch == subsequent.Ch:
                            case RegexNodeKind.Set when !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M > 0 && node.Ch != subsequent.Ch:
                            case RegexNodeKind.Notonelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                            case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                            case RegexNodeKind.Multi when node.Ch != subsequent.Str![0]:
                            case RegexNodeKind.End:
                            case RegexNodeKind.EndZ or RegexNodeKind.Eol when node.Ch != '\n':
                            case RegexNodeKind.Boundary when RegexCharClass.IsBoundaryWordChar(node.Ch):
                            case RegexNodeKind.NonBoundary when !RegexCharClass.IsBoundaryWordChar(node.Ch):
                            case RegexNodeKind.ECMABoundary when RegexCharClass.IsECMAWordChar(node.Ch):
                            case RegexNodeKind.NonECMABoundary when !RegexCharClass.IsECMAWordChar(node.Ch):
                                return true;

                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M == 0 && node.Ch != subsequent.Ch:
                            case RegexNodeKind.Notonelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic when subsequent.M == 0 && node.Ch == subsequent.Ch:
                            case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when subsequent.M == 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                                // The loop can be made atomic based on this subsequent node, but we'll need to evaluate the next one as well.
                                break;

                            default:
                                return false;
                        }
                        break;

                    case RegexNodeKind.Notoneloop:
                        switch (subsequent.Kind)
                        {
                            case RegexNodeKind.One when node.Ch == subsequent.Ch:
                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                            case RegexNodeKind.Multi when node.Ch == subsequent.Str![0]:
                            case RegexNodeKind.End:
                                return true;

                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M == 0 && node.Ch == subsequent.Ch:
                                // The loop can be made atomic based on this subsequent node, but we'll need to evaluate the next one as well.
                                break;

                            default:
                                return false;
                        }
                        break;

                    case RegexNodeKind.Setloop:
                        switch (subsequent.Kind)
                        {
                            case RegexNodeKind.One when !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                            case RegexNodeKind.Set when !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                            case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                            case RegexNodeKind.Multi when !RegexCharClass.CharInClass(subsequent.Str![0], node.Str!):
                            case RegexNodeKind.End:
                            case RegexNodeKind.EndZ or RegexNodeKind.Eol when !RegexCharClass.CharInClass('\n', node.Str!):
                            case RegexNodeKind.Boundary when node.Str is RegexCharClass.WordClass or RegexCharClass.DigitClass:
                            case RegexNodeKind.NonBoundary when node.Str is RegexCharClass.NotWordClass or RegexCharClass.NotDigitClass:
                            case RegexNodeKind.ECMABoundary when node.Str is RegexCharClass.ECMAWordClass or RegexCharClass.ECMADigitClass:
                            case RegexNodeKind.NonECMABoundary when node.Str is RegexCharClass.NotECMAWordClass or RegexCharClass.NotDigitClass:
                                return true;

                            case RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic when subsequent.M == 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                            case RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic when subsequent.M == 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                                // The loop can be made atomic based on this subsequent node, but we'll need to evaluate the next one as well.
                                break;

                            default:
                                return false;
                        }
                        break;

                    default:
                        return false;
                }

                // We only get here if the node could be made atomic based on subsequent but subsequent has a lower bound of zero
                // and thus we need to move subsequent to be the next node in sequence and loop around to try again.
                Debug.Assert(subsequent.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy);
                Debug.Assert(subsequent.M == 0);
                if (!allowSubsequentIteration)
                {
                    return false;
                }

                // To be conservative, we only walk up through a very limited set of constructs (even though we may have walked
                // down through more, like loops), looking for the next concatenation that we're not at the end of, at
                // which point subsequent becomes whatever node is next in that concatenation.
                while (true)
                {
                    RegexNode? parent = subsequent.Parent;
                    switch (parent?.Kind)
                    {
                        case RegexNodeKind.Atomic:
                        case RegexNodeKind.Alternate:
                        case RegexNodeKind.Capture:
                            subsequent = parent;
                            continue;

                        case RegexNodeKind.Concatenate:
                            var peers = (List<RegexNode>)parent.Children!;
                            int currentIndex = peers.IndexOf(subsequent);
                            Debug.Assert(currentIndex >= 0, "Node should have been in its parent's child list");
                            if (currentIndex + 1 == peers.Count)
                            {
                                subsequent = parent;
                                continue;
                            }
                            else
                            {
                                subsequent = peers[currentIndex + 1];
                                break;
                            }

                        case null:
                            // If we hit the root, we're at the end of the expression, at which point nothing could backtrack
                            // in and we can declare success.
                            return true;

                        default:
                            // Anything else, we don't know what to do, so we have to assume it could conflict with the loop.
                            return false;
                    }

                    break;
                }
            }
        }

        /// <summary>Computes a min bound on the required length of any string that could possibly match.</summary>
        /// <returns>The min computed length.  If the result is 0, there is no minimum we can enforce.</returns>
        /// <remarks>
        /// e.g. abc[def](ghijkl|mn) => 6
        /// </remarks>
        public int ComputeMinLength()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, assume there's no minimum we can enforce.
                return 0;
            }

            switch (Kind)
            {
                case RegexNodeKind.One:
                case RegexNodeKind.Notone:
                case RegexNodeKind.Set:
                    // Single character.
                    return 1;

                case RegexNodeKind.Multi:
                    // Every character in the string needs to match.
                    return Str!.Length;

                case RegexNodeKind.Notonelazy:
                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notoneloopatomic:
                case RegexNodeKind.Onelazy:
                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Oneloopatomic:
                case RegexNodeKind.Setlazy:
                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setloopatomic:
                    // One character repeated at least M times.
                    return M;

                case RegexNodeKind.Lazyloop:
                case RegexNodeKind.Loop:
                    // A node graph repeated at least M times.
                    return (int)Math.Min(int.MaxValue - 1, (long)M * Child(0).ComputeMinLength());

                case RegexNodeKind.Alternate:
                    // The minimum required length for any of the alternation's branches.
                    {
                        int childCount = ChildCount();
                        Debug.Assert(childCount >= 2);
                        int min = Child(0).ComputeMinLength();
                        for (int i = 1; i < childCount && min > 0; i++)
                        {
                            min = Math.Min(min, Child(i).ComputeMinLength());
                        }
                        return min;
                    }

                case RegexNodeKind.BackreferenceConditional:
                    // Minimum of its yes and no branches.  The backreference doesn't add to the length.
                    return Math.Min(Child(0).ComputeMinLength(), Child(1).ComputeMinLength());

                case RegexNodeKind.ExpressionConditional:
                    // Minimum of its yes and no branches.  The condition is a zero-width assertion.
                    return Math.Min(Child(1).ComputeMinLength(), Child(2).ComputeMinLength());

                case RegexNodeKind.Concatenate:
                    // The sum of all of the concatenation's children.
                    {
                        long sum = 0;
                        int childCount = ChildCount();
                        for (int i = 0; i < childCount; i++)
                        {
                            sum += Child(i).ComputeMinLength();
                        }
                        return (int)Math.Min(int.MaxValue - 1, sum);
                    }

                case RegexNodeKind.Atomic:
                case RegexNodeKind.Capture:
                case RegexNodeKind.Group:
                    // For groups, we just delegate to the sole child.
                    Debug.Assert(ChildCount() == 1);
                    return Child(0).ComputeMinLength();

                case RegexNodeKind.Empty:
                case RegexNodeKind.Nothing:
                case RegexNodeKind.UpdateBumpalong:
                // Nothing to match. In the future, we could potentially use Nothing to say that the min length
                // is infinite, but that would require a different structure, as that would only apply if the
                // Nothing match is required in all cases (rather than, say, as one branch of an alternation).
                case RegexNodeKind.Beginning:
                case RegexNodeKind.Bol:
                case RegexNodeKind.Boundary:
                case RegexNodeKind.ECMABoundary:
                case RegexNodeKind.End:
                case RegexNodeKind.EndZ:
                case RegexNodeKind.Eol:
                case RegexNodeKind.NonBoundary:
                case RegexNodeKind.NonECMABoundary:
                case RegexNodeKind.Start:
                case RegexNodeKind.NegativeLookaround:
                case RegexNodeKind.PositiveLookaround:
                // Zero-width
                case RegexNodeKind.Backreference:
                    // Requires matching data available only at run-time.  In the future, we could choose to find
                    // and follow the capture group this aligns with, while being careful not to end up in an
                    // infinite cycle.
                    return 0;

                default:
                    Debug.Fail($"Unknown node: {Kind}");
                    goto case RegexNodeKind.Empty;
            }
        }

        /// <summary>Computes a maximum length of any string that could possibly match.</summary>
        /// <returns>The maximum length of any string that could possibly match, or null if the length may not always be the same.</returns>
        /// <remarks>
        /// e.g. abc[def](gh|ijklmnop) => 12
        /// </remarks>
        public int? ComputeMaxLength()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, assume there's no minimum we can enforce.
                return null;
            }

            switch (Kind)
            {
                case RegexNodeKind.One:
                case RegexNodeKind.Notone:
                case RegexNodeKind.Set:
                    // Single character.
                    return 1;

                case RegexNodeKind.Multi:
                    // Every character in the string needs to match.
                    return Str!.Length;

                case RegexNodeKind.Notonelazy or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or
                     RegexNodeKind.Onelazy or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or
                     RegexNodeKind.Setlazy or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic:
                    // Return the max number of iterations if there's an upper bound, or null if it's infinite
                    return N == int.MaxValue ? null : N;

                case RegexNodeKind.Loop or RegexNodeKind.Lazyloop:
                    if (N != int.MaxValue)
                    {
                        // A node graph repeated a fixed number of times
                        if (Child(0).ComputeMaxLength() is int childMaxLength)
                        {
                            long maxLength = (long)N * childMaxLength;
                            if (maxLength < int.MaxValue)
                            {
                                return (int)maxLength;
                            }
                        }
                    }
                    return null;

                case RegexNodeKind.Alternate:
                    // The maximum length of any child branch, as long as they all have one.
                    {
                        int childCount = ChildCount();
                        Debug.Assert(childCount >= 2);
                        if (Child(0).ComputeMaxLength() is not int maxLength)
                        {
                            return null;
                        }

                        for (int i = 1; i < childCount; i++)
                        {
                            if (Child(i).ComputeMaxLength() is not int next)
                            {
                                return null;
                            }

                            maxLength = Math.Max(maxLength, next);
                        }

                        return maxLength;
                    }

                case RegexNodeKind.BackreferenceConditional:
                case RegexNodeKind.ExpressionConditional:
                    // The maximum length of either child branch, as long as they both have one.. The condition for an expression conditional is a zero-width assertion.
                    {
                        int i = Kind == RegexNodeKind.BackreferenceConditional ? 0 : 1;
                        return Child(i).ComputeMaxLength() is int yes && Child(i + 1).ComputeMaxLength() is int no ?
                            Math.Max(yes, no) :
                            null;
                    }

                case RegexNodeKind.Concatenate:
                    // The sum of all of the concatenation's children's max lengths, as long as they all have one.
                    {
                        long sum = 0;
                        int childCount = ChildCount();
                        for (int i = 0; i < childCount; i++)
                        {
                            if (Child(i).ComputeMaxLength() is not int length)
                            {
                                return null;
                            }
                            sum += length;
                        }

                        if (sum < int.MaxValue)
                        {
                            return (int)sum;
                        }

                        return null;
                    }

                case RegexNodeKind.Atomic:
                case RegexNodeKind.Capture:
                    // For groups, we just delegate to the sole child.
                    Debug.Assert(ChildCount() == 1);
                    return Child(0).ComputeMaxLength();

                case RegexNodeKind.Empty:
                case RegexNodeKind.Nothing:
                case RegexNodeKind.UpdateBumpalong:
                case RegexNodeKind.Beginning:
                case RegexNodeKind.Bol:
                case RegexNodeKind.Boundary:
                case RegexNodeKind.ECMABoundary:
                case RegexNodeKind.End:
                case RegexNodeKind.EndZ:
                case RegexNodeKind.Eol:
                case RegexNodeKind.NonBoundary:
                case RegexNodeKind.NonECMABoundary:
                case RegexNodeKind.Start:
                case RegexNodeKind.PositiveLookaround:
                case RegexNodeKind.NegativeLookaround:
                    // Zero-width
                    return 0;

                case RegexNodeKind.Backreference:
                    // Requires matching data available only at run-time.  In the future, we could choose to find
                    // and follow the capture group this aligns with, while being careful not to end up in an
                    // infinite cycle.
                    return null;

                default:
                    Debug.Fail($"Unknown node: {Kind}");
                    goto case RegexNodeKind.Empty;
            }
        }

        /// <summary>
        /// Determines whether the specified child index of a concatenation begins a sequence whose values
        /// should be used to perform an ordinal case-insensitive comparison.
        /// </summary>
        /// <param name="childIndex">The index of the child with which to start the sequence.</param>
        /// <param name="exclusiveChildBound">The exclusive upper bound on the child index to iterate to.</param>
        /// <param name="nodesConsumed">How many nodes make up the sequence, if any.</param>
        /// <param name="caseInsensitiveString">The string to use for an ordinal case-insensitive comparison, if any.</param>
        /// <returns>true if a sequence was found; otherwise, false.</returns>
        public bool TryGetOrdinalCaseInsensitiveString(int childIndex, int exclusiveChildBound, out int nodesConsumed, [NotNullWhen(true)] out string? caseInsensitiveString)
        {
            Debug.Assert(Kind == RegexNodeKind.Concatenate, $"Expected Concatenate, got {Kind}");

            var vsb = new ValueStringBuilder(stackalloc char[32]);

            // We're looking in particular for sets of ASCII characters, so we focus only on sets with two characters in them, e.g. [Aa].
            Span<char> twoChars = stackalloc char[2];

            // Iterate from the child index to the exclusive upper bound.
            int i = childIndex;
            for (; i < exclusiveChildBound; i++)
            {
                RegexNode child = Child(i);

                if (child.Kind is RegexNodeKind.One)
                {
                    // We only want to include ASCII characters, and only if they don't participate in case conversion
                    // such that they only case to themselves and nothing other cases to them.  Otherwise, including
                    // them would potentially cause us to match against things not allowed by the pattern.
                    if (child.Ch >= 128 ||
                        RegexCharClass.ParticipatesInCaseConversion(child.Ch))
                    {
                        break;
                    }

                    vsb.Append(child.Ch);
                }
                else if (child.Kind is RegexNodeKind.Multi)
                {
                    // As with RegexNodeKind.One, the string needs to be composed solely of ASCII characters that
                    // don't participate in case conversion.
                    if (!RegexCharClass.IsAscii(child.Str.AsSpan()) ||
                        RegexCharClass.ParticipatesInCaseConversion(child.Str.AsSpan()))
                    {
                        break;
                    }

                    vsb.Append(child.Str);
                }
                else if (child.Kind is RegexNodeKind.Set ||
                         (child.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic && child.M == child.N))
                {
                    // In particular we want to look for sets that contain only the upper and lowercase variant
                    // of the same ASCII letter.
                    if (RegexCharClass.IsNegated(child.Str!) ||
                        RegexCharClass.GetSetChars(child.Str!, twoChars) != 2 ||
                        twoChars[0] >= 128 ||
                        twoChars[1] >= 128 ||
                        twoChars[0] == twoChars[1] ||
                        !char.IsLetter(twoChars[0]) ||
                        !char.IsLetter(twoChars[1]) ||
                        ((twoChars[0] | 0x20) != (twoChars[1] | 0x20)))
                    {
                        break;
                    }

                    vsb.Append((char)(twoChars[0] | 0x20), child.Kind is RegexNodeKind.Set ? 1 : child.M);
                }
                else
                {
                    break;
                }
            }

            // If we found at least two characters, consider it a sequence found.  It's possible
            // they all came from the same node, so this could be a sequence of just one node.
            if (vsb.Length >= 2)
            {
                caseInsensitiveString = vsb.ToString();
                nodesConsumed = i - childIndex;
                return true;
            }

            // No sequence found.
            caseInsensitiveString = null;
            nodesConsumed = 0;
            vsb.Dispose();
            return false;
        }

        /// <summary>
        /// Determine whether the specified child node is the beginning of a sequence that can
        /// trivially have length checks combined in order to avoid bounds checks.
        /// </summary>
        /// <param name="childIndex">The starting index of the child to check.</param>
        /// <param name="requiredLength">The sum of all the fixed lengths for the nodes in the sequence.</param>
        /// <param name="exclusiveEnd">The index of the node just after the last one in the sequence.</param>
        /// <returns>true if more than one node can have their length checks combined; otherwise, false.</returns>
        /// <remarks>
        /// There are additional node types for which we can prove a fixed length, e.g. examining all branches
        /// of an alternation and returning true if all their lengths are equal.  However, the primary purpose
        /// of this method is to avoid bounds checks by consolidating length checks that guard accesses to
        /// strings/spans for which the JIT can see a fixed index within bounds, and alternations employ
        /// patterns that defeat that (e.g. reassigning the span in question).  As such, the implementation
        /// remains focused on only a core subset of nodes that are a) likely to be used in concatenations and
        /// b) employ simple patterns of checks.
        /// </remarks>
        public bool TryGetJoinableLengthCheckChildRange(int childIndex, out int requiredLength, out int exclusiveEnd)
        {
            Debug.Assert(Kind == RegexNodeKind.Concatenate, $"Expected Concatenate, got {Kind}");

            static bool CanJoinLengthCheck(RegexNode node) => node.Kind switch
            {
                RegexNodeKind.One or RegexNodeKind.Notone or RegexNodeKind.Set => true,
                RegexNodeKind.Multi => true,
                RegexNodeKind.Oneloop or RegexNodeKind.Onelazy or RegexNodeKind.Oneloopatomic or
                    RegexNodeKind.Notoneloop or RegexNodeKind.Notonelazy or RegexNodeKind.Notoneloopatomic or
                    RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic
                    when node.M == node.N => true,
                _ => false,
            };

            RegexNode child = Child(childIndex);
            if (CanJoinLengthCheck(child))
            {
                requiredLength = child.ComputeMinLength();

                int childCount = ChildCount();
                for (exclusiveEnd = childIndex + 1; exclusiveEnd < childCount; exclusiveEnd++)
                {
                    child = Child(exclusiveEnd);
                    if (!CanJoinLengthCheck(child))
                    {
                        break;
                    }

                    requiredLength += child.ComputeMinLength();
                }

                if (exclusiveEnd - childIndex > 1)
                {
                    return true;
                }
            }

            requiredLength = 0;
            exclusiveEnd = 0;
            return false;
        }

        public RegexNode MakeQuantifier(bool lazy, int min, int max)
        {
            // Certain cases of repeaters (min == max) can be handled specially
            if (min == max)
            {
                switch (max)
                {
                    case 0:
                        // The node is repeated 0 times, so it's actually empty.
                        return new RegexNode(RegexNodeKind.Empty, Options);

                    case 1:
                        // The node is repeated 1 time, so it's not actually a repeater.
                        return this;

                    case <= MultiVsRepeaterLimit when Kind == RegexNodeKind.One:
                        // The same character is repeated a fixed number of times, so it's actually a multi.
                        // While this could remain a repeater, multis are more readily optimized later in
                        // processing. The counts used here in real-world expressions are invariably small (e.g. 4),
                        // but we set an upper bound just to avoid creating really large strings.
                        Debug.Assert(max >= 2);
                        Kind = RegexNodeKind.Multi;
                        Str = new string(Ch, max);
                        Ch = '\0';
                        return this;
                }
            }

            switch (Kind)
            {
                case RegexNodeKind.One:
                case RegexNodeKind.Notone:
                case RegexNodeKind.Set:
                    MakeRep(lazy ? RegexNodeKind.Onelazy : RegexNodeKind.Oneloop, min, max);
                    return this;

                default:
                    var result = new RegexNode(lazy ? RegexNodeKind.Lazyloop : RegexNodeKind.Loop, Options, min, max);
                    result.AddChild(this);
                    return result;
            }
        }

        public void AddChild(RegexNode newChild)
        {
            newChild.Parent = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Parent = this; // in case Reduce returns a different node that needs to be reparented

            if (Children is null)
            {
                Children = newChild;
            }
            else if (Children is RegexNode currentChild)
            {
                Children = new List<RegexNode>() { currentChild, newChild };
            }
            else
            {
                ((List<RegexNode>)Children).Add(newChild);
            }
        }

        public void InsertChild(int index, RegexNode newChild)
        {
            Debug.Assert(Children is List<RegexNode>);

            newChild.Parent = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Parent = this; // in case Reduce returns a different node that needs to be reparented

            ((List<RegexNode>)Children).Insert(index, newChild);
        }

        public void ReplaceChild(int index, RegexNode newChild)
        {
            Debug.Assert(Children != null);
            Debug.Assert(index < ChildCount());

            newChild.Parent = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Parent = this; // in case Reduce returns a different node that needs to be reparented

            if (Children is RegexNode)
            {
                Children = newChild;
            }
            else
            {
                ((List<RegexNode>)Children)[index] = newChild;
            }
        }

        public RegexNode Child(int i) => Children is RegexNode child ? child : ((List<RegexNode>)Children!)[i];

        public int ChildCount()
        {
            if (Children is null)
            {
                return 0;
            }

            if (Children is List<RegexNode> children)
            {
                return children.Count;
            }

            Debug.Assert(Children is RegexNode);
            return 1;
        }

        // Determines whether the node supports a compilation strategy based on walking the node tree.
        // Also returns a human-readable string to explain the reason (it will be emitted by the source generator, hence
        // there's no need to localize).
        internal bool SupportsCompilation([NotNullWhen(false)] out string? reason)
        {
            if ((Options & RegexOptions.NonBacktracking) != 0)
            {
                reason = "RegexOptions.NonBacktracking isn't supported";
                return false;
            }

            if (ExceedsMaxDepthAllowedDepth(this, allowedDepth: 40))
            {
                // For the source generator, deep RegexNode trees can result in emitting C# code that exceeds C# compiler
                // limitations, leading to "CS8078: An expression is too long or complex to compile". As such, we place
                // an artificial limit on max tree depth in order to mitigate such issues. The allowed depth can be tweaked
                // as needed; its exceedingly rare to find expressions with such deep trees. And while RegexCompiler doesn't
                // have to deal with C# compiler limitations, we still want to limit max tree depth as we want to limit
                // how deep recursion we'll employ as part of code generation.
                reason = "the expression may result exceeding run-time or compiler limits";
                return false;
            }

            // Supported.
            reason = null;
            return true;

            static bool ExceedsMaxDepthAllowedDepth(RegexNode node, int allowedDepth)
            {
                if (allowedDepth <= 0)
                {
                    return true;
                }

                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    if (ExceedsMaxDepthAllowedDepth(node.Child(i), allowedDepth - 1))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Gets whether the node is a Set/Setloop/Setloopatomic/Setlazy node.</summary>
        public bool IsSetFamily => Kind is RegexNodeKind.Set or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy;

        /// <summary>Gets whether the node is a One/Oneloop/Oneloopatomic/Onelazy node.</summary>
        public bool IsOneFamily => Kind is RegexNodeKind.One or RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy;

        /// <summary>Gets whether the node is a Notone/Notoneloop/Notoneloopatomic/Notonelazy node.</summary>
        public bool IsNotoneFamily => Kind is RegexNodeKind.Notone or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Notonelazy;

#if DEBUG
        [ExcludeFromCodeCoverage] // Used only for debugging assistance
        public override string ToString()
        {
            RegexNode? curNode = this;
            int curChild = 0;
            var sb = new StringBuilder().AppendLine(curNode.Describe());
            var stack = new List<int>();
            while (true)
            {
                if (curChild < curNode!.ChildCount())
                {
                    stack.Add(curChild + 1);
                    curNode = curNode.Child(curChild);
                    curChild = 0;

                    sb.Append(new string(' ', stack.Count * 2)).Append(curNode.Describe()).AppendLine();
                }
                else
                {
                    if (stack.Count == 0)
                    {
                        break;
                    }

                    curChild = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    curNode = curNode.Parent;
                }
            }

            return sb.ToString();
        }

        [ExcludeFromCodeCoverage] // Used only for debugging assistance
        private string Describe()
        {
            var sb = new StringBuilder(Kind.ToString());

            if ((Options & RegexOptions.ExplicitCapture) != 0) sb.Append("-C");
            if ((Options & RegexOptions.IgnoreCase) != 0) sb.Append("-I");
            if ((Options & RegexOptions.RightToLeft) != 0) sb.Append("-L");
            if ((Options & RegexOptions.Multiline) != 0) sb.Append("-M");
            if ((Options & RegexOptions.Singleline) != 0) sb.Append("-S");
            if ((Options & RegexOptions.IgnorePatternWhitespace) != 0) sb.Append("-X");
            if ((Options & RegexOptions.ECMAScript) != 0) sb.Append("-E");

            switch (Kind)
            {
                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Oneloopatomic:
                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notoneloopatomic:
                case RegexNodeKind.Onelazy:
                case RegexNodeKind.Notonelazy:
                case RegexNodeKind.One:
                case RegexNodeKind.Notone:
                    sb.Append(" '").Append(RegexCharClass.DescribeChar(Ch)).Append('\'');
                    break;
                case RegexNodeKind.Capture:
                    sb.Append(' ').Append($"index = {M}");
                    if (N != -1)
                    {
                        sb.Append($", unindex = {N}");
                    }
                    break;
                case RegexNodeKind.Backreference:
                case RegexNodeKind.BackreferenceConditional:
                    sb.Append(' ').Append($"index = {M}");
                    break;
                case RegexNodeKind.Multi:
                    sb.Append(" \"").Append(Str).Append('"');
                    break;
                case RegexNodeKind.Set:
                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setloopatomic:
                case RegexNodeKind.Setlazy:
                    sb.Append(' ').Append(RegexCharClass.DescribeSet(Str!));
                    break;
            }

            switch (Kind)
            {
                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Oneloopatomic:
                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notoneloopatomic:
                case RegexNodeKind.Onelazy:
                case RegexNodeKind.Notonelazy:
                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setloopatomic:
                case RegexNodeKind.Setlazy:
                case RegexNodeKind.Loop:
                case RegexNodeKind.Lazyloop:
                    sb.Append(
                        (M == 0 && N == int.MaxValue) ? "*" :
                        (M == 0 && N == 1) ? "?" :
                        (M == 1 && N == int.MaxValue) ? "+" :
                        (N == int.MaxValue) ? $"{{{M}, *}}" :
                        (N == M) ? $"{{{M}}}" :
                        $"{{{M}, {N}}}");
                    break;
            }

            return sb.ToString();
        }
#endif
    }
}
