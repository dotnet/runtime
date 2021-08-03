// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This RegexNode class is internal to the Regex package.
// It is built into a parsed tree for a regular expression.

// Implementation notes:
//
// Since the node tree is a temporary data structure only used
// during compilation of the regexp to integer codes, it's
// designed for clarity and convenience rather than
// space efficiency.
//
// RegexNodes are built into a tree, linked by the _children list.
// Each node also has a _parent and _ichild member indicating
// its parent and which child # it is in its parent's list.
//
// RegexNodes come in as many types as there are constructs in
// a regular expression, for example, "concatenate", "alternate",
// "one", "rept", "group". There are also node types for basic
// peephole optimizations, e.g., "onerep", "notsetrep", etc.
//
// Because perl 5 allows "lookback" groups that scan backwards,
// each node also gets a "direction". Normally the value of
// boolean _backward = false.
//
// During parsing, top-level nodes are also stacked onto a parse
// stack (a stack of trees). For this purpose we have a _next
// pointer. [Note that to save a few bytes, we could overload the
// _parent pointer instead.]
//
// On the parse stack, each tree has a "role" - basically, the
// nonterminal in the grammar that the parser has currently
// assigned to the tree. That code is stored in _role.
//
// Finally, some of the different kinds of nodes have data.
// Two integers (for the looping constructs) are stored in
// _operands, an object (either a string or a set)
// is stored in _data

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexNode
    {
        // RegexNode types

        // The following are leaves, and correspond to primitive operations

        public const int Oneloop = RegexCode.Oneloop;                 // c,n      a*
        public const int Notoneloop = RegexCode.Notoneloop;           // c,n      .*
        public const int Setloop = RegexCode.Setloop;                 // set,n    \d*

        public const int Onelazy = RegexCode.Onelazy;                 // c,n      a*?
        public const int Notonelazy = RegexCode.Notonelazy;           // c,n      .*?
        public const int Setlazy = RegexCode.Setlazy;                 // set,n    \d*?

        public const int One = RegexCode.One;                         // char     a
        public const int Notone = RegexCode.Notone;                   // char     . [^a]
        public const int Set = RegexCode.Set;                         // set      [a-z] \w \s \d

        public const int Multi = RegexCode.Multi;                     // string   abcdef
        public const int Ref = RegexCode.Ref;                         // index    \1

        public const int Bol = RegexCode.Bol;                         //          ^
        public const int Eol = RegexCode.Eol;                         //          $
        public const int Boundary = RegexCode.Boundary;               //          \b
        public const int NonBoundary = RegexCode.NonBoundary;         //          \B
        public const int ECMABoundary = RegexCode.ECMABoundary;       // \b
        public const int NonECMABoundary = RegexCode.NonECMABoundary; // \B
        public const int Beginning = RegexCode.Beginning;             //          \A
        public const int Start = RegexCode.Start;                     //          \G
        public const int EndZ = RegexCode.EndZ;                       //          \Z
        public const int End = RegexCode.End;                         //          \z

        public const int Oneloopatomic = RegexCode.Oneloopatomic;        // c,n      (?> a*)
        public const int Notoneloopatomic = RegexCode.Notoneloopatomic;  // c,n      (?> .*)
        public const int Setloopatomic = RegexCode.Setloopatomic;        // set,n    (?> \d*)
        public const int UpdateBumpalong = RegexCode.UpdateBumpalong;

        // Interior nodes do not correspond to primitive operations, but
        // control structures compositing other operations

        // Concat and alternate take n children, and can run forward or backwards

        public const int Nothing = 22;                                //          []
        public const int Empty = 23;                                  //          ()

        public const int Alternate = 24;                              //          a|b
        public const int Concatenate = 25;                            //          ab

        public const int Loop = 26;                                   // m,x      * + ? {,}
        public const int Lazyloop = 27;                               // m,x      *? +? ?? {,}?

        public const int Capture = 28;                                // n        ()         - capturing group
        public const int Group = 29;                                  //          (?:)       - noncapturing group
        public const int Require = 30;                                //          (?=) (?<=) - lookahead and lookbehind assertions
        public const int Prevent = 31;                                //          (?!) (?<!) - negative lookahead and lookbehind assertions
        public const int Atomic = 32;                                 //          (?>)       - atomic subexpression
        public const int Testref = 33;                                //          (?(n) | )  - alternation, reference
        public const int Testgroup = 34;                              //          (?(...) | )- alternation, expression

        private const uint DefaultMaxRecursionDepth = 20; // arbitrary cut-off to avoid unbounded recursion

        private object? Children;
        public int Type { get; private set; }
        public string? Str { get; private set; }
        public char Ch { get; private set; }
        public int M { get; private set; }
        public int N { get; private set; }
        public RegexOptions Options;
        public RegexNode? Next;

        public RegexNode(int type, RegexOptions options)
        {
            Type = type;
            Options = options;
        }

        public RegexNode(int type, RegexOptions options, char ch)
        {
            Type = type;
            Options = options;
            Ch = ch;
        }

        public RegexNode(int type, RegexOptions options, string str)
        {
            Type = type;
            Options = options;
            Str = str;
        }

        public RegexNode(int type, RegexOptions options, int m)
        {
            Type = type;
            Options = options;
            M = m;
        }

        public RegexNode(int type, RegexOptions options, int m, int n)
        {
            Type = type;
            Options = options;
            M = m;
            N = n;
        }

        public bool UseOptionR()
        {
            return (Options & RegexOptions.RightToLeft) != 0;
        }

        public RegexNode ReverseLeft()
        {
            if (UseOptionR() && Type == Concatenate && ChildCount() > 1)
            {
                ((List<RegexNode>)Children!).Reverse();
            }

            return this;
        }

        /// <summary>
        /// Pass type as OneLazy or OneLoop
        /// </summary>
        private void MakeRep(int type, int min, int max)
        {
            Type += (type - One);
            M = min;
            N = max;
        }

        private void MakeLoopAtomic()
        {
            switch (Type)
            {
                case Oneloop:
                    Type = Oneloopatomic;
                    break;
                case Notoneloop:
                    Type = Notoneloopatomic;
                    break;
                default:
#if DEBUG
                    Debug.Assert(Type == Setloop, $"Unexpected type: {TypeName}");
#endif
                    Type = Setloopatomic;
                    break;
            }
        }

#if DEBUG
        /// <summary>Validate invariants the rest of the implementation relies on for processing fully-built trees.</summary>
        [Conditional("DEBUG")]
        private void ValidateFinalTreeInvariants()
        {
            var toExamine = new Stack<RegexNode>();
            toExamine.Push(this);
            while (toExamine.TryPop(out RegexNode? node))
            {
                // Validate that we never see certain node types.
                Debug.Assert(Type != Group, "All Group nodes should have been removed.");

                // Validate expected child counts.
                int childCount = node.ChildCount();
                switch (node.Type)
                {
                    case Beginning:
                    case Bol:
                    case Boundary:
                    case ECMABoundary:
                    case Empty:
                    case End:
                    case EndZ:
                    case Eol:
                    case Multi:
                    case NonBoundary:
                    case NonECMABoundary:
                    case Nothing:
                    case Notone:
                    case Notonelazy:
                    case Notoneloop:
                    case Notoneloopatomic:
                    case One:
                    case Onelazy:
                    case Oneloop:
                    case Oneloopatomic:
                    case Ref:
                    case Set:
                    case Setlazy:
                    case Setloop:
                    case Setloopatomic:
                    case Start:
                    case UpdateBumpalong:
                        Debug.Assert(childCount == 0, $"Expected zero children for {node.TypeName}, got {childCount}.");
                        break;

                    case Atomic:
                    case Capture:
                    case Lazyloop:
                    case Loop:
                    case Prevent:
                    case Require:
                        Debug.Assert(childCount == 1, $"Expected one and only one child for {node.TypeName}, got {childCount}.");
                        toExamine.Push(node.Child(0));
                        break;

                    case Testref:
                    case Testgroup:
                        Debug.Assert(childCount >= 1, $"Expected at least one child for {node.TypeName}, got {childCount}.");
                        for (int i = 0; i < childCount; i++)
                        {
                            toExamine.Push(node.Child(i));
                        }
                        break;

                    case Concatenate:
                    case Alternate:
                        Debug.Assert(childCount >= 2, $"Expected at least two children for {node.TypeName}, got {childCount}.");
                        for (int i = 0; i < childCount; i++)
                        {
                            toExamine.Push(node.Child(i));
                        }
                        break;
                }

                // Validate node configuration.
                switch (node.Type)
                {
                    case Multi:
                    case Set:
                    case Setloop:
                    case Setloopatomic:
                    case Setlazy:
                        Debug.Assert(!string.IsNullOrEmpty(node.Str), $"Expected non-null, non-empty string for {node.TypeName}.");
                        break;

                    default:
                        Debug.Assert(node.Str is null, $"Expected null string for {node.TypeName}, got \"{node.Str}\".");
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
            Debug.Assert(rootNode.Type == Capture);
            Debug.Assert(rootNode.Next is null);
            Debug.Assert(rootNode.ChildCount() == 1);

            if ((Options & RegexOptions.RightToLeft) == 0) // only apply optimization when LTR to avoid needing additional code for the rarer RTL case
            {
                // Optimization: backtracking removal at expression end.
                // If we find backtracking construct at the end of the regex, we can instead make it non-backtracking,
                // since nothing would ever backtrack into it anyway.  Doing this then makes the construct available
                // to implementations that don't support backtracking.
                EliminateEndingBacktracking(rootNode.Child(0), DefaultMaxRecursionDepth);

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
                // apply this optimization to non-atomic loops. Even though backtracking could be necessary, such backtracking would be handled within the processing
                // of a single starting position.
                {
                    RegexNode node = rootNode.Child(0); // skip implicit root capture node
                    while (true)
                    {
                        switch (node.Type)
                        {
                            case Atomic:
                            case Concatenate:
                                node = node.Child(0);
                                continue;

                            case Oneloop when node.N == int.MaxValue:
                            case Oneloopatomic when node.N == int.MaxValue:
                            case Notoneloop when node.N == int.MaxValue:
                            case Notoneloopatomic when node.N == int.MaxValue:
                            case Setloop when node.N == int.MaxValue:
                            case Setloopatomic when node.N == int.MaxValue:
                                RegexNode? parent = node.Next;
                                if (parent != null && parent.Type == Concatenate)
                                {
                                    parent.InsertChild(1, new RegexNode(UpdateBumpalong, node.Options));
                                }
                                break;
                        }

                        break;
                    }
                }
            }

            // Optimization: Unnecessary root atomic.
            // If the root node under the implicit Capture is an Atomic, the Atomic is useless as there's nothing
            // to backtrack into it, so we can remove it.
            while (rootNode.Child(0).Type == Atomic)
            {
                rootNode.ReplaceChild(0, rootNode.Child(0).Child(0));
            }

            // Done optimizing.  Return the final tree.
#if DEBUG
            rootNode.ValidateFinalTreeInvariants();
#endif
            return rootNode;
        }

        /// <summary>Converts nodes at the end of the specified node tree to be atomic.</summary>
        /// <remarks>
        /// The correctness of this optimization depends on nothing being able to backtrack into
        /// the provided node.  That means it must be at the root of the overall expression, or
        /// it must be an Atomic node that nothing will backtrack into by the very nature of Atomic.
        /// </remarks>
        private static void EliminateEndingBacktracking(RegexNode node, uint maxDepth)
        {
            if (maxDepth == 0)
            {
                return;
            }

            // Walk the tree starting from the provided node.
            while (true)
            {
                switch (node.Type)
                {
                    // {One/Notone/Set}loops can be upgraded to {One/Notone/Set}loopatomic nodes,
                    // e.g. [abc]* => (?>[abc]*)
                    case Oneloop:
                    case Notoneloop:
                    case Setloop:
                        node.MakeLoopAtomic();
                        break;

                    case Capture:
                    case Concatenate:
                        // For Capture and Concatenate, we just recur into their last child (only child in the case
                        // of Capture).  However, if the child is Alternate, Loop, and Lazyloop, we can also make the
                        // node itself atomic by wrapping it in an Atomic node. Since we later check to see whether a
                        // node is atomic based on its parent or grandparent, we don't bother wrapping such a node in
                        // an Atomic one if its grandparent is already Atomic.
                        // e.g. [xyz](?:abc|def) => [xyz](?>abc|def)
                        RegexNode existingChild = node.Child(node.ChildCount() - 1);
                        if ((existingChild.Type == Alternate || existingChild.Type == Loop || existingChild.Type == Lazyloop) &&
                            (node.Next is null || node.Next.Type != Atomic)) // validate grandparent isn't atomic
                        {
                            var atomic = new RegexNode(Atomic, existingChild.Options);
                            atomic.AddChild(existingChild);
                            node.ReplaceChild(node.ChildCount() - 1, atomic);
                        }
                        node = existingChild;
                        continue;

                    // For alternate, we can recur into each branch separately.  We use this iteration for the first branch.
                    // e.g. abc*|def* => ab(?>c*)|de(?>f*)
                    case Alternate:
                        {
                            int branches = node.ChildCount();
                            for (int i = 1; i < branches; i++)
                            {
                                EliminateEndingBacktracking(node.Child(i), maxDepth - 1);
                            }
                        }
                        node = node.Child(0);
                        continue;

                    // For Loop, we search to see if there's a viable last expression, and iff there
                    // is we recur into processing it.
                    // e.g. (?:abc*)* => (?:ab(?>c*))*
                    case Loop:
                        {
                            RegexNode? loopDescendent = FindLastExpressionInLoopForAutoAtomic(node, maxDepth - 1);
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

        /// <summary>Whether this node is considered to be atomic based on its parent.</summary>
        /// <remarks>
        /// This is used to determine whether additional atomic nodes may be valuable to
        /// be introduced into the tree.  It should not be used to determine for sure whether
        /// a node will be backtracked into.
        /// </remarks>
        public bool IsAtomicByParent()
        {
            RegexNode? next = Next;
            if (next is null) return false;
            if (next.Type == Atomic) return true;

            // We only walk up one group as a balance between optimization and cost.
            if ((next.Type != Concatenate && next.Type != Capture) ||
                next.Child(next.ChildCount() - 1) != this)
            {
                return false;
            }

            next = next.Next;
            return next != null && next.Type == Atomic;
        }

        /// <summary>
        /// Removes redundant nodes from the subtree, and returns an optimized subtree.
        /// </summary>
        private RegexNode Reduce()
        {
            switch (Type)
            {
                case Alternate:
                    return ReduceAlternation();

                case Concatenate:
                    return ReduceConcatenation();

                case Loop:
                case Lazyloop:
                    return ReduceLoops();

                case Atomic:
                    return ReduceAtomic();

                case Group:
                    return ReduceGroup();

                case Set:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                    return ReduceSet();

                default:
                    return this;
            }
        }

        /// <summary>Remove an unnecessary Concatenation or Alternation node</summary>
        /// <remarks>
        /// Simple optimization for a concatenation or alternation:
        /// - if the node has only one child, use it instead
        /// - if the node has zero children, turn it into an empty with the specified empty type
        /// </remarks>
        private RegexNode ReplaceNodeIfUnnecessary(int emptyTypeIfNoChildren)
        {
            Debug.Assert(
                (Type == Alternate && emptyTypeIfNoChildren == Nothing) ||
                (Type == Concatenate && emptyTypeIfNoChildren == Empty));

            return ChildCount() switch
            {
                0 => new RegexNode(emptyTypeIfNoChildren, Options),
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
            Debug.Assert(Type == Group);

            RegexNode u = this;
            while (u.Type == Group)
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
            Debug.Assert(Type == Atomic);
            Debug.Assert(ChildCount() == 1);

            RegexNode atomic = this;
            RegexNode child = Child(0);
            while (child.Type == Atomic)
            {
                atomic = child;
                child = atomic.Child(0);
            }

            switch (child.Type)
            {
                // If the child is already atomic, we can just remove the atomic node.
                case Oneloopatomic:
                case Notoneloopatomic:
                case Setloopatomic:
                    return child;

                // If an atomic subexpression contains only a {one/notone/set}loop,
                // change it to be an {one/notone/set}loopatomic and remove the atomic node.
                case Oneloop:
                case Notoneloop:
                case Setloop:
                    child.MakeLoopAtomic();
                    return child;

                // For everything else, try to reduce ending backtracking of the last contained expression.
                default:
                    EliminateEndingBacktracking(child, DefaultMaxRecursionDepth);
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
            Debug.Assert(Type == Loop || Type == Lazyloop);

            RegexNode u = this;
            int type = Type;

            int min = M;
            int max = N;

            while (u.ChildCount() > 0)
            {
                RegexNode child = u.Child(0);

                // multiply reps of the same type only
                if (child.Type != type)
                {
                    bool valid = false;
                    if (type == Loop)
                    {
                        switch (child.Type)
                        {
                            case Oneloop:
                            case Oneloopatomic:
                            case Notoneloop:
                            case Notoneloopatomic:
                            case Setloop:
                            case Setloopatomic:
                                valid = true;
                                break;
                        }
                    }
                    else // type == Lazyloop
                    {
                        switch (child.Type)
                        {
                            case Onelazy:
                            case Notonelazy:
                            case Setlazy:
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
                return new RegexNode(Nothing, Options);
            }

            // If the Loop or Lazyloop now only has one child node and its a Set, One, or Notone,
            // reduce to just Setloop/lazy, Oneloop/lazy, or Notoneloop/lazy.  The parser will
            // generally have only produced the latter, but other reductions could have exposed
            // this.
            if (u.ChildCount() == 1)
            {
                RegexNode child = u.Child(0);
                switch (child.Type)
                {
                    case One:
                    case Notone:
                    case Set:
                        child.MakeRep(u.Type == Lazyloop ? Onelazy : Oneloop, u.M, u.N);
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
            Debug.Assert(Type == Set || Type == Setloop || Type == Setloopatomic || Type == Setlazy);
            Debug.Assert(!string.IsNullOrEmpty(Str));

            if (RegexCharClass.IsEmpty(Str))
            {
                Type = Nothing;
                Str = null;
            }
            else if (RegexCharClass.IsSingleton(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Type =
                    Type == Set ? One :
                    Type == Setloop ? Oneloop :
                    Type == Setloopatomic ? Oneloopatomic :
                    Onelazy;
            }
            else if (RegexCharClass.IsSingletonInverse(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Type =
                    Type == Set ? Notone :
                    Type == Setloop ? Notoneloop :
                    Type == Setloopatomic ? Notoneloopatomic :
                    Notonelazy;
            }

            return this;
        }

        /// <summary>Optimize an alternation.</summary>
        private RegexNode ReduceAlternation()
        {
            Debug.Assert(Type == Alternate);

            switch (ChildCount())
            {
                case 0:
                    return new RegexNode(Nothing, Options);

                case 1:
                    return Child(0);

                default:
                    ReduceSingleLetterAndNestedAlternations();
                    RegexNode newThis = ReplaceNodeIfUnnecessary(Nothing);
                    return newThis != this ? newThis : ExtractCommonPrefix();
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
                        if (at.Type == Alternate)
                        {
                            if (at.Children is List<RegexNode> atChildren)
                            {
                                for (int k = 0; k < atChildren.Count; k++)
                                {
                                    atChildren[k].Next = this;
                                }
                                children.InsertRange(i + 1, atChildren);
                            }
                            else
                            {
                                RegexNode atChild = (RegexNode)at.Children!;
                                atChild.Next = this;
                                children.Insert(i + 1, atChild);
                            }
                            j--;
                        }
                        else if (at.Type == Set || at.Type == One)
                        {
                            // Cannot merge sets if L or I options differ, or if either are negated.
                            optionsAt = at.Options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                            if (at.Type == Set)
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
                            if (prev.Type == One)
                            {
                                prevCharClass = new RegexCharClass();
                                prevCharClass.AddChar(prev.Ch);
                            }
                            else
                            {
                                prevCharClass = RegexCharClass.Parse(prev.Str!);
                            }

                            if (at.Type == One)
                            {
                                prevCharClass.AddChar(at.Ch);
                            }
                            else
                            {
                                RegexCharClass atCharClass = RegexCharClass.Parse(at.Str!);
                                prevCharClass.AddCharClass(atCharClass);
                            }

                            prev.Type = Set;
                            prev.Str = prevCharClass.ToStringClass();
                        }
                        else if (at.Type == Nothing)
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

            // Analyzes all the branches of the alternation for text that's identical at the beginning
            // of every branch.  That text is then pulled out into its own one or multi node in a
            // concatenation with the alternation (whose branches are updated to remove that prefix).
            // This is valuable for a few reasons.  One, it exposes potentially more text to the
            // expression prefix analyzer used to influence FindFirstChar.  Second, it exposes more
            // potential alternation optimizations, e.g. if the same prefix is followed in two branches
            // by sets that can be merged.  Third, it reduces the amount of duplicated comparisons required
            // if we end up backtracking into subsequent branches.
            // e.g. abc|ade => a(?bc|de)
            RegexNode ExtractCommonPrefix()
            {
                // To keep things relatively simple, we currently only handle:
                // - Left to right (e.g. we don't process alternations in lookbehinds)
                // - Branches that are one or multi nodes, or that are concatenations beginning with one or multi nodes.
                // - All branches having the same options.
                // - Text, rather than also trying to combine identical sets that start each branch.

                Debug.Assert(Children is List<RegexNode>);
                var children = (List<RegexNode>)Children;
                Debug.Assert(children.Count >= 2);

                // Only extract left-to-right prefixes.
                if ((Options & RegexOptions.RightToLeft) != 0)
                {
                    return this;
                }

                // Process the first branch to get the maximum possible common string.
                RegexNode? startingNode = FindBranchOneMultiStart(children[0]);
                if (startingNode is null)
                {
                    return this;
                }

                RegexOptions startingNodeOptions = startingNode.Options;
                string? originalStartingString = startingNode.Str;
                ReadOnlySpan<char> startingSpan = startingNode.Type == One ? stackalloc char[1] { startingNode.Ch } : (ReadOnlySpan<char>)originalStartingString;
                Debug.Assert(startingSpan.Length > 0);

                // Now compare the rest of the branches against it.
                for (int i = 1; i < children.Count; i++)
                {
                    // Get the starting node of the next branch.
                    startingNode = FindBranchOneMultiStart(children[i]);
                    if (startingNode is null || startingNode.Options != startingNodeOptions)
                    {
                        return this;
                    }

                    // See if the new branch's prefix has a shared prefix with the current one.
                    // If it does, shorten to that; if it doesn't, bail.
                    if (startingNode.Type == One)
                    {
                        if (startingSpan[0] != startingNode.Ch)
                        {
                            return this;
                        }

                        if (startingSpan.Length != 1)
                        {
                            startingSpan = startingSpan.Slice(0, 1);
                        }
                    }
                    else
                    {
                        Debug.Assert(startingNode.Type == Multi);
                        Debug.Assert(startingNode.Str!.Length > 0);

                        int minLength = Math.Min(startingSpan.Length, startingNode.Str.Length);
                        int c = 0;
                        while (c < minLength && startingSpan[c] == startingNode.Str[c]) c++;
                        if (c == 0)
                        {
                            return this;
                        }

                        startingSpan = startingSpan.Slice(0, c);
                    }
                }

                // If we get here, we have a starting string prefix shared by all branches.
                Debug.Assert(startingSpan.Length > 0);

                // Now remove the prefix from each branch.
                for (int i = 0; i < children.Count; i++)
                {
                    RegexNode branch = children[i];
                    if (branch.Type == Concatenate)
                    {
                        ProcessOneOrMulti(branch.Child(0), startingSpan);
                        ReplaceChild(i, branch.Reduce());
                    }
                    else
                    {
                        ProcessOneOrMulti(branch, startingSpan);
                    }

                    // Remove the starting text from the one or multi node.  This may end up changing
                    // the type of the node to be Empty if the starting text matches the node's full value.
                    static void ProcessOneOrMulti(RegexNode node, ReadOnlySpan<char> startingSpan)
                    {
                        if (node.Type == One)
                        {
                            Debug.Assert(startingSpan.Length == 1);
                            Debug.Assert(startingSpan[0] == node.Ch);
                            node.Type = Empty;
                            node.Ch = '\0';
                        }
                        else
                        {
                            Debug.Assert(node.Type == Multi);
                            Debug.Assert(node.Str.AsSpan().StartsWith(startingSpan, StringComparison.Ordinal));
                            if (node.Str!.Length == startingSpan.Length)
                            {
                                node.Type = Empty;
                                node.Str = null;
                            }
                            else if (node.Str.Length - 1 == startingSpan.Length)
                            {
                                node.Type = One;
                                node.Ch = node.Str[^1];
                                node.Str = null;
                            }
                            else
                            {
                                node.Str = node.Str.Substring(startingSpan.Length);
                            }
                        }
                    }
                }

                // We may have changed multiple branches to be Empty, but we only need to keep
                // the first (keeping the rest would just duplicate work in backtracking, though
                // it would also mean the original regex had at least two identical branches).
                // e.g. abc|Empty|Empty|def|Empty => abc|Empty|def
                for (int firstEmpty = 0; firstEmpty < children.Count; firstEmpty++)
                {
                    if (children[firstEmpty].Type != Empty)
                    {
                        continue;
                    }

                    // Found the first empty.  Now starting after it, remove all subsequent found Empty nodes,
                    // pushing everything else down. (In the future, should we want to there's also the opportunity
                    // here to remove other duplication, but such duplication is a more egregious mistake on the
                    // part of the expression author.)
                    int i = firstEmpty + 1;
                    int j = i;
                    while (i < children.Count)
                    {
                        if (children[i].Type != Empty)
                        {
                            if (j != i)
                            {
                                children[j] = children[i];
                            }
                            j++;
                        }
                        i++;
                    }

                    if (j < i)
                    {
                        children.RemoveRange(j, i - j);
                    }

                    break;
                }

                var concat = new RegexNode(Concatenate, Options); // use same options as the Alternate
                concat.AddChild(startingSpan.Length == 1 ? // use same options as the branches
                    new RegexNode(One, startingNodeOptions) { Ch = startingSpan[0] } :
                    new RegexNode(Multi, startingNodeOptions) { Str = originalStartingString?.Length == startingSpan.Length ? originalStartingString : startingSpan.ToString() });
                concat.AddChild(this); // this will re-reduce the node, allowing for newly exposed possible optimizations in what came after the prefix
                return concat;

                // Finds the starting one or multi of the branch, if it has one; otherwise, returns null.
                // For simplicity, this only considers branches that are One or Multi, or a Concatenation
                // beginning with a One or Multi.  We don't traverse more than one level to avoid the
                // complication of then having to later update that hierarchy when removing the prefix,
                // but it could be done in the future if proven beneficial enough.
                static RegexNode? FindBranchOneMultiStart(RegexNode branch)
                {
                    if (branch.Type == Concatenate)
                    {
                        branch = branch.Child(0);
                    }

                    return branch.Type == One || branch.Type == Multi ? branch : null;
                }
            }
        }

        /// <summary>
        /// Optimizes a concatenation by coalescing adjacent characters and strings,
        /// coalescing adjacent loops, converting loops to be atomic where applicable,
        /// and removing the concatenation itself if it's unnecessary.
        /// </summary>
        private RegexNode ReduceConcatenation()
        {
            Debug.Assert(Type == Concatenate);

            // If the concat node has zero or only one child, get rid of the concat.
            switch (ChildCount())
            {
                case 0:
                    return new RegexNode(Empty, Options);
                case 1:
                    return Child(0);
            }

            // Coalesce adjacent characters/strings.
            ReduceConcatenationWithAdjacentStrings();

            // Coalesce adjacent loops.  This helps to minimize work done by the interpreter, minimize code gen,
            // and also help to reduce catastrophic backtracking.
            ReduceConcatenationWithAdjacentLoops();

            // Now convert as many loops as possible to be atomic to avoid unnecessary backtracking.
            if ((Options & RegexOptions.RightToLeft) == 0)
            {
                ReduceConcatenationWithAutoAtomic();
            }

            // If the concatenation is now empty, return an empty node, or if it's got a single child, return that child.
            // Otherwise, return this.
            return ReplaceNodeIfUnnecessary(Empty);
        }

        /// <summary>
        /// Combine adjacent characters/strings.
        /// e.g. (?:abc)(?:def) -> abcdef
        /// </summary>
        private void ReduceConcatenationWithAdjacentStrings()
        {
            Debug.Assert(Type == Concatenate);
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

                if (at.Type == Concatenate &&
                    ((at.Options & RegexOptions.RightToLeft) == (Options & RegexOptions.RightToLeft)))
                {
                    if (at.Children is List<RegexNode> atChildren)
                    {
                        for (int k = 0; k < atChildren.Count; k++)
                        {
                            atChildren[k].Next = this;
                        }
                        children.InsertRange(i + 1, atChildren);
                    }
                    else
                    {
                        RegexNode atChild = (RegexNode)at.Children!;
                        atChild.Next = this;
                        children.Insert(i + 1, atChild);
                    }
                    j--;
                }
                else if (at.Type == Multi || at.Type == One)
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

                    if (prev.Type == One)
                    {
                        prev.Type = Multi;
                        prev.Str = prev.Ch.ToString();
                    }

                    if ((optionsAt & RegexOptions.RightToLeft) == 0)
                    {
                        prev.Str = (at.Type == One) ? $"{prev.Str}{at.Ch}" : prev.Str + at.Str;
                    }
                    else
                    {
                        prev.Str = (at.Type == One) ? $"{at.Ch}{prev.Str}" : at.Str + prev.Str;
                    }
                }
                else if (at.Type == Empty)
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
        /// </summary>
        private void ReduceConcatenationWithAdjacentLoops()
        {
            Debug.Assert(Type == Concatenate);
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

                    switch (currentNode.Type)
                    {
                        // Coalescing a loop with its same type
                        case Oneloop when nextNode.Type == Oneloop && currentNode.Ch == nextNode.Ch:
                        case Oneloopatomic when nextNode.Type == Oneloopatomic && currentNode.Ch == nextNode.Ch:
                        case Onelazy when nextNode.Type == Onelazy && currentNode.Ch == nextNode.Ch:
                        case Notoneloop when nextNode.Type == Notoneloop && currentNode.Ch == nextNode.Ch:
                        case Notoneloopatomic when nextNode.Type == Notoneloopatomic && currentNode.Ch == nextNode.Ch:
                        case Notonelazy when nextNode.Type == Notonelazy && currentNode.Ch == nextNode.Ch:
                        case Setloop when nextNode.Type == Setloop && currentNode.Str == nextNode.Str:
                        case Setloopatomic when nextNode.Type == Setloopatomic && currentNode.Str == nextNode.Str:
                        case Setlazy when nextNode.Type == Setlazy && currentNode.Str == nextNode.Str:
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
                        case Oneloop when nextNode.Type == One && currentNode.Ch == nextNode.Ch:
                        case Oneloopatomic when nextNode.Type == One && currentNode.Ch == nextNode.Ch:
                        case Onelazy when nextNode.Type == One && currentNode.Ch == nextNode.Ch:
                        case Notoneloop when nextNode.Type == Notone && currentNode.Ch == nextNode.Ch:
                        case Notoneloopatomic when nextNode.Type == Notone && currentNode.Ch == nextNode.Ch:
                        case Notonelazy when nextNode.Type == Notone && currentNode.Ch == nextNode.Ch:
                        case Setloop when nextNode.Type == Set && currentNode.Str == nextNode.Str:
                        case Setloopatomic when nextNode.Type == Set && currentNode.Str == nextNode.Str:
                        case Setlazy when nextNode.Type == Set && currentNode.Str == nextNode.Str:
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

                        // Coalescing an individual item with a loop.
                        case One when (nextNode.Type == Oneloop || nextNode.Type == Oneloopatomic || nextNode.Type == Onelazy) && currentNode.Ch == nextNode.Ch:
                        case Notone when (nextNode.Type == Notoneloop || nextNode.Type == Notoneloopatomic || nextNode.Type == Notonelazy) && currentNode.Ch == nextNode.Ch:
                        case Set when (nextNode.Type == Setloop || nextNode.Type == Setloopatomic || nextNode.Type == Setlazy) && currentNode.Str == nextNode.Str:
                            if (CanCombineCounts(1, 1, nextNode.M, nextNode.N))
                            {
                                currentNode.Type = nextNode.Type;
                                currentNode.M = nextNode.M + 1;
                                currentNode.N = nextNode.N == int.MaxValue ? int.MaxValue : nextNode.N + 1;
                                next++;
                                continue;
                            }
                            break;

                        // Coalescing an individual item with another individual item.
                        case One when nextNode.Type == One && currentNode.Ch == nextNode.Ch:
                        case Notone when nextNode.Type == Notone && currentNode.Ch == nextNode.Ch:
                        case Set when nextNode.Type == Set && currentNode.Str == nextNode.Str:
                            currentNode.MakeRep(Oneloop, 2, 2);
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
        private void ReduceConcatenationWithAutoAtomic()
        {
            Debug.Assert(Type == Concatenate);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);
            Debug.Assert(Children is List<RegexNode>);

            var children = (List<RegexNode>)Children;
            for (int i = 0; i < children.Count - 1; i++)
            {
                ProcessNode(children[i], children[i + 1], DefaultMaxRecursionDepth);

                static void ProcessNode(RegexNode node, RegexNode subsequent, uint maxDepth)
                {
                    // Skip down the node past irrelevant nodes.
                    while (true)
                    {
                        // We can always recur into captures and into the last node of concatenations.
                        if (node.Type == Capture || node.Type == Concatenate)
                        {
                            node = node.Child(node.ChildCount() - 1);
                            continue;
                        }

                        // For loops with at least one guaranteed iteration, we can recur into them, but
                        // we need to be careful not to just always do so; the ending node of a loop can only
                        // be made atomic if what comes after the loop but also the beginning of the loop are
                        // compatible for the optimization.
                        if (node.Type == Loop)
                        {
                            RegexNode? loopDescendent = FindLastExpressionInLoopForAutoAtomic(node, maxDepth - 1);
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
                    switch (node.Type)
                    {
                        case Oneloop when CanBeMadeAtomic(node, subsequent, maxDepth - 1):
                        case Notoneloop when CanBeMadeAtomic(node, subsequent, maxDepth - 1):
                        case Setloop when CanBeMadeAtomic(node, subsequent, maxDepth - 1):
                            node.MakeLoopAtomic();
                            break;
                        case Alternate:
                            // In the case of alternation, we can't change the alternation node itself
                            // based on what comes after it (at least not with more complicated analysis
                            // that factors in all branches together), but we can look at each individual
                            // branch, and analyze ending loops in each branch individually to see if they
                            // can be made atomic.  Then if we do end up backtracking into the alternation,
                            // we at least won't need to backtrack into that loop.
                            {
                                int alternateBranches = node.ChildCount();
                                for (int b = 0; b < alternateBranches; b++)
                                {
                                    ProcessNode(node.Child(b), subsequent, maxDepth - 1);
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
        private static RegexNode? FindLastExpressionInLoopForAutoAtomic(RegexNode node, uint maxDepth)
        {
            Debug.Assert(node.Type == Loop);

            // Start by looking at the loop's sole child.
            node = node.Child(0);

            // Skip past captures.
            while (node.Type == Capture)
            {
                node = node.Child(0);
            }

            // If the loop's body terminates with a {one/notone/set} loop, return it.
            if (node.Type == Oneloop || node.Type == Notoneloop || node.Type == Setloop)
            {
                return node;
            }

            // If the loop's body is a concatenate, we can skip to its last child iff that
            // last child doesn't conflict with the first child, since this whole concatenation
            // could be repeated, such that the first node ends up following the last.  For
            // example, in the expression (a+[def])*, the last child is [def] and the first is
            // a+, which can't possibly overlap with [def].  In contrast, if we had (a+[ade])*,
            // [ade] could potentially match the starting 'a'.
            if (node.Type == Concatenate)
            {
                int concatCount = node.ChildCount();
                RegexNode lastConcatChild = node.Child(concatCount - 1);
                if (CanBeMadeAtomic(lastConcatChild, node.Child(0), maxDepth - 1))
                {
                    return lastConcatChild;
                }
            }

            // Otherwise, the loop has nothing that can participate in auto-atomicity.
            return null;
        }

        /// <summary>
        /// Determines whether node can be switched to an atomic loop.  Subsequent is the node
        /// immediately after 'node'.
        /// </summary>
        private static bool CanBeMadeAtomic(RegexNode node, RegexNode subsequent, uint maxDepth)
        {
            if (maxDepth == 0)
            {
                // We hit our recursion limit.  Just don't apply the optimization.
                return false;
            }

            // Skip the successor down to the closest node that's guaranteed to follow it.
            while (subsequent.ChildCount() > 0)
            {
                Debug.Assert(subsequent.Type != Group);
                switch (subsequent.Type)
                {
                    case Concatenate:
                    case Capture:
                    case Atomic:
                    case Require when (subsequent.Options & RegexOptions.RightToLeft) == 0: // only lookaheads, not lookbehinds (represented as RTL Require nodes)
                    case Loop when subsequent.M > 0:
                    case Lazyloop when subsequent.M > 0:
                        subsequent = subsequent.Child(0);
                        continue;
                }

                break;
            }

            // If the two nodes don't agree on options in any way, don't try to optimize them.
            if (node.Options != subsequent.Options)
            {
                return false;
            }

            // If the successor is an alternation, all of its children need to be evaluated, since any of them
            // could come after this node.  If any of them fail the optimization, then the whole node fails.
            if (subsequent.Type == Alternate)
            {
                int childCount = subsequent.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    if (!CanBeMadeAtomic(node, subsequent.Child(i), maxDepth - 1))
                    {
                        return false;
                    }
                }

                return true;
            }

            // If this node is a {one/notone/set}loop, see if it overlaps with its successor in the concatenation.
            // If it doesn't, then we can upgrade it to being a {one/notone/set}loopatomic.
            // Doing so avoids unnecessary backtracking.
            switch (node.Type)
            {
                case Oneloop:
                    switch (subsequent.Type)
                    {
                        case One when node.Ch != subsequent.Ch:
                        case Onelazy when subsequent.M > 0 && node.Ch != subsequent.Ch:
                        case Oneloop when subsequent.M > 0 && node.Ch != subsequent.Ch:
                        case Oneloopatomic when subsequent.M > 0 && node.Ch != subsequent.Ch:
                        case Notone when node.Ch == subsequent.Ch:
                        case Notonelazy when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Notoneloop when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Notoneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Multi when node.Ch != subsequent.Str![0]:
                        case Set when !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case Setlazy when subsequent.M > 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case Setloop when subsequent.M > 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case Setloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case End:
                        case EndZ when node.Ch != '\n':
                        case Eol when node.Ch != '\n':
                        case Boundary when RegexCharClass.IsWordChar(node.Ch):
                        case NonBoundary when !RegexCharClass.IsWordChar(node.Ch):
                        case ECMABoundary when RegexCharClass.IsECMAWordChar(node.Ch):
                        case NonECMABoundary when !RegexCharClass.IsECMAWordChar(node.Ch):
                            return true;
                    }
                    break;

                case Notoneloop:
                    switch (subsequent.Type)
                    {
                        case One when node.Ch == subsequent.Ch:
                        case Onelazy when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Oneloop when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Oneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Multi when node.Ch == subsequent.Str![0]:
                        case End:
                            return true;
                    }
                    break;

                case Setloop:
                    switch (subsequent.Type)
                    {
                        case One when !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Onelazy when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Oneloop when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Oneloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Multi when !RegexCharClass.CharInClass(subsequent.Str![0], node.Str!):
                        case Set when !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case Setlazy when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case Setloop when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case Setloopatomic when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case End:
                        case EndZ when !RegexCharClass.CharInClass('\n', node.Str!):
                        case Eol when !RegexCharClass.CharInClass('\n', node.Str!):
                        case Boundary when node.Str == RegexCharClass.WordClass || node.Str == RegexCharClass.DigitClass: // TODO: Expand these with a more inclusive overlap check that considers categories
                        case NonBoundary when node.Str == RegexCharClass.NotWordClass || node.Str == RegexCharClass.NotDigitClass:
                        case ECMABoundary when node.Str == RegexCharClass.ECMAWordClass || node.Str == RegexCharClass.ECMADigitClass:
                        case NonECMABoundary when node.Str == RegexCharClass.NotECMAWordClass || node.Str == RegexCharClass.NotDigitClass:
                            return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>Computes a min bound on the required length of any string that could possibly match.</summary>
        /// <returns>The min computed length.  If the result is 0, there is no minimum we can enforce.</returns>
        /// <remarks>
        /// e.g. abc[def](ghijkl|mn) => 6
        /// </remarks>
        public int ComputeMinLength()
        {
            return ComputeMinLength(this, DefaultMaxRecursionDepth);

            static int ComputeMinLength(RegexNode node, uint maxDepth)
            {
                if (maxDepth == 0)
                {
                    // Don't examine any further, as we've reached the max allowed depth.
                    return 0;
                }

                switch (node.Type)
                {
                    case One:
                    case Notone:
                    case Set:
                        // Single character.
                        return 1;

                    case Multi:
                        // Every character in the string needs to match.
                        return node.Str!.Length;

                    case Notonelazy:
                    case Notoneloop:
                    case Notoneloopatomic:
                    case Onelazy:
                    case Oneloop:
                    case Oneloopatomic:
                    case Setlazy:
                    case Setloop:
                    case Setloopatomic:
                        // One character repeated at least M times.
                        return node.M;

                    case Lazyloop:
                    case Loop:
                        // A node graph repeated at least M times.
                        return (int)Math.Min(int.MaxValue, (long)node.M * ComputeMinLength(node.Child(0), maxDepth - 1));

                    case Alternate:
                        // The minimum required length for any of the alternation's branches.
                        {
                            int childCount = node.ChildCount();
                            Debug.Assert(childCount >= 2);
                            int min = ComputeMinLength(node.Child(0), maxDepth - 1);
                            for (int i = 1; i < childCount && min > 0; i++)
                            {
                                min = Math.Min(min, ComputeMinLength(node.Child(i), maxDepth - 1));
                            }
                            return min;
                        }

                    case Concatenate:
                        // The sum of all of the concatenation's children.
                        {
                            long sum = 0;
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                sum += ComputeMinLength(node.Child(i), maxDepth - 1);
                            }
                            return (int)Math.Min(int.MaxValue, sum);
                        }

                    case Atomic:
                    case Capture:
                    case Group:
                        // For groups, we just delegate to the sole child.
                        Debug.Assert(node.ChildCount() == 1);
                        return ComputeMinLength(node.Child(0), maxDepth - 1);

                    case Empty:
                    case Nothing:
                    case UpdateBumpalong:
                    // Nothing to match. In the future, we could potentially use Nothing to say that the min length
                    // is infinite, but that would require a different structure, as that would only apply if the
                    // Nothing match is required in all cases (rather than, say, as one branch of an alternation).
                    case Beginning:
                    case Bol:
                    case Boundary:
                    case ECMABoundary:
                    case End:
                    case EndZ:
                    case Eol:
                    case NonBoundary:
                    case NonECMABoundary:
                    case Start:
                    // Difficult to glean anything meaningful from boundaries or results only known at run time.
                    case Prevent:
                    case Require:
                    // Lookaheads/behinds could potentially be included in the future, but that will require
                    // a different structure, as they can't be added as part of a concatenation, since they overlap
                    // with what comes after.
                    case Ref:
                    case Testgroup:
                    case Testref:
                        // Constructs requiring data at runtime from the matching pattern can't influence min length.
                        return 0;

                    default:
#if DEBUG
                        Debug.Fail($"Unknown node: {node.TypeName}");
#endif
                        goto case Empty;
                }
            }
        }

        public RegexNode MakeQuantifier(bool lazy, int min, int max)
        {
            if (min == 0 && max == 0)
                return new RegexNode(Empty, Options);

            if (min == 1 && max == 1)
                return this;

            switch (Type)
            {
                case One:
                case Notone:
                case Set:
                    MakeRep(lazy ? Onelazy : Oneloop, min, max);
                    return this;

                default:
                    var result = new RegexNode(lazy ? Lazyloop : Loop, Options, min, max);
                    result.AddChild(this);
                    return result;
            }
        }

        public void AddChild(RegexNode newChild)
        {
            newChild.Next = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Next = this; // in case Reduce returns a different node that needs to be reparented

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

            newChild.Next = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Next = this; // in case Reduce returns a different node that needs to be reparented

            ((List<RegexNode>)Children).Insert(index, newChild);
        }

        public void ReplaceChild(int index, RegexNode newChild)
        {
            Debug.Assert(Children != null);
            Debug.Assert(index < ChildCount());

            newChild.Next = this;
            if (Children is RegexNode)
            {
                Children = newChild;
            }
            else
            {
                ((List<RegexNode>)Children)[index] = newChild;
            }
        }

        public RegexNode Child(int i)
        {
            if (Children is RegexNode child)
            {
                return child;
            }

            return ((List<RegexNode>)Children!)[i];
        }

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

#if DEBUG
        private string TypeName =>
            Type switch
            {
                Oneloop => nameof(Oneloop),
                Notoneloop => nameof(Notoneloop),
                Setloop => nameof(Setloop),
                Onelazy => nameof(Onelazy),
                Notonelazy => nameof(Notonelazy),
                Setlazy => nameof(Setlazy),
                One => nameof(One),
                Notone => nameof(Notone),
                Set => nameof(Set),
                Multi => nameof(Multi),
                Ref => nameof(Ref),
                Bol => nameof(Bol),
                Eol => nameof(Eol),
                Boundary => nameof(Boundary),
                NonBoundary => nameof(NonBoundary),
                ECMABoundary => nameof(ECMABoundary),
                NonECMABoundary => nameof(NonECMABoundary),
                Beginning => nameof(Beginning),
                Start => nameof(Start),
                EndZ => nameof(EndZ),
                End => nameof(End),
                Oneloopatomic => nameof(Oneloopatomic),
                Notoneloopatomic => nameof(Notoneloopatomic),
                Setloopatomic => nameof(Setloopatomic),
                Nothing => nameof(Nothing),
                Empty => nameof(Empty),
                Alternate => nameof(Alternate),
                Concatenate => nameof(Concatenate),
                Loop => nameof(Loop),
                Lazyloop => nameof(Lazyloop),
                Capture => nameof(Capture),
                Group => nameof(Group),
                Require => nameof(Require),
                Prevent => nameof(Prevent),
                Atomic => nameof(Atomic),
                Testref => nameof(Testref),
                Testgroup => nameof(Testgroup),
                UpdateBumpalong => nameof(UpdateBumpalong),
                _ => $"(unknown {Type})"
            };

        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        public string Description()
        {
            var sb = new StringBuilder(TypeName);

            if ((Options & RegexOptions.ExplicitCapture) != 0) sb.Append("-C");
            if ((Options & RegexOptions.IgnoreCase) != 0) sb.Append("-I");
            if ((Options & RegexOptions.RightToLeft) != 0) sb.Append("-L");
            if ((Options & RegexOptions.Multiline) != 0) sb.Append("-M");
            if ((Options & RegexOptions.Singleline) != 0) sb.Append("-S");
            if ((Options & RegexOptions.IgnorePatternWhitespace) != 0) sb.Append("-X");
            if ((Options & RegexOptions.ECMAScript) != 0) sb.Append("-E");

            switch (Type)
            {
                case Oneloop:
                case Oneloopatomic:
                case Notoneloop:
                case Notoneloopatomic:
                case Onelazy:
                case Notonelazy:
                case One:
                case Notone:
                    sb.Append(" '").Append(RegexCharClass.CharDescription(Ch)).Append('\'');
                    break;
                case Capture:
                    sb.Append(' ').Append($"index = {M}");
                    if (N != -1)
                    {
                        sb.Append($", unindex = {N}");
                    }
                    break;
                case Ref:
                case Testref:
                    sb.Append(' ').Append($"index = {M}");
                    break;
                case Multi:
                    sb.Append(" \"").Append(Str).Append('"');
                    break;
                case Set:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                    sb.Append(' ').Append(RegexCharClass.SetDescription(Str!));
                    break;
            }

            switch (Type)
            {
                case Oneloop:
                case Oneloopatomic:
                case Notoneloop:
                case Notoneloopatomic:
                case Onelazy:
                case Notonelazy:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                case Loop:
                case Lazyloop:
                    sb.Append(
                        (M == 0 && N == int.MaxValue) ? "*" :
                        (M == 0 && N == 1) ? "?" :
                        (M == 1 && N == int.MaxValue) ? "+" :
                        (N == int.MaxValue) ? $"{{{M}, *}}" :
                        $"{{{M}, {N}}}");
                    break;
            }

            return sb.ToString();
        }

        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        public void Dump() => Debug.WriteLine(ToString());

        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        public override string ToString()
        {
            RegexNode? curNode = this;
            int curChild = 0;
            var sb = new StringBuilder().AppendLine(curNode.Description());
            var stack = new List<int>();
            while (true)
            {
                if (curChild < curNode!.ChildCount())
                {
                    stack.Add(curChild + 1);
                    curNode = curNode.Child(curChild);
                    curChild = 0;

                    sb.Append(new string(' ', stack.Count * 2)).Append(curNode.Description()).AppendLine();
                }
                else
                {
                    if (stack.Count == 0)
                    {
                        break;
                    }

                    curChild = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    curNode = curNode.Next;
                }
            }

            return sb.ToString();
        }
#endif
    }
}
