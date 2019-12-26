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

        public const int Oneloopgreedy = RegexCode.Oneloopgreedy;     // c,n      (?> a*)
        public const int Setloopgreedy = RegexCode.Setloopgreedy;     // set,n    (?> \d*)

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
        public const int Greedy = 32;                                 //          (?>)       - greedy subexpression
        public const int Testref = 33;                                //          (?(n) | )  - alternation, reference
        public const int Testgroup = 34;                              //          (?(...) | )- alternation, expression

        public int NType;
        public List<RegexNode>? Children;
        public string? Str;
        public char Ch;
        public int M;
        public int N;
        public readonly RegexOptions Options;
        public RegexNode? Next;

        public RegexNode(int type, RegexOptions options)
        {
            NType = type;
            Options = options;
        }

        public RegexNode(int type, RegexOptions options, char ch)
        {
            NType = type;
            Options = options;
            Ch = ch;
        }

        public RegexNode(int type, RegexOptions options, string str)
        {
            NType = type;
            Options = options;
            Str = str;
        }

        public RegexNode(int type, RegexOptions options, int m)
        {
            NType = type;
            Options = options;
            M = m;
        }

        public RegexNode(int type, RegexOptions options, int m, int n)
        {
            NType = type;
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
            if (UseOptionR() && NType == Concatenate && Children != null)
            {
                Children.Reverse(0, Children.Count);
            }

            return this;
        }

        /// <summary>
        /// Pass type as OneLazy or OneLoop
        /// </summary>
        private void MakeRep(int type, int min, int max)
        {
            NType += (type - One);
            M = min;
            N = max;
        }

        /// <summary>
        /// Removes redundant nodes from the subtree, and returns a reduced subtree.
        /// </summary>
        private RegexNode Reduce()
        {
            RegexNode n;

            switch (Type())
            {
                case Alternate:
                    n = ReduceAlternation();
                    break;

                case Concatenate:
                    n = ReduceConcatenation();
                    break;

                case Loop:
                case Lazyloop:
                    n = ReduceRep();
                    break;

                case Greedy:
                    n = ReduceGreedy();
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

            while (u.Type() == Group)
            {
                Debug.Assert(u.ChildCount() == 1);
                u = u.Child(0);
            }

            return u;
        }

        /// <summary>
        /// Simple optimization. If a greedy subexpression contains only a set loop
        /// or a one loop, change them to be a greedy set loop or greedy one loop,
        /// and remove the greedy node.
        /// </summary>
        private RegexNode ReduceGreedy()
        {
            Debug.Assert(NType == Greedy);
            if (Children != null && Children.Count == 1)
            {
                RegexNode child = Children[0];
                switch (child.NType)
                {
                    case Oneloop:
                        child.NType = Oneloopgreedy;
                        return child;

                    case Setloop:
                        child.NType = Setloopgreedy;
                        return child;

                    case Oneloopgreedy:
                    case Setloopgreedy:
                        return child;
                }
            }

            return this;
        }

        /// <summary>
        /// Nested repeaters just get multiplied with each other if they're not too lumpy
        /// </summary>
        private RegexNode ReduceRep()
        {
            RegexNode u = this;
            int type = Type();
            Debug.Assert(type == Loop || type == Lazyloop);

            int min = M;
            int max = N;

            while (u.ChildCount() > 0)
            {
                RegexNode child = u.Child(0);

                // multiply reps of the same type only
                if (child.Type() != type)
                {
                    bool valid = false;
                    if (type == Loop)
                    {
                        switch (child.Type())
                        {
                            case Oneloop:
                            case Oneloopgreedy:
                            case Notoneloop:
                            case Setloop:
                            case Setloopgreedy:
                                valid = true;
                                break;
                        }
                    }
                    else // type == Lazyloop
                    {
                        switch (child.Type())
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

            return min == int.MaxValue ? new RegexNode(Nothing, Options) : u;
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
                NType = Nothing;
                Str = null;
            }
            else if (RegexCharClass.IsSingleton(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                NType += (One - Set);
            }
            else if (RegexCharClass.IsSingletonInverse(Str))
            {
                Ch = RegexCharClass.SingletonChar(Str);
                Str = null;
                NType += (Notone - Set);
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
            if (Children == null)
                return new RegexNode(Nothing, Options);

            bool wasLastSet = false;
            bool lastNodeCannotMerge = false;
            RegexOptions optionsLast = 0;
            RegexOptions optionsAt;
            int i;
            int j;
            RegexNode at;
            RegexNode prev;

            for (i = 0, j = 0; i < Children.Count; i++, j++)
            {
                at = Children[i];

                if (j < i)
                    Children[j] = at;

                while (true)
                {
                    if (at.NType == Alternate)
                    {
                        for (int k = 0; k < at.Children!.Count; k++)
                            at.Children[k].Next = this;

                        Children.InsertRange(i + 1, at.Children);
                        j--;
                    }
                    else if (at.NType == Set || at.NType == One)
                    {
                        // Cannot merge sets if L or I options differ, or if either are negated.
                        optionsAt = at.Options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                        if (at.NType == Set)
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
                        prev = Children[j];

                        RegexCharClass prevCharClass;
                        if (prev.NType == One)
                        {
                            prevCharClass = new RegexCharClass();
                            prevCharClass.AddChar(prev.Ch);
                        }
                        else
                        {
                            prevCharClass = RegexCharClass.Parse(prev.Str!);
                        }

                        if (at.NType == One)
                        {
                            prevCharClass.AddChar(at.Ch);
                        }
                        else
                        {
                            RegexCharClass atCharClass = RegexCharClass.Parse(at.Str!);
                            prevCharClass.AddCharClass(atCharClass);
                        }

                        prev.NType = Set;
                        prev.Str = prevCharClass.ToStringClass();
                    }
                    else if (at.NType == Nothing)
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
                Children.RemoveRange(j, i - j);

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
            if (Children == null)
            {
                return new RegexNode(Empty, Options);
            }

            bool wasLastString = false;
            RegexOptions optionsLast = 0;
            int i, j;

            for (i = 0, j = 0; i < Children.Count; i++, j++)
            {
                RegexNode at = Children[i];

                if (j < i)
                {
                    Children[j] = at;
                }

                if (at.NType == Concatenate &&
                    ((at.Options & RegexOptions.RightToLeft) == (Options & RegexOptions.RightToLeft)))
                {
                    for (int k = 0; k < at.Children!.Count; k++)
                    {
                        at.Children[k].Next = this;
                    }

                    Children.InsertRange(i + 1, at.Children);
                    j--;
                }
                else if (at.NType == Multi || at.NType == One)
                {
                    // Cannot merge strings if L or I options differ
                    RegexOptions optionsAt = at.Options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

                    if (!wasLastString || optionsLast != optionsAt)
                    {
                        wasLastString = true;
                        optionsLast = optionsAt;
                        continue;
                    }

                    RegexNode prev = Children[--j];

                    if (prev.NType == One)
                    {
                        prev.NType = Multi;
                        prev.Str = prev.Ch.ToString();
                    }

                    if ((optionsAt & RegexOptions.RightToLeft) == 0)
                    {
                        prev.Str += (at.NType == One) ? at.Ch.ToString() : at.Str;
                    }
                    else
                    {
                        prev.Str = (at.NType == One) ? at.Ch.ToString() + prev.Str : at.Str + prev.Str;
                    }
                }
                else if (at.NType == Empty)
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
                Children.RemoveRange(j, i - j);
            }

            // Now try to convert as many loops as possible to be greedy to avoid unnecessary backtracking.
            if ((Options & RegexOptions.RightToLeft) == 0)
            {
                ReduceConcatenateWithAutoGreedy();
            }

            // If the concatenation is now empty, return an empty node, or if it's got a single child, return that child.
            // Otherwise, return this.
            return StripEnation(Empty);
        }

        /// <summary>
        /// Finds oneloop and setloop nodes in the concatenation that can be automatically upgraded
        /// to oneloopgreedy and setloopgreedy nodes.  Such changes avoid potential useless backtracking.
        /// This looks for cases like A*B, where A and B are known to not overlap: in such cases,
        /// we can effectively convert this to (?>A*)B.
        /// </summary>
        private void ReduceConcatenateWithAutoGreedy()
        {
            Debug.Assert(NType == Concatenate);
            Debug.Assert((Options & RegexOptions.RightToLeft) == 0);

            for (int i = 0; i < Children!.Count - 1; i++)
            {
                RegexNode node = Children[i], subsequent = Children[i + 1];

                // Skip down the node past irrelevant capturing and non-capturing groups.
                while (node.NType == Capture || node.NType == Group)
                {
                    Debug.Assert(node.Children!.Count == 1);
                    node = node.Children[0];
                }

                // Skip the successor down to the guaranteed next node.
                while (subsequent.Children != null && subsequent.Children.Count > 0)
                {
                    switch (subsequent.NType)
                    {
                        case Capture:
                        case Group:
                        case Greedy:
                        case Require:
                        case Concatenate:
                        case Loop when subsequent.M > 0:
                        case Lazyloop when subsequent.M > 0:
                            subsequent = subsequent.Children[0];
                            continue;
                    }

                    break;
                }

                // If the two nodes don't agree on case-insensitivity, don't try to optimize.
                // If they're both case sensitive or both case insensitive, then their tokens
                // will be comparable.
                if ((node.Options & RegexOptions.IgnoreCase) != (subsequent.Options & RegexOptions.IgnoreCase))
                {
                    continue;
                }

                // If this node is a oneloop or a setloop, see if it overlaps with its successor in the concatenation.
                // If it doesn't, then we can upgrade it to being a oneloopgreedy or setloopgreedy, respectively.
                // Doing so avoids unnecessary backtracking.
                switch (node.NType)
                {
                    case Oneloop:
                        switch (subsequent.NType)
                        {
                            case One when node.Ch != subsequent.Ch:
                            case Onelazy when subsequent.M > 0 && node.Ch != subsequent.Ch:
                            case Oneloop when subsequent.M > 0 && node.Ch != subsequent.Ch:
                            case Oneloopgreedy when subsequent.M > 0 && node.Ch != subsequent.Ch:
                            case Notone when node.Ch == subsequent.Ch:
                            case Notonelazy when subsequent.M > 0 && node.Ch == subsequent.Ch:
                            case Notoneloop when subsequent.M > 0 && node.Ch == subsequent.Ch:
                            case Multi when !string.IsNullOrEmpty(subsequent.Str) && node.Ch != subsequent.Str[0]:
                            case Set when subsequent.Str != null && !RegexCharClass.CharInClass(node.Ch, subsequent.Str):
                            case Setlazy when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.CharInClass(node.Ch, subsequent.Str):
                            case Setloop when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.CharInClass(node.Ch, subsequent.Str):
                            case Setloopgreedy when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.CharInClass(node.Ch, subsequent.Str):
                                node.NType = Oneloopgreedy;
                                break;
                        }
                        break;

                    case Setloop when node.Str != null:
                        switch (subsequent.NType)
                        {
                            case One when !RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Onelazy when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Oneloop when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Oneloopgreedy when subsequent.M > 0 && !RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Notone when RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Notonelazy when subsequent.M > 0 && RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Notoneloop when subsequent.M > 0 && RegexCharClass.CharInClass(subsequent.Ch, node.Str):
                            case Multi when !string.IsNullOrEmpty(subsequent.Str) && !RegexCharClass.CharInClass(subsequent.Str[0], node.Str):
                            case Set when subsequent.Str != null && !RegexCharClass.MayOverlap(node.Str, subsequent.Str):
                            case Setlazy when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.MayOverlap(node.Str, subsequent.Str):
                            case Setloop when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.MayOverlap(node.Str, subsequent.Str):
                            case Setloopgreedy when subsequent.M > 0 && subsequent.Str != null && !RegexCharClass.MayOverlap(node.Str, subsequent.Str):
                                node.NType = Setloopgreedy;
                                break;
                        }
                        break;
                }
            }
        }

        public RegexNode MakeQuantifier(bool lazy, int min, int max)
        {
            if (min == 0 && max == 0)
                return new RegexNode(Empty, Options);

            if (min == 1 && max == 1)
                return this;

            switch (NType)
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
            Children ??= new List<RegexNode>();

            RegexNode reducedChild = newChild.Reduce();
            Children.Add(reducedChild);
            reducedChild.Next = this;
        }

        public RegexNode Child(int i)
        {
            return Children![i];
        }

        public int ChildCount()
        {
            return Children == null ? 0 : Children.Count;
        }

        public int Type()
        {
            return NType;
        }

#if DEBUG
        private const string Space = "                                ";
        private static readonly string[] s_typeStr = new string[] {
            "Onerep", "Notonerep", "Setrep",
            "Oneloop", "Notoneloop", "Setloop",
            "Onelazy", "Notonelazy", "Setlazy",
            "One", "Notone", "Set",
            "Multi", "Ref",
            "Bol", "Eol", "Boundary", "Nonboundary",
            "Beginning", "Start", "EndZ", "End",
            "Nothing", "Empty",
            "Alternate", "Concatenate",
            "Loop", "Lazyloop",
            "Capture", "Group", "Require", "Prevent", "Greedy",
            "Testref", "Testgroup",
            "", "", "", "", "", "",
            "ECMABoundary", "NonECMABoundary",
            "Oneloopgreedy", "Setloopgreedy",
        };

        private string Description()
        {
            StringBuilder ArgSb = new StringBuilder();

            ArgSb.Append(s_typeStr[NType]);

            if ((Options & RegexOptions.ExplicitCapture) != 0)
                ArgSb.Append("-C");
            if ((Options & RegexOptions.IgnoreCase) != 0)
                ArgSb.Append("-I");
            if ((Options & RegexOptions.RightToLeft) != 0)
                ArgSb.Append("-L");
            if ((Options & RegexOptions.Multiline) != 0)
                ArgSb.Append("-M");
            if ((Options & RegexOptions.Singleline) != 0)
                ArgSb.Append("-S");
            if ((Options & RegexOptions.IgnorePatternWhitespace) != 0)
                ArgSb.Append("-X");
            if ((Options & RegexOptions.ECMAScript) != 0)
                ArgSb.Append("-E");

            switch (NType)
            {
                case Oneloop:
                case Oneloopgreedy:
                case Notoneloop:
                case Onelazy:
                case Notonelazy:
                case One:
                case Notone:
                    ArgSb.Append("(Ch = " + RegexCharClass.CharDescription(Ch) + ")");
                    break;
                case Capture:
                    ArgSb.Append("(index = " + M.ToString(CultureInfo.InvariantCulture) + ", unindex = " + N.ToString(CultureInfo.InvariantCulture) + ")");
                    break;
                case Ref:
                case Testref:
                    ArgSb.Append("(index = " + M.ToString(CultureInfo.InvariantCulture) + ")");
                    break;
                case Multi:
                    ArgSb.Append("(String = " + Str + ")");
                    break;
                case Set:
                case Setloop:
                case Setloopgreedy:
                case Setlazy:
                    ArgSb.Append("(Set = " + RegexCharClass.SetDescription(Str!) + ")");
                    break;
            }

            switch (NType)
            {
                case Oneloop:
                case Oneloopgreedy:
                case Notoneloop:
                case Onelazy:
                case Notonelazy:
                case Setloop:
                case Setloopgreedy:
                case Setlazy:
                case Loop:
                case Lazyloop:
                    ArgSb.Append("(Min = " + M.ToString(CultureInfo.InvariantCulture) + ", Max = " + (N == int.MaxValue ? "inf" : Convert.ToString(N, CultureInfo.InvariantCulture)) + ")");
                    break;
            }

            return ArgSb.ToString();
        }

        public void Dump()
        {
            List<int> Stack = new List<int>();
            RegexNode? CurNode;
            int CurChild;

            CurNode = this;
            CurChild = 0;

            Debug.WriteLine(CurNode.Description());

            while (true)
            {
                if (CurNode!.Children != null && CurChild < CurNode.Children.Count)
                {
                    Stack.Add(CurChild + 1);
                    CurNode = CurNode.Children[CurChild];
                    CurChild = 0;

                    int Depth = Stack.Count;
                    if (Depth > 32)
                        Depth = 32;

                    Debug.WriteLine(Space.Substring(0, Depth) + CurNode.Description());
                }
                else
                {
                    if (Stack.Count == 0)
                        break;

                    CurChild = Stack[Stack.Count - 1];
                    Stack.RemoveAt(Stack.Count - 1);
                    CurNode = CurNode.Next;
                }
            }
        }
#endif
    }
}
