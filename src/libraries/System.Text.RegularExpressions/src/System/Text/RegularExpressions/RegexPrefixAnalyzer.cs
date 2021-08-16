// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>Detects various forms of prefixes in the regular expression that can help FindFirstChars optimize its search.</summary>
    internal ref struct RegexPrefixAnalyzer
    {
        private const int StackBufferSize = 32;
        private const int BeforeChild = 64;
        private const int AfterChild = 128;

        // where the regex can be pegged
        public const int Beginning = 0x0001;
        public const int Bol = 0x0002;
        public const int Start = 0x0004;
        public const int Eol = 0x0008;
        public const int EndZ = 0x0010;
        public const int End = 0x0020;
        public const int Boundary = 0x0040;
        public const int ECMABoundary = 0x0080;

        private readonly List<RegexFC> _fcStack;
        private ValueListBuilder<int> _intStack;    // must not be readonly
        private bool _skipAllChildren;              // don't process any more children at the current level
        private bool _skipchild;                    // don't process the current child.
        private bool _failed;

        private RegexPrefixAnalyzer(Span<int> intStack)
        {
            _fcStack = new List<RegexFC>(StackBufferSize);
            _intStack = new ValueListBuilder<int>(intStack);
            _failed = false;
            _skipchild = false;
            _skipAllChildren = false;
        }

        /// <summary>Computes the leading substring in <paramref name="tree"/>.</summary>
        /// <remarks>It's quite trivial and gives up easily, in which case an empty string is returned.</remarks>
        public static (string Prefix, bool CaseInsensitive) ComputeLeadingSubstring(RegexTree tree)
        {
            RegexNode curNode = tree.Root;
            RegexNode? concatNode = null;
            int nextChild = 0;

            while (true)
            {
                switch (curNode.Type)
                {
                    case RegexNode.Concatenate:
                        if (curNode.ChildCount() > 0)
                        {
                            concatNode = curNode;
                            nextChild = 0;
                        }
                        break;

                    case RegexNode.Atomic:
                    case RegexNode.Capture:
                        curNode = curNode.Child(0);
                        concatNode = null;
                        continue;

                    case RegexNode.Oneloop:
                    case RegexNode.Oneloopatomic:
                    case RegexNode.Onelazy:

                        // In release, cutoff at a length to which we can still reasonably construct a string and Boyer-Moore search.
                        // In debug, use a smaller cutoff to exercise the cutoff path in tests
                        const int Cutoff =
#if DEBUG
                            50;
#else
                            RegexBoyerMoore.MaxLimit;
#endif

                        if (curNode.M > 0 && curNode.M < Cutoff)
                        {
                            return (new string(curNode.Ch, curNode.M), (curNode.Options & RegexOptions.IgnoreCase) != 0);
                        }

                        return (string.Empty, false);

                    case RegexNode.One:
                        return (curNode.Ch.ToString(), (curNode.Options & RegexOptions.IgnoreCase) != 0);

                    case RegexNode.Multi:
                        return (curNode.Str!, (curNode.Options & RegexOptions.IgnoreCase) != 0);

                    case RegexNode.Bol:
                    case RegexNode.Eol:
                    case RegexNode.Boundary:
                    case RegexNode.ECMABoundary:
                    case RegexNode.Beginning:
                    case RegexNode.Start:
                    case RegexNode.EndZ:
                    case RegexNode.End:
                    case RegexNode.Empty:
                    case RegexNode.Require:
                    case RegexNode.Prevent:
                        break;

                    default:
                        return (string.Empty, false);
                }

                if (concatNode == null || nextChild >= concatNode.ChildCount())
                {
                    return (string.Empty, false);
                }

                curNode = concatNode.Child(nextChild++);
            }
        }

        /// <summary>Computes a character class for the first character in <paramref name="tree"/>.</summary>
        /// <remarks>true if a character class could be computed; otherwise, false.</remarks>
        public static (string CharClass, bool CaseInsensitive)[]? ComputeFirstCharClass(RegexTree tree)
        {
            var s = new RegexPrefixAnalyzer(stackalloc int[StackBufferSize]);
            RegexFC? fc = s.RegexFCFromRegexTree(tree);
            s.Dispose();

            if (fc == null || fc._nullable)
            {
                return null;
            }

            if (fc.CaseInsensitive)
            {
                fc.AddLowercase(((tree.Options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            return new[] { (fc.GetFirstChars(), fc.CaseInsensitive) };
        }

        /// <summary>Computes character classes for the first <paramref name="maxChars"/> characters in <paramref name="tree"/>.</summary>
        /// <remarks>
        /// For example, given "hello|world" and a <paramref name="maxChars"/> of 3, this will compute the sets [hw], [eo], and [lr].
        /// As with some of the other computations, it's quite trivial and gives up easily; for example, we could in
        /// theory handle nodes in a concatenation after an alternation, but we look only at the branches of the
        /// alternation itself.  As this computation is intended primarily to handle global alternations, it's currently
        /// a reasonable tradeoff between simplicity, performance, and the fullness of potential optimizations.
        /// </remarks>
        public static (string CharClass, bool CaseInsensitive)[]? ComputeMultipleCharClasses(RegexTree tree, int maxChars)
        {
            Debug.Assert(maxChars > 1);

            if ((tree.Options & RegexOptions.RightToLeft) != 0)
            {
                // We don't bother for RightToLeft.  It's rare and adds non-trivial complication.
                return null;
            }

            // The known minimum required length will have already factored in knowledge about alternations.
            // If the known min length is less than the maximum number of chars requested, we can
            // cut this short.  If it's zero, there's nothing to be found.  If it's one, we won't do
            // any better than ComputeFirstCharClass (and likely worse).  Otherwise, don't bother looking for more
            // the min of the min length and the max requested chars.
            maxChars = Math.Min(tree.MinRequiredLength, maxChars);
            if (maxChars <= 1)
            {
                return null;
            }

            // Find an alternation on the path to the first node.  If we can't, bail.
            RegexNode node = tree.Root;
            while (node.Type != RegexNode.Alternate)
            {
                switch (node.Type)
                {
                    case RegexNode.Atomic:
                    case RegexNode.Capture:
                    case RegexNode.Concatenate:
                        node = node.Child(0);
                        break;

                    default:
                        return null;
                }
            }
            Debug.Assert(node.Type == RegexNode.Alternate);

            // Create RegexCharClasses to store the built-up sets.  We may end up returning fewer
            // than this if we find we can't easily fill this number of sets with 100% confidence.
            var classes = new RegexCharClass?[maxChars];
            bool caseInsensitive = false;

            int branches = node.ChildCount();
            Debug.Assert(branches >= 2);
            for (int branchNum = 0; branchNum < branches; branchNum++)
            {
                RegexNode alternateBranch = node.Child(branchNum);
                caseInsensitive |= (alternateBranch.Options & RegexOptions.IgnoreCase) != 0;

                switch (alternateBranch.Type)
                {
                    case RegexNode.Multi:
                        maxChars = Math.Min(maxChars, alternateBranch.Str!.Length);
                        for (int i = 0; i < maxChars; i++)
                        {
                            (classes[i] ??= new RegexCharClass()).AddChar(alternateBranch.Str[i]);
                        }
                        continue;

                    case RegexNode.Concatenate:
                        {
                            int classPos = 0;
                            int concatChildren = alternateBranch.ChildCount();
                            for (int i = 0; i < concatChildren && classPos < classes.Length; i++)
                            {
                                RegexNode concatChild = alternateBranch.Child(i);
                                caseInsensitive |= (concatChild.Options & RegexOptions.IgnoreCase) != 0;

                                switch (concatChild.Type)
                                {
                                    case RegexNode.One:
                                        (classes[classPos++] ??= new RegexCharClass()).AddChar(concatChild.Ch);
                                        break;
                                    case RegexNode.Set:
                                        if (!(classes[classPos++] ??= new RegexCharClass()).TryAddCharClass(RegexCharClass.Parse(concatChild.Str!)))
                                        {
                                            // If the classes can't be merged, give up.
                                            return null;
                                        }
                                        break;
                                    case RegexNode.Multi:
                                        for (int c = 0; c < concatChild.Str!.Length && classPos < classes.Length; c++)
                                        {
                                            (classes[classPos++] ??= new RegexCharClass()).AddChar(concatChild.Str[c]);
                                        }
                                        break;

                                    default: // nothing else supported
                                        i = concatChildren; // stop looking at additional nodes
                                        break;
                                }
                            }

                            maxChars = Math.Min(maxChars, classPos);
                        }
                        continue;

                    default:
                        // Any other node type as a branch in the alternation and we give up.  Note that we don't special-case One/Notone/Set
                        // because that would mean the whole branch was a single char, in which case this computation provides
                        // zero benefit over the ComputeFirstCharClass computation.
                        return null;
                }
            }

            // We've now examined all of the alternate branches and were able to successfully process them.
            // Determine how many we can actually return.
            for (int i = 0; i < maxChars; i++)
            {
                if (classes[i] is null)
                {
                    maxChars = i;
                    break;
                }
            }

            // Make sure we got something.
            if (maxChars == 0)
            {
                return null;
            }

            // Create and return the RegexPrefix objects.
            var prefixes = new (string CharClass, bool CaseInsensitive)[maxChars];

            CultureInfo? ci = null;
            if (caseInsensitive)
            {
                ci = (tree.Options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            }

            for (int i = 0; i < prefixes.Length; i++)
            {
                if (caseInsensitive)
                {
                    classes[i]!.AddLowercase(ci!);
                }
                prefixes[i] = (classes[i]!.ToStringClass(), caseInsensitive);
            }

            return prefixes;
        }

        /// <summary>Takes a RegexTree and computes the leading anchor that it encounters.</summary>
        public static int FindLeadingAnchor(RegexTree tree)
        {
            RegexNode curNode = tree.Root;
            RegexNode? concatNode = null;
            int nextChild = 0;

            while (true)
            {
                switch (curNode.Type)
                {
                    case RegexNode.Bol:
                        return Bol;

                    case RegexNode.Eol:
                        return Eol;

                    case RegexNode.Boundary:
                        return Boundary;

                    case RegexNode.ECMABoundary:
                        return ECMABoundary;

                    case RegexNode.Beginning:
                        return Beginning;

                    case RegexNode.Start:
                        return Start;

                    case RegexNode.EndZ:
                        return EndZ;

                    case RegexNode.End:
                        return End;

                    case RegexNode.Concatenate:
                        if (curNode.ChildCount() > 0)
                        {
                            concatNode = curNode;
                            nextChild = 0;
                        }
                        break;

                    case RegexNode.Atomic:
                    case RegexNode.Capture:
                        curNode = curNode.Child(0);
                        concatNode = null;
                        continue;

                    case RegexNode.Empty:
                    case RegexNode.Require:
                    case RegexNode.Prevent:
                        break;

                    default:
                        return 0;
                }

                if (concatNode == null || nextChild >= concatNode.ChildCount())
                {
                    return 0;
                }

                curNode = concatNode.Child(nextChild++);
            }
        }

#if DEBUG
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        public static string AnchorDescription(int anchors)
        {
            var sb = new StringBuilder();

            if ((anchors & Beginning) != 0) sb.Append(", Beginning");
            if ((anchors & Start) != 0) sb.Append(", Start");
            if ((anchors & Bol) != 0) sb.Append(", Bol");
            if ((anchors & Boundary) != 0) sb.Append(", Boundary");
            if ((anchors & ECMABoundary) != 0) sb.Append(", ECMABoundary");
            if ((anchors & Eol) != 0) sb.Append(", Eol");
            if ((anchors & End) != 0) sb.Append(", End");
            if ((anchors & EndZ) != 0) sb.Append(", EndZ");

            return sb.Length >= 2 ?
                sb.ToString(2, sb.Length - 2) :
                "None";
        }
#endif

        /// <summary>
        /// To avoid recursion, we use a simple integer stack.
        /// </summary>
        private void PushInt(int i) => _intStack.Append(i);

        private bool IntIsEmpty() => _intStack.Length == 0;

        private int PopInt() => _intStack.Pop();

        /// <summary>
        /// We also use a stack of RegexFC objects.
        /// </summary>
        private void PushFC(RegexFC fc) => _fcStack.Add(fc);

        private bool FCIsEmpty() => _fcStack.Count == 0;

        private RegexFC PopFC()
        {
            RegexFC item = TopFC();
            _fcStack.RemoveAt(_fcStack.Count - 1);
            return item;
        }

        private RegexFC TopFC() => _fcStack[_fcStack.Count - 1];

        /// <summary>
        /// Return rented buffers.
        /// </summary>
        public void Dispose() => _intStack.Dispose();

        /// <summary>
        /// The main FC computation. It does a shortcutted depth-first walk
        /// through the tree and calls CalculateFC to emits code before
        /// and after each child of an interior node, and at each leaf.
        /// </summary>
        private RegexFC? RegexFCFromRegexTree(RegexTree tree)
        {
            RegexNode? curNode = tree.Root;
            int curChild = 0;

            while (true)
            {
                int curNodeChildCount = curNode.ChildCount();
                if (curNodeChildCount == 0)
                {
                    // This is a leaf node
                    CalculateFC(curNode.Type, curNode, 0);
                }
                else if (curChild < curNodeChildCount && !_skipAllChildren)
                {
                    // This is an interior node, and we have more children to analyze
                    CalculateFC(curNode.Type | BeforeChild, curNode, curChild);

                    if (!_skipchild)
                    {
                        curNode = curNode.Child(curChild);
                        // this stack is how we get a depth first walk of the tree.
                        PushInt(curChild);
                        curChild = 0;
                    }
                    else
                    {
                        curChild++;
                        _skipchild = false;
                    }
                    continue;
                }

                // This is an interior node where we've finished analyzing all the children, or
                // the end of a leaf node.
                _skipAllChildren = false;

                if (IntIsEmpty())
                    break;

                curChild = PopInt();
                curNode = curNode.Next;

                CalculateFC(curNode!.Type | AfterChild, curNode, curChild);
                if (_failed)
                    return null;

                curChild++;
            }

            if (FCIsEmpty())
                return null;

            return PopFC();
        }

        /// <summary>
        /// Called in Beforechild to prevent further processing of the current child
        /// </summary>
        private void SkipChild() => _skipchild = true;

        /// <summary>
        /// FC computation and shortcut cases for each node type
        /// </summary>
        private void CalculateFC(int NodeType, RegexNode node, int CurIndex)
        {
            bool ci = (node.Options & RegexOptions.IgnoreCase) != 0;
            bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

            switch (NodeType)
            {
                case RegexNode.Concatenate | BeforeChild:
                case RegexNode.Alternate | BeforeChild:
                case RegexNode.Testref | BeforeChild:
                case RegexNode.Loop | BeforeChild:
                case RegexNode.Lazyloop | BeforeChild:
                    break;

                case RegexNode.Testgroup | BeforeChild:
                    if (CurIndex == 0)
                        SkipChild();
                    break;

                case RegexNode.Empty:
                    PushFC(new RegexFC(true));
                    break;

                case RegexNode.Concatenate | AfterChild:
                    if (CurIndex != 0)
                    {
                        RegexFC child = PopFC();
                        RegexFC cumul = TopFC();

                        _failed = !cumul.AddFC(child, true);
                    }

                    if (!TopFC()._nullable)
                        _skipAllChildren = true;
                    break;

                case RegexNode.Testgroup | AfterChild:
                    if (CurIndex > 1)
                    {
                        RegexFC child = PopFC();
                        RegexFC cumul = TopFC();

                        _failed = !cumul.AddFC(child, false);
                    }
                    break;

                case RegexNode.Alternate | AfterChild:
                case RegexNode.Testref | AfterChild:
                    if (CurIndex != 0)
                    {
                        RegexFC child = PopFC();
                        RegexFC cumul = TopFC();

                        _failed = !cumul.AddFC(child, false);
                    }
                    break;

                case RegexNode.Loop | AfterChild:
                case RegexNode.Lazyloop | AfterChild:
                    if (node.M == 0)
                        TopFC()._nullable = true;
                    break;

                case RegexNode.Group | BeforeChild:
                case RegexNode.Group | AfterChild:
                case RegexNode.Capture | BeforeChild:
                case RegexNode.Capture | AfterChild:
                case RegexNode.Atomic | BeforeChild:
                case RegexNode.Atomic | AfterChild:
                    break;

                case RegexNode.Require | BeforeChild:
                case RegexNode.Prevent | BeforeChild:
                    SkipChild();
                    PushFC(new RegexFC(true));
                    break;

                case RegexNode.Require | AfterChild:
                case RegexNode.Prevent | AfterChild:
                    break;

                case RegexNode.One:
                case RegexNode.Notone:
                    PushFC(new RegexFC(node.Ch, NodeType == RegexNode.Notone, false, ci));
                    break;

                case RegexNode.Oneloop:
                case RegexNode.Oneloopatomic:
                case RegexNode.Onelazy:
                    PushFC(new RegexFC(node.Ch, false, node.M == 0, ci));
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notoneloopatomic:
                case RegexNode.Notonelazy:
                    PushFC(new RegexFC(node.Ch, true, node.M == 0, ci));
                    break;

                case RegexNode.Multi:
                    if (node.Str!.Length == 0)
                        PushFC(new RegexFC(true));
                    else if (!rtl)
                        PushFC(new RegexFC(node.Str[0], false, false, ci));
                    else
                        PushFC(new RegexFC(node.Str[node.Str.Length - 1], false, false, ci));
                    break;

                case RegexNode.Set:
                    PushFC(new RegexFC(node.Str!, false, ci));
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setloopatomic:
                case RegexNode.Setlazy:
                    PushFC(new RegexFC(node.Str!, node.M == 0, ci));
                    break;

                case RegexNode.Ref:
                    PushFC(new RegexFC(RegexCharClass.AnyClass, true, false));
                    break;

                case RegexNode.Nothing:
                case RegexNode.Bol:
                case RegexNode.Eol:
                case RegexNode.Boundary:
                case RegexNode.NonBoundary:
                case RegexNode.ECMABoundary:
                case RegexNode.NonECMABoundary:
                case RegexNode.Beginning:
                case RegexNode.Start:
                case RegexNode.EndZ:
                case RegexNode.End:
                case RegexNode.UpdateBumpalong:
                    PushFC(new RegexFC(true));
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, NodeType.ToString(CultureInfo.CurrentCulture)));
            }
        }
    }

    internal sealed class RegexFC
    {
        private readonly RegexCharClass _cc;
        public bool _nullable;

        public RegexFC(bool nullable)
        {
            _cc = new RegexCharClass();
            _nullable = nullable;
        }

        public RegexFC(char ch, bool not, bool nullable, bool caseInsensitive)
        {
            _cc = new RegexCharClass();

            if (not)
            {
                if (ch > 0)
                {
                    _cc.AddRange('\0', (char)(ch - 1));
                }

                if (ch < 0xFFFF)
                {
                    _cc.AddRange((char)(ch + 1), '\uFFFF');
                }
            }
            else
            {
                _cc.AddRange(ch, ch);
            }

            CaseInsensitive = caseInsensitive;
            _nullable = nullable;
        }

        public RegexFC(string charClass, bool nullable, bool caseInsensitive)
        {
            _cc = RegexCharClass.Parse(charClass);

            _nullable = nullable;
            CaseInsensitive = caseInsensitive;
        }

        public bool AddFC(RegexFC fc, bool concatenate)
        {
            if (!_cc.CanMerge || !fc._cc.CanMerge)
            {
                return false;
            }

            if (concatenate)
            {
                if (!_nullable)
                    return true;

                if (!fc._nullable)
                    _nullable = false;
            }
            else
            {
                if (fc._nullable)
                    _nullable = true;
            }

            CaseInsensitive |= fc.CaseInsensitive;
            _cc.AddCharClass(fc._cc);
            return true;
        }

        public bool CaseInsensitive { get; private set; }

        public void AddLowercase(CultureInfo culture)
        {
            Debug.Assert(CaseInsensitive);
            _cc.AddLowercase(culture);
        }

        public string GetFirstChars() => _cc.ToStringClass();
    }
}
