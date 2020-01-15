// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public const int Nonboundary = RegexCode.Nonboundary;         //          \B
        public const int ECMABoundary = RegexCode.ECMABoundary;       // \b
        public const int NonECMABoundary = RegexCode.NonECMABoundary; // \B
        public const int Beginning = RegexCode.Beginning;             //          \A
        public const int Start = RegexCode.Start;                     //          \G
        public const int EndZ = RegexCode.EndZ;                       //          \Z
        public const int End = RegexCode.End;                         //          \z

        public const int Oneloopatomic = RegexCode.Oneloopatomic;        // c,n      (?> a*)
        public const int Notoneloopatomic = RegexCode.Notoneloopatomic;  // c,n      (?> .*)
        public const int Setloopatomic = RegexCode.Setloopatomic;        // set,n    (?> \d*)

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

        /// <summary>Performs additional optimizations on an entire tree prior to being used.</summary>
        internal RegexNode FinalOptimize()
        {
            RegexNode rootNode = this;
            Debug.Assert(rootNode.Type == Capture && rootNode.ChildCount() == 1);

            // If we find backtracking construct at the end of the regex, we can instead make it non-backtracking,
            // since nothing would ever backtrack into it anyway.  Doing this then makes the construct available
            // to implementations that don't support backtracking.
            if ((Options & RegexOptions.RightToLeft) == 0 && // only apply optimization when LTR to avoid needing additional code for the rarer RTL case
                (Options & RegexOptions.Compiled) != 0) // only apply when we're compiling, as that's the only time it would make a meaningful difference
            {
                // Walk the tree, starting from the sole child of the root implicit capture.
                RegexNode node = rootNode.Child(0);
                while (true)
                {
                    switch (node.Type)
                    {
                        case Oneloop:
                            node.Type = Oneloopatomic;
                            break;

                        case Notoneloop:
                            node.Type = Notoneloopatomic;
                            break;

                        case Setloop:
                            node.Type = Setloopatomic;
                            break;

                        case Capture:
                        case Concatenate:
                            RegexNode existingChild = node.Child(node.ChildCount() - 1);
                            switch (existingChild.Type)
                            {
                                default:
                                    node = existingChild;
                                    break;

                                case Alternate:
                                case Loop:
                                case Lazyloop:
                                    var atomic = new RegexNode(Atomic, Options);
                                    atomic.AddChild(existingChild);
                                    node.ReplaceChild(node.ChildCount() - 1, atomic);
                                    break;
                            }
                            continue;

                        case Atomic:
                            node = node.Child(0);
                            continue;
                    }

                    break;
                }
            }

            // If the root node under the implicit Capture is an Atomic, the Atomic is useless as there's nothing
            // to backtrack into it, so we can remove it.
            if (rootNode.Child(0).Type == Atomic)
            {
                rootNode.ReplaceChild(0, rootNode.Child(0).Child(0));
            }

            // Done optimizing.  Return the final tree.
            return rootNode;
        }

        /// <summary>
        /// Removes redundant nodes from the subtree, and returns a reduced subtree.
        /// </summary>
        private RegexNode Reduce()
        {
            RegexNode n;

            switch (Type)
            {
                case Alternate:
                    n = ReduceAlternation();
                    break;

                case Concatenate:
                    n = ReduceConcatenation();
                    break;

                case Loop:
                case Lazyloop:
                    n = ReduceLoops();
                    break;

                case Atomic:
                    n = ReduceAtomic();
                    break;

                case Group:
                    n = ReduceGroup();
                    break;

                case Set:
                case Setloop:
                    n = ReduceSet();
                    break;

                default:
                    n = this;
                    break;
            }

            return n;
        }

        /// <summary>
        /// Simple optimization. If a concatenation or alternation has only
        /// one child strip out the intermediate node. If it has zero children,
        /// turn it into an empty.
        /// </summary>
        private RegexNode StripEnation(int emptyType) =>
            ChildCount() switch
            {
                0 => new RegexNode(emptyType, Options),
                1 => Child(0),
                _ => this,
            };

        /// <summary>
        /// Simple optimization. Once parsed into a tree, non-capturing groups
        /// serve no function, so strip them out.
        /// </summary>
        private RegexNode ReduceGroup()
        {
            RegexNode u = this;

            while (u.Type == Group)
            {
                Debug.Assert(u.ChildCount() == 1);
                u = u.Child(0);
            }

            return u;
        }

        /// <summary>
        /// Simple optimization. If an atomic subexpression contains only a one/notone/set loop,
        /// change it to be an atomic one/notone/set loop and remove the atomic node.
        /// </summary>
        private RegexNode ReduceAtomic()
        {
            Debug.Assert(Type == Atomic);
            Debug.Assert(ChildCount() == 1);

            RegexNode child = Child(0);
            switch (child.Type)
            {
                case Oneloop:
                    child.Type = Oneloopatomic;
                    return child;

                case Notoneloop:
                    child.Type = Notoneloopatomic;
                    return child;

                case Setloop:
                    child.Type = Setloopatomic;
                    return child;

                case Oneloopatomic:
                case Notoneloopatomic:
                case Setloopatomic:
                    return child;
            }

            return this;
        }

        /// <summary>
        /// Nested repeaters just get multiplied with each other if they're not too lumpy.
        /// Other optimizations may have also resulted in {Lazy}loops directly containing
        /// sets, ones, and notones, in which case they can be transformed into the corresponding
        /// individual looping constructs.
        /// </summary>
        private RegexNode ReduceLoops()
        {
            RegexNode u = this;
            int type = Type;
            Debug.Assert(type == Loop || type == Lazyloop);

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
        /// Simple optimization. If a set is a singleton, an inverse singleton, or empty, it's transformed accordingly.
        /// </summary>
        private RegexNode ReduceSet()
        {
            // Extract empty-set, one, and not-one case as special

            Debug.Assert(Str != null);

            if (RegexCharClass.IsEmpty(Str))
            {
                Type = Nothing;
                Str = null;
            }
            else if (RegexCharClass.IsSingleton(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Type += (One - Set);
            }
            else if (RegexCharClass.IsSingletonInverse(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                Type += (Notone - Set);
            }

            return this;
        }

        /// <summary>
        /// Combine adjacent sets/chars.
        /// Basic optimization. Single-letter alternations can be replaced
        /// by faster set specifications, and nested alternations with no
        /// intervening operators can be flattened:
        ///
        /// a|b|c|def|g|h -> [a-c]|def|[gh]
        /// apple|(?:orange|pear)|grape -> apple|orange|pear|grape
        /// </summary>
        private RegexNode ReduceAlternation()
        {
            int childCount = ChildCount();
            if (childCount == 0)
            {
                return new RegexNode(Nothing, Options);
            }

            if (childCount == 1)
            {
                return Child(0);
            }

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
                            if (!wasLastSet || optionsLast != optionsAt || lastNodeCannotMerge || !RegexCharClass.IsMergeable(at.Str))
                            {
                                wasLastSet = true;
                                lastNodeCannotMerge = !RegexCharClass.IsMergeable(at.Str);
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

            return StripEnation(Nothing);
        }

        /// <summary>
        /// Eliminate empties and concat adjacent strings/chars.
        /// Basic optimization. Adjacent strings can be concatenated.
        ///
        /// (?:abc)(?:def) -> abcdef
        /// </summary>
        private RegexNode ReduceConcatenation()
        {
            int childCount = ChildCount();
            if (childCount == 0)
            {
                return new RegexNode(Empty, Options);
            }

            if (childCount == 1)
            {
                return Child(0);
            }

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
                        prev.Str += (at.Type == One) ? at.Ch.ToString() : at.Str;
                    }
                    else
                    {
                        prev.Str = (at.Type == One) ? at.Ch.ToString() + prev.Str : at.Str + prev.Str;
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

            // Now try to convert as many loops as possible to be atomic to avoid unnecessary backtracking.
            if ((Options & RegexOptions.RightToLeft) == 0)
            {
                ReduceConcatenateWithAutoAtomic();
            }

            // If the concatenation is now empty, return an empty node, or if it's got a single child, return that child.
            // Otherwise, return this.
            return StripEnation(Empty);
        }

        /// <summary>
        /// Finds one/notone/setloop nodes in the concatenation that can be automatically upgraded
        /// to one/notone/setloopatomic nodes.  Such changes avoid potential useless backtracking.
        /// This looks for cases like A*B, where A and B are known to not overlap: in such cases,
        /// we can effectively convert this to (?>A*)B.
        /// </summary>
        private void ReduceConcatenateWithAutoAtomic()
        {
            Debug.Assert(Type == Concatenate);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);
            Debug.Assert(Children is List<RegexNode>);

            List<RegexNode> children = (List<RegexNode>)Children;
            for (int i = 0; i < children.Count - 1; i++)
            {
                RegexNode node = children[i], subsequent = children[i + 1];

                // Skip down the node past irrelevant nodes.  We don't need to
                // skip Groups, as they should have already been reduced away.
                // If there's a concatenation, we can jump to the last element of it.
                while (node.Type == Capture || node.Type == Concatenate)
                {
                    node = node.Child(node.ChildCount() - 1);
                }
                Debug.Assert(node.Type != Group);

                // If the node can be changed to atomic based on what comes after it, do so.
                switch (node.Type)
                {
                    case Oneloop when CanBeMadeAtomic(node, subsequent):
                        node.Type = Oneloopatomic;
                        break;
                    case Notoneloop when CanBeMadeAtomic(node, subsequent):
                        node.Type = Notoneloopatomic;
                        break;
                    case Setloop when CanBeMadeAtomic(node, subsequent):
                        node.Type = Setloopatomic;
                        break;
                }

                // Determines whether node can be switched to an atomic loop.  Subsequent is the node
                // immediately after 'node'.
                static bool CanBeMadeAtomic(RegexNode node, RegexNode subsequent, int maxDepth = 20)
                {
                    if (maxDepth <= 0)
                    {
                        // We hit our recursion limit.  Just don't apply the optimization.
                        return false;
                    }

                    // Skip the successor down to the guaranteed next node.
                    while (subsequent.ChildCount() > 0)
                    {
                        Debug.Assert(subsequent.Type != Group);
                        switch (subsequent.Type)
                        {
                            case Concatenate:
                            case Capture:
                            case Atomic:
                            case Require:
                            case Loop when subsequent.M > 0:
                            case Lazyloop when subsequent.M > 0:
                                subsequent = subsequent.Child(0);
                                continue;
                        }

                        break;
                    }

                    // If the two nodes don't agree on case-insensitivity, don't try to optimize.
                    // If they're both case sensitive or both case insensitive, then their tokens
                    // will be comparable.
                    if ((node.Options & RegexOptions.IgnoreCase) != (subsequent.Options & RegexOptions.IgnoreCase))
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

                    // If this node is a one/notone/setloop, see if it overlaps with its successor in the concatenation.
                    // If it doesn't, then we can upgrade it to being a one/notone/setloopatomic.
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
                                case Nonboundary when !RegexCharClass.IsWordChar(node.Ch):
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
                                case Notone when RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                                case Notonelazy when subsequent.M > 0 && RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                                case Notoneloop when subsequent.M > 0 && RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                                case Notoneloopatomic when subsequent.M > 0 && RegexCharClass.CharInClass(subsequent.Ch, node.Str!):
                                case Multi when !RegexCharClass.CharInClass(subsequent.Str![0], node.Str!):
                                case Set when !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                                case Setlazy when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                                case Setloop when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                                case Setloopatomic when subsequent.M > 0 && !RegexCharClass.MayOverlap(node.Str!, subsequent.Str!):
                                case End:
                                case EndZ when !RegexCharClass.CharInClass('\n', node.Str!):
                                case Eol when !RegexCharClass.CharInClass('\n', node.Str!):
                                case Boundary when node.Str == RegexCharClass.WordClass || node.Str == RegexCharClass.DigitClass: // TODO: Expand these with a more inclusive overlap check that considers categories
                                case Nonboundary when node.Str == RegexCharClass.NotWordClass || node.Str == RegexCharClass.NotDigitClass:
                                case ECMABoundary when node.Str == RegexCharClass.ECMAWordClass || node.Str == RegexCharClass.ECMADigitClass:
                                case NonECMABoundary when node.Str == RegexCharClass.NotECMAWordClass || node.Str == RegexCharClass.NotDigitClass:
                                    return true;
                            }
                            break;
                    }

                    return false;
                }
            }
        }

        /// <summary>Computes a min bound on the required length of any string that could possibly match.</summary>
        /// <returns>The min computed length.  If the result is 0, there is no minimum we can enforce.</returns>
        public int ComputeMinLength()
        {
            return ComputeMinLength(this, 20); // arbitrary cut-off to avoid stack overflow with degenerate expressions

            static int ComputeMinLength(RegexNode node, int maxDepth)
            {
                if (maxDepth == 0)
                {
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
                        return node.M * ComputeMinLength(node.Child(0), maxDepth - 1);

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
                            int sum = 0;
                            int childCount = node.ChildCount();
                            for (int i = 0; i < childCount; i++)
                            {
                                sum += ComputeMinLength(node.Child(i), maxDepth - 1);
                            }
                            return sum;
                        }

                    case Atomic:
                    case Capture:
                    case Group:
                        // For groups, we just delegate to the sole child.
                        Debug.Assert(node.ChildCount() == 1);
                        return ComputeMinLength(node.Child(0), maxDepth - 1);

                    case Empty:
                    case Nothing:
                    // Nothing to match.
                    case Beginning:
                    case Bol:
                    case Boundary:
                    case ECMABoundary:
                    case End:
                    case EndZ:
                    case Eol:
                    case Nonboundary:
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
                        Debug.Fail($"Unknown node: {node.Type}");
                        return 0;
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
            RegexNode reducedChild = newChild.Reduce();
            reducedChild.Next = this;

            if (Children is null)
            {
                Children = reducedChild;
            }
            else if (Children is RegexNode currentChild)
            {
                Children = new List<RegexNode>() { currentChild, reducedChild };
            }
            else
            {
                ((List<RegexNode>)Children).Add(reducedChild);
            }
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
        [ExcludeFromCodeCoverage]
        public string Description()
        {

            string typeStr = Type switch
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
                Nonboundary => nameof(Nonboundary),
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
                _ => $"(unknown {Type})"
            };

            var argSb = new StringBuilder().Append(typeStr);

            if ((Options & RegexOptions.ExplicitCapture) != 0) argSb.Append("-C");
            if ((Options & RegexOptions.IgnoreCase) != 0) argSb.Append("-I");
            if ((Options & RegexOptions.RightToLeft) != 0) argSb.Append("-L");
            if ((Options & RegexOptions.Multiline) != 0) argSb.Append("-M");
            if ((Options & RegexOptions.Singleline) != 0) argSb.Append("-S");
            if ((Options & RegexOptions.IgnorePatternWhitespace) != 0) argSb.Append("-X");
            if ((Options & RegexOptions.ECMAScript) != 0) argSb.Append("-E");

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
                    argSb.Append("(Ch = " + RegexCharClass.CharDescription(Ch) + ")");
                    break;
                case Capture:
                    argSb.Append("(index = " + M.ToString(CultureInfo.InvariantCulture) + ", unindex = " + N.ToString(CultureInfo.InvariantCulture) + ")");
                    break;
                case Ref:
                case Testref:
                    argSb.Append("(index = " + M.ToString(CultureInfo.InvariantCulture) + ")");
                    break;
                case Multi:
                    argSb.Append("(String = " + Str + ")");
                    break;
                case Set:
                case Setloop:
                case Setloopatomic:
                case Setlazy:
                    argSb.Append("(Set = " + RegexCharClass.SetDescription(Str!) + ")");
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
                    argSb.Append("(Min = " + M.ToString(CultureInfo.InvariantCulture) + ", Max = " + (N == int.MaxValue ? "inf" : Convert.ToString(N, CultureInfo.InvariantCulture)) + ")");
                    break;
            }

            return argSb.ToString();
        }

        [ExcludeFromCodeCoverage]
        public void Dump()
        {
            List<int> stack = new List<int>();
            RegexNode? curNode = this;
            int curChild = 0;

            Debug.WriteLine(curNode.Description());

            while (true)
            {
                if (curChild < curNode!.ChildCount())
                {
                    stack.Add(curChild + 1);
                    curNode = curNode.Child(curChild);
                    curChild = 0;

                    Debug.WriteLine(new string(' ', stack.Count) + curNode.Description());
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
        }
#endif
    }
}
