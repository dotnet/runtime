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
using System.Globalization;
using System.Threading;

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

        /// <summary>empty bit from the node's options to store data on whether a node contains captures</summary>
        internal const RegexOptions HasCapturesFlag = (RegexOptions)(1 << 31);

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

        /// <summary>Creates a RegexNode representing a single character.</summary>
        /// <param name="ch">The character.</param>
        /// <param name="options">The node's options.</param>
        /// <param name="culture">The culture to use to perform any required transformations.</param>
        /// <returns>The created RegexNode.  This might be a RegexNode.One or a RegexNode.Set.</returns>
        public static RegexNode CreateOneWithCaseConversion(char ch, RegexOptions options, CultureInfo? culture)
        {
            // If the options specify case-insensitivity, we try to create a node that fully encapsulates that.
            if ((options & RegexOptions.IgnoreCase) != 0)
            {
                Debug.Assert(culture is not null);

                // If the character is part of a Unicode category that doesn't participate in case conversion,
                // we can simply strip out the IgnoreCase option and make the node case-sensitive.
                if (!RegexCharClass.ParticipatesInCaseConversion(ch))
                {
                    return new RegexNode(One, options & ~RegexOptions.IgnoreCase, ch);
                }

                // Create a set for the character, trying to include all case-insensitive equivalent characters.
                // If it's successful in doing so, resultIsCaseInsensitive will be false and we can strip
                // out RegexOptions.IgnoreCase as part of creating the set.
                string stringSet = RegexCharClass.OneToStringClass(ch, culture, out bool resultIsCaseInsensitive);
                if (!resultIsCaseInsensitive)
                {
                    return new RegexNode(Set, options & ~RegexOptions.IgnoreCase, stringSet);
                }

                // Otherwise, until we can get rid of ToLower usage at match time entirely (https://github.com/dotnet/runtime/issues/61048),
                // lowercase the character and proceed to create an IgnoreCase One node.
                ch = culture.TextInfo.ToLower(ch);
            }

            // Create a One node for the character.
            return new RegexNode(One, options, ch);
        }

        /// <summary>Reverses all children of a concatenation when in RightToLeft mode.</summary>
        public RegexNode ReverseConcatenationIfRightToLeft()
        {
            if ((Options & RegexOptions.RightToLeft) != 0 &&
                Type == Concatenate &&
                ChildCount() > 1)
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
            Type += type - One;
            M = min;
            N = max;
        }

        private void MakeLoopAtomic()
        {
            switch (Type)
            {
                case Oneloop or Notoneloop or Setloop:
                    // For loops, we simply change the Type to the atomic variant.
                    // Atomic greedy loops should consume as many values as they can.
                    Type += Oneloopatomic - Oneloop;
                    break;

                case Onelazy or Notonelazy or Setlazy:
                    // For lazy, we not only change the Type, we also lower the max number of iterations
                    // to the minimum number of iterations, as they should end up matching as little as possible.
                    Type += Oneloopatomic - Onelazy;
                    N = M;
                    break;

                default:
                    Debug.Fail($"Unexpected type: {Type}");
                    break;
            }
        }

#if DEBUG
        /// <summary>Validate invariants the rest of the implementation relies on for processing fully-built trees.</summary>
        [Conditional("DEBUG")]
        private void ValidateFinalTreeInvariants()
        {
            Debug.Assert(Type == Capture, "Every generated tree should begin with a capture node");

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
                    Debug.Assert(child.Next == node, $"{child.Description()} missing reference to parent {node.Description()}");

                    toExamine.Push(child);
                }

                // Validate that we never see certain node types.
                Debug.Assert(Type != Group, "All Group nodes should have been removed.");

                // Validate node types and expected child counts.
                switch (node.Type)
                {
                    case Group:
                        Debug.Fail("All Group nodes should have been removed.");
                        break;

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
                        break;

                    case Testref:
                        Debug.Assert(childCount == 2, $"Expected two children for {node.TypeName}, got {childCount}");
                        break;

                    case Testgroup:
                        Debug.Assert(childCount == 3, $"Expected three children for {node.TypeName}, got {childCount}");
                        break;

                    case Concatenate:
                    case Alternate:
                        Debug.Assert(childCount >= 2, $"Expected at least two children for {node.TypeName}, got {childCount}.");
                        break;

                    default:
                        Debug.Fail($"Unexpected node type: {node.Type}");
                        break;
                }

                // Validate node configuration.
                switch (node.Type)
                {
                    case Multi:
                        Debug.Assert(node.Str is not null, "Expect non-null multi string");
                        Debug.Assert(node.Str.Length >= 2, $"Expected {node.Str} to be at least two characters");
                        break;

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

                            case Oneloop or Oneloopatomic or Notoneloop or Notoneloopatomic or Setloop or Setloopatomic when node.N == int.MaxValue:
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
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, just stop optimizing.
                return;
            }

            // RegexOptions.NonBacktracking doesn't support atomic groups, so when that option
            // is set we don't want to create atomic groups where they weren't explicitly authored.
            if ((Options & RegexOptions.NonBacktracking) != 0)
            {
                return;
            }

            // Walk the tree starting from the current node.
            RegexNode node = this;
            while (true)
            {
                switch (node.Type)
                {
                    // {One/Notone/Set}loops can be upgraded to {One/Notone/Set}loopatomic nodes, e.g. [abc]* => (?>[abc]*).
                    // And {One/Notone/Set}lazys can similarly be upgraded to be atomic, which really makes them into repeaters
                    // or even empty nodes.
                    case Oneloop:
                    case Notoneloop:
                    case Setloop:
                    case Onelazy:
                    case Notonelazy:
                    case Setlazy:
                        node.MakeLoopAtomic();
                        break;

                    // Just because a particular node is atomic doesn't mean all its descendants are.
                    // Process them as well.
                    case Atomic:
                        node = node.Child(0);
                        continue;

                    // For Capture and Concatenate, we just recur into their last child (only child in the case
                    // of Capture).  However, if the child is Alternate, Loop, and Lazyloop, we can also make the
                    // node itself atomic by wrapping it in an Atomic node. Since we later check to see whether a
                    // node is atomic based on its parent or grandparent, we don't bother wrapping such a node in
                    // an Atomic one if its grandparent is already Atomic.
                    // e.g. [xyz](?:abc|def) => [xyz](?>abc|def)
                    case Capture:
                    case Concatenate:
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
                                node.Child(i).EliminateEndingBacktracking();
                            }
                        }
                        node = node.Child(0);
                        continue;

                    // For Loop, we search to see if there's a viable last expression, and iff there
                    // is we recur into processing it.
                    // e.g. (?:abc*)* => (?:ab(?>c*))*
                    case Loop:
                        {
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

        /// <summary>Whether this node may be considered to be atomic based on its parent.</summary>
        /// <remarks>
        /// This may have false negatives, meaning the node may actually be atomic even if this returns false.
        /// But any true result may be relied on to mean the node will actually be considered to be atomic.
        /// </remarks>
        public bool IsAtomicByParent()
        {
            // Walk up the parent hierarchy.
            RegexNode child = this;
            for (RegexNode? parent = child.Next; parent is not null; child = parent, parent = child.Next)
            {
                switch (parent.Type)
                {
                    case Atomic:
                    case Prevent:
                    case Require:
                        // If the parent is atomic, so is the child.  That's the whole purpose
                        // of the Atomic node, and lookarounds are also implicitly atomic.
                        return true;

                    case Alternate:
                    case Testref:
                        // Skip alternations.  Each branch is considered independently,
                        // so any atomicity applied to the alternation also applies to
                        // each individual branch.  This is true as well for conditional
                        // backreferences, where each of the yes/no branches are independent.
                    case Testgroup when parent.Child(0) != child:
                        // As with alternations, each yes/no branch of an expression conditional
                        // are independent from each other, but the conditional expression itself
                        // can be backtracked into from each of the branches, so we can't make
                        // it atomic just because the whole conditional is.
                    case Capture:
                        // Skip captures. They don't affect atomicity.
                    case Concatenate when parent.Child(parent.ChildCount() - 1) == child:
                        // If the parent is a concatenation and this is the last node,
                        // any atomicity applying to the concatenation applies to this
                        // node, too.
                        continue;

                    default:
                        // For any other parent type, give up on trying to prove atomicity.
                        return false;
                }
            }

            // The parent was null, so nothing can backtrack in.
            return true;
        }

        /// <summary>
        /// Removes redundant nodes from the subtree, and returns an optimized subtree.
        /// </summary>
        internal RegexNode Reduce() =>
            Type switch
            {
                Alternate => ReduceAlternation(),
                Atomic => ReduceAtomic(),
                Concatenate => ReduceConcatenation(),
                Group => ReduceGroup(),
                Loop or Lazyloop => ReduceLoops(),
                Prevent => ReducePrevent(),
                Set or Setloop or Setloopatomic or Setlazy => ReduceSet(),
                Testgroup => ReduceTestgroup(),
                Testref => ReduceTestref(),
                _ => this,
            };

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
            // RegexOptions.NonBacktracking doesn't support atomic groups, so when that option
            // is set we don't want to create atomic groups where they weren't explicitly authored.
            if ((Options & RegexOptions.NonBacktracking) != 0)
            {
                return this;
            }

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

                // If an atomic subexpression contains only a {one/notone/set}{loop/lazy},
                // change it to be an {one/notone/set}loopatomic and remove the atomic node.
                case Oneloop:
                case Notoneloop:
                case Setloop:
                case Onelazy:
                case Notonelazy:
                case Setlazy:
                    child.MakeLoopAtomic();
                    return child;

                // Alternations have a variety of possible optimizations that can be applied
                // iff they're atomic.
                case Alternate:
                    if ((Options & RegexOptions.RightToLeft) == 0)
                    {
                        List<RegexNode>? branches = child.Children as List<RegexNode>;
                        Debug.Assert(branches is not null && branches.Count != 0);

                        // If an alternation is atomic and its first branch is Empty, the whole thing
                        // is a nop, as Empty will match everything trivially, and no backtracking
                        // into the node will be performed, making the remaining branches irrelevant.
                        if (branches[0].Type == Empty)
                        {
                            return new RegexNode(Empty, child.Options);
                        }

                        // Similarly, we can trim off any branches after an Empty, as they'll never be used.
                        // An Empty will match anything, and thus branches after that would only be used
                        // if we backtracked into it and advanced passed the Empty after trying the Empty...
                        // but if the alternation is atomic, such backtracking won't happen.
                        for (int i = 1; i < branches.Count - 1; i++)
                        {
                            if (branches[i].Type == Empty)
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

                        // If anything we reordered, there may be new optimization opportunities inside
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
                    RegexNode node = ReplaceNodeIfUnnecessary(Nothing);
                    node = ExtractCommonPrefixText(node);
                    node = ExtractCommonPrefixOneNotoneSet(node);
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
                            prev.Str = prevCharClass.ToStringClass(Options);
                            if ((prev.Options & RegexOptions.IgnoreCase) != 0 &&
                                RegexCharClass.MakeCaseSensitiveIfPossible(prev.Str, RegexParser.GetTargetCulture(prev.Options)) is string newSetString)
                            {
                                prev.Str = newSetString;
                                prev.Options &= ~RegexOptions.IgnoreCase;
                            }
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

            // This function optimizes out prefix nodes from alternation branches that are
            // the same across multiple contiguous branches.
            // e.g. \w12|\d34|\d56|\w78|\w90 => \w12|\d(?:34|56)|\w(?:78|90)
            static RegexNode ExtractCommonPrefixOneNotoneSet(RegexNode alternation)
            {
                if (alternation.Type != Alternate)
                {
                    return alternation;
                }

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
                    if (child.Type != Concatenate || child.ChildCount() < 2)
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
                    switch (required.Type)
                    {
                        case One or Notone or Set:
                        case Oneloopatomic or Notoneloopatomic or Setloopatomic:
                        case Oneloop or Notoneloop or Setloop or Onelazy or Notonelazy or Setlazy when required.M == required.N:
                            break;

                        default:
                            continue;
                    }

                    // Only handle the case where each branch begins with the exact same node value
                    int endingIndex = startingIndex + 1;
                    for (; endingIndex < children.Count; endingIndex++)
                    {
                        RegexNode other = children[endingIndex].Child(0);
                        if (required.Type != other.Type ||
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
                    var newAlternate = new RegexNode(Alternate, alternation.Options);
                    for (int i = startingIndex; i < endingIndex; i++)
                    {
                        ((List<RegexNode>)children[i].Children!).RemoveAt(0);
                        newAlternate.AddChild(children[i]);
                    }

                    // If this alternation is wrapped as atomic, we need to do the same for the new alternation.
                    if (alternation.Next is RegexNode parent && parent.Type == Atomic)
                    {
                        var atomic = new RegexNode(Atomic, alternation.Options);
                        atomic.AddChild(newAlternate);
                        newAlternate = atomic;
                    }

                    // Now create a concatenation of the prefix node with the new alternation for the combined
                    // branches, and replace all of the branches in this alternation with that new concatenation.
                    var newConcat = new RegexNode(Concatenate, alternation.Options);
                    newConcat.AddChild(required);
                    newConcat.AddChild(newAlternate);
                    alternation.ReplaceChild(startingIndex, newConcat);
                    children.RemoveRange(startingIndex + 1, endingIndex - startingIndex - 1);
                }

                // If we've reduced this alternation to just a single branch, return it.
                // Otherwise, return the alternation.
                return alternation.ChildCount() == 1 ? alternation.Child(0) : alternation;
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
                if (alternation.Type != Alternate)
                {
                    return alternation;
                }

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
                    if (startingNode.Type == One)
                    {
                        scratchChar[0] = startingNode.Ch;
                        startingSpan = scratchChar;
                    }
                    Debug.Assert(startingSpan.Length > 0);

                    // Now compare the rest of the branches against it.
                    int endingIndex = startingIndex + 1;
                    for ( ; endingIndex < children.Count; endingIndex++)
                    {
                        // Get the starting node of the next branch.
                        startingNode = children[endingIndex].FindBranchOneOrMultiStart();
                        if (startingNode is null || startingNode.Options != startingNodeOptions)
                        {
                            break;
                        }

                        // See if the new branch's prefix has a shared prefix with the current one.
                        // If it does, shorten to that; if it doesn't, bail.
                        if (startingNode.Type == One)
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
                            Debug.Assert(startingNode.Type == Multi);
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
                        new RegexNode(One, startingNodeOptions, startingSpan[0]) :
                        new RegexNode(Multi, startingNodeOptions, startingSpan.ToString());
                    var newAlternate = new RegexNode(Alternate, startingNodeOptions);
                    bool seenEmpty = false;
                    for (int i = startingIndex; i < endingIndex; i++)
                    {
                        RegexNode branch = children[i];
                        ProcessOneOrMulti(branch.Type == Concatenate ? branch.Child(0) : branch, startingSpan);
                        branch = branch.Reduce();
                        if (branch.Type == Empty)
                        {
                            if (seenEmpty)
                            {
                                continue;
                            }
                            seenEmpty = true;
                        }
                        newAlternate.AddChild(branch);

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

                    if (alternation.Next is RegexNode parent && parent.Type == Atomic)
                    {
                        var atomic = new RegexNode(Atomic, startingNodeOptions);
                        atomic.AddChild(newAlternate);
                        newAlternate = atomic;
                    }

                    var newConcat = new RegexNode(Concatenate, startingNodeOptions);
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
            RegexNode branch = this;

            if (branch.Type == Concatenate)
            {
                branch = branch.Child(0);
            }

            return branch.Type == One || branch.Type == Multi ? branch : null;
        }

        /// <summary>Gets the character that begins a One or Multi.</summary>
        public char FirstCharOfOneOrMulti()
        {
            Debug.Assert(Type is One or Multi);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);
            return Type == One ? Ch : Str![0];
        }

        /// <summary>Finds the guaranteed beginning character of the node, or null if none exists.</summary>
        public char? FindStartingCharacter()
        {
            RegexNode? node = this;
            while (true)
            {
                if (node is null || (node.Options & RegexOptions.RightToLeft) != 0)
                {
                    return null;
                }

                char c;
                switch (node.Type)
                {
                    case One:
                    case Oneloop or Oneloopatomic or Onelazy when node.M > 0:
                        c = node.Ch;
                        break;

                    case Multi:
                        c = node.Str![0];
                        break;

                    case Atomic:
                    case Concatenate:
                    case Capture:
                    case Group:
                    case Loop or Lazyloop when node.M > 0:
                    case Require:
                        node = node.Child(0);
                        continue;

                    default:
                        return null;
                }

                if ((node.Options & RegexOptions.IgnoreCase) == 0 ||
                    !RegexCharClass.ParticipatesInCaseConversion(c))
                {
                    return c;
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
                        case Oneloop or Oneloopatomic or Onelazy or Notoneloop or Notoneloopatomic or Notonelazy when nextNode.Type == currentNode.Type && currentNode.Ch == nextNode.Ch:
                        case Setloop or Setloopatomic or Setlazy when nextNode.Type == currentNode.Type && currentNode.Str == nextNode.Str:
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
                        case Oneloop or Oneloopatomic or Onelazy when nextNode.Type == One && currentNode.Ch == nextNode.Ch:
                        case Notoneloop or Notoneloopatomic or Notonelazy when nextNode.Type == Notone && currentNode.Ch == nextNode.Ch:
                        case Setloop or Setloopatomic or Setlazy when nextNode.Type == Set && currentNode.Str == nextNode.Str:
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
                        case One or Notone when nextNode.Type == currentNode.Type && currentNode.Ch == nextNode.Ch:
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
            // RegexOptions.NonBacktracking doesn't support atomic groups, so when that option
            // is set we don't want to create atomic groups where they weren't explicitly authored.
            if ((Options & RegexOptions.NonBacktracking) != 0)
            {
                return;
            }

            Debug.Assert(Type == Concatenate);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);
            Debug.Assert(Children is List<RegexNode>);

            var children = (List<RegexNode>)Children;
            for (int i = 0; i < children.Count - 1; i++)
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
                    switch (node.Type)
                    {
                        case Oneloop when CanBeMadeAtomic(node, subsequent):
                        case Notoneloop when CanBeMadeAtomic(node, subsequent):
                        case Setloop when CanBeMadeAtomic(node, subsequent):
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

            Debug.Assert(node.Type == Loop);

            // Start by looking at the loop's sole child.
            node = node.Child(0);

            // Skip past captures.
            while (node.Type == Capture)
            {
                node = node.Child(0);
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
                if (CanBeMadeAtomic(lastConcatChild, node.Child(0)))
                {
                    return lastConcatChild;
                }
            }

            // Otherwise, the loop has nothing that can participate in auto-atomicity.
            return null;
        }

        /// <summary>Optimizations for negative lookaheads/behinds.</summary>
        private RegexNode ReducePrevent()
        {
            Debug.Assert(Type == Prevent);
            Debug.Assert(ChildCount() == 1);

            // A negative lookahead/lookbehind wrapped around an empty child, i.e. (?!), is
            // sometimes used as a way to insert a guaranteed no-match into the expression.
            // We can reduce it to simply Nothing.
            if (Child(0).Type == Empty)
            {
                Type = Nothing;
                Children = null;
            }

            return this;
        }

        /// <summary>Optimizations for backreference conditionals.</summary>
        private RegexNode ReduceTestref()
        {
            Debug.Assert(Type == Testref);
            Debug.Assert(ChildCount() is 1 or 2);

            // This isn't so much an optimization as it is changing the tree for consistency.
            // We want all engines to be able to trust that every Testref will have two children,
            // even though it's optional in the syntax.  If it's missing a "not matched" branch,
            // we add one that will match empty.
            if (ChildCount() == 1)
            {
                AddChild(new RegexNode(Empty, Options));
            }

            return this;
        }

        /// <summary>Optimizations for expression conditionals.</summary>
        private RegexNode ReduceTestgroup()
        {
            Debug.Assert(Type == Testgroup);
            Debug.Assert(ChildCount() is 2 or 3);

            // This isn't so much an optimization as it is changing the tree for consistency.
            // We want all engines to be able to trust that every Testgroup will have three children,
            // even though it's optional in the syntax.  If it's missing a "not matched" branch,
            // we add one that will match empty.
            if (ChildCount() == 2)
            {
                AddChild(new RegexNode(Empty, Options));
            }

            // It's common for the condition to be an explicit positive lookahead, as specifying
            // that eliminates any ambiguity in syntax as to whether the expression is to be matched
            // as an expression or to be a reference to a capture group.  After parsing, however,
            // there's no ambiguity, and we can remove an extra level of positive lookahead, as the
            // engines need to treat the condition as a zero-width positive, atomic assertion regardless.
            RegexNode condition = Child(0);
            if (condition.Type == Require && (condition.Options & RegexOptions.RightToLeft) == 0)
            {
                ReplaceChild(0, condition.Child(0));
            }

            return this;
        }

        /// <summary>
        /// Determines whether node can be switched to an atomic loop.  Subsequent is the node
        /// immediately after 'node'.
        /// </summary>
        private static bool CanBeMadeAtomic(RegexNode node, RegexNode subsequent)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, just stop optimizing.
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
                    case Loop or Lazyloop when subsequent.M > 0:
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
                    if (!CanBeMadeAtomic(node, subsequent.Child(i)))
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
                        case Onelazy or Oneloop or Oneloopatomic when subsequent.M > 0 && node.Ch != subsequent.Ch:
                        case Notone when node.Ch == subsequent.Ch:
                        case Notonelazy or Notoneloop or Notoneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Multi when node.Ch != subsequent.Str![0]:
                        case Set when !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case Setlazy or Setloop or Setloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(node.Ch, subsequent.Str!):
                        case End:
                        case EndZ or Eol when node.Ch != '\n':
                        case Boundary when RegexCharClass.IsBoundaryWordChar(node.Ch):
                        case NonBoundary when !RegexCharClass.IsBoundaryWordChar(node.Ch):
                        case ECMABoundary when RegexCharClass.IsECMAWordChar(node.Ch):
                        case NonECMABoundary when !RegexCharClass.IsECMAWordChar(node.Ch):
                            return true;
                    }
                    break;

                case Notoneloop:
                    switch (subsequent.Type)
                    {
                        case One when node.Ch == subsequent.Ch:
                        case Onelazy or Oneloop or Oneloopatomic when subsequent.M > 0 && node.Ch == subsequent.Ch:
                        case Multi when node.Ch == subsequent.Str![0]:
                        case End:
                            return true;
                    }
                    break;

                case Setloop:
                    switch (subsequent.Type)
                    {
                        case One when !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Onelazy or Oneloop or Oneloopatomic when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                        case Multi when !RegexCharClass.CharInClass(subsequent.Str![0], node.Str!):
                        case Set when !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case Setlazy or Setloop or Setloopatomic when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                        case End:
                        case EndZ or Eol when !RegexCharClass.CharInClass('\n', node.Str!):
                        case Boundary when node.Str == RegexCharClass.WordClass || node.Str == RegexCharClass.DigitClass:
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
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, assume there's no minimum we can enforce.
                return 0;
            }

            switch (Type)
            {
                case One:
                case Notone:
                case Set:
                    // Single character.
                    return 1;

                case Multi:
                    // Every character in the string needs to match.
                    return Str!.Length;

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
                    return M;

                case Lazyloop:
                case Loop:
                    // A node graph repeated at least M times.
                    return (int)Math.Min(int.MaxValue, (long)M * Child(0).ComputeMinLength());

                case Alternate:
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

                case Concatenate:
                    // The sum of all of the concatenation's children.
                    {
                        long sum = 0;
                        int childCount = ChildCount();
                        for (int i = 0; i < childCount; i++)
                        {
                            sum += Child(i).ComputeMinLength();
                        }
                        return (int)Math.Min(int.MaxValue, sum);
                    }

                case Atomic:
                case Capture:
                case Group:
                    // For groups, we just delegate to the sole child.
                    Debug.Assert(ChildCount() == 1);
                    return Child(0).ComputeMinLength();

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
                    Debug.Fail($"Unknown node: {TypeName}");
#endif
                    goto case Empty;
            }
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
            static bool CanJoinLengthCheck(RegexNode node) => node.Type switch
            {
                One or Notone or Set => true,
                Multi => true,
                Oneloop or Onelazy or Oneloopatomic or
                    Notoneloop or Notonelazy or Notoneloopatomic or
                    Setloop or Setlazy or Setloopatomic when node.M == node.N => true,
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

            newChild.Next = this; // so that the child can see its parent while being reduced
            newChild = newChild.Reduce();
            newChild.Next = this; // in case Reduce returns a different node that needs to be reparented

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

        // Determines whether the node supports a compilation / code generation strategy based on walking the node tree.
        internal bool SupportsCompilation()
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                // If we can't recur further, code generation isn't supported as the tree is too deep.
                return false;
            }

            if ((Options & (RegexOptions.RightToLeft | RegexOptions.NonBacktracking)) != 0)
            {
                // NonBacktracking isn't supported, nor RightToLeft.  The latter applies to both the top-level
                // options as well as when used to specify positive and negative lookbehinds.
                return false;
            }

            int childCount = ChildCount();
            for (int i = 0; i < childCount; i++)
            {
                // The node isn't supported if any of its children aren't supported.
                if (!Child(i).SupportsCompilation())
                {
                    return false;
                }
            }

            // TODO: This should be moved somewhere else, to a pass somewhere where we explicitly
            // annotate the tree, potentially as part of the final optimization pass.  It doesn't
            // belong in this check.
            if (Type == Capture)
            {
                // If we've found a supported capture, mark all of the nodes in its parent hierarchy as containing a capture.
                for (RegexNode? parent = this; parent != null && (parent.Options & HasCapturesFlag) == 0; parent = parent.Next)
                {
                    parent.Options |= HasCapturesFlag;
                }
            }

            // Supported.
            return true;
        }

        /// <summary>Gets whether the node is a Set/Setloop/Setloopatomic/Setlazy node.</summary>
        public bool IsSetFamily => Type is Set or Setloop or Setloopatomic or Setlazy;

        /// <summary>Gets whether the node is a One/Oneloop/Oneloopatomic/Onelazy node.</summary>
        public bool IsOneFamily => Type is One or Oneloop or Oneloopatomic or Onelazy;

        /// <summary>Gets whether the node is a Notone/Notoneloop/Notoneloopatomic/Notonelazy node.</summary>
        public bool IsNotoneFamily => Type is Notone or Notoneloop or Notoneloopatomic or Notonelazy;

        /// <summary>Gets whether this node is contained inside of a loop.</summary>
        public bool IsInLoop()
        {
            for (RegexNode? parent = Next; parent is not null; parent = parent.Next)
            {
                if (parent.Type is Loop or Lazyloop)
                {
                    return true;
                }
            }

            return false;
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

        [ExcludeFromCodeCoverage]
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
                        (N == M) ? $"{{{M}}}" :
                        $"{{{M}, {N}}}");
                    break;
            }

            return sb.ToString();
        }

        [ExcludeFromCodeCoverage]
        public void Dump() => Debug.WriteLine(ToString());

        [ExcludeFromCodeCoverage]
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
