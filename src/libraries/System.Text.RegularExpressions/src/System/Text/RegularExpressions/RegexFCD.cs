// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexFCD class is internal to the Regex package.
// It builds a bunch of FC information (RegexFC) about
// the regex for optimization purposes.

// Implementation notes:
//
// This step is as simple as walking the tree and emitting
// sequences of codes.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal ref struct RegexFCD
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

        private RegexFCD(Span<int> intStack)
        {
            _fcStack = new List<RegexFC>(StackBufferSize);
            _intStack = new ValueListBuilder<int>(intStack);
            _failed = false;
            _skipchild = false;
            _skipAllChildren = false;
        }

        /// <summary>
        /// This is the one of the only two functions that should be called from outside.
        /// It takes a RegexTree and computes the set of chars that can start it.
        /// </summary>
        public static RegexPrefix? FirstChars(RegexTree t)
        {
            var s = new RegexFCD(stackalloc int[StackBufferSize]);
            RegexFC? fc = s.RegexFCFromRegexTree(t);
            s.Dispose();

            if (fc == null || fc._nullable)
            {
                return null;
            }

            if (fc.CaseInsensitive)
            {
                fc.AddLowercase(((t.Options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            return new RegexPrefix(fc.GetFirstChars(), fc.CaseInsensitive);
        }

        /// <summary>
        /// This is a related computation: it takes a RegexTree and computes the
        /// leading substring if it see one. It's quite trivial and gives up easily.
        /// </summary>
        public static RegexPrefix Prefix(RegexTree tree)
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

                        // In release, cutoff at a length to which we can still reasonably construct a string
                        // In debug, use a smaller cutoff to exercise the cutoff path in tests
                        const int Cutoff =
#if DEBUG
                            50;
#else
                            1_000_000;
#endif

                        if (curNode.M > 0 && curNode.M < Cutoff)
                        {
                            string pref = new string(curNode.Ch, curNode.M);
                            return new RegexPrefix(pref, 0 != (curNode.Options & RegexOptions.IgnoreCase));
                        }

                        return RegexPrefix.Empty;

                    case RegexNode.One:
                        return new RegexPrefix(curNode.Ch.ToString(), 0 != (curNode.Options & RegexOptions.IgnoreCase));

                    case RegexNode.Multi:
                        return new RegexPrefix(curNode.Str!, 0 != (curNode.Options & RegexOptions.IgnoreCase));

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
                        return RegexPrefix.Empty;
                }

                if (concatNode == null || nextChild >= concatNode.ChildCount())
                    return RegexPrefix.Empty;

                curNode = concatNode.Child(nextChild++);
            }
        }

        /// <summary>
        /// Yet another related computation: it takes a RegexTree and computes
        /// the leading anchor that it encounters.
        /// </summary>
        public static int Anchors(RegexTree tree)
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

                    case RegexNode.Bol:
                    case RegexNode.Eol:
                    case RegexNode.Boundary:
                    case RegexNode.ECMABoundary:
                    case RegexNode.Beginning:
                    case RegexNode.Start:
                    case RegexNode.EndZ:
                    case RegexNode.End:
                        return AnchorFromType(curNode.Type);

                    case RegexNode.Empty:
                    case RegexNode.Require:
                    case RegexNode.Prevent:
                        break;

                    default:
                        return 0;
                }

                if (concatNode == null || nextChild >= concatNode.ChildCount())
                    return 0;

                curNode = concatNode.Child(nextChild++);
            }
        }

        /// <summary>
        /// Convert anchor type to anchor bit.
        /// </summary>
        private static int AnchorFromType(int type) =>
            type switch
            {
                RegexNode.Bol => Bol,
                RegexNode.Eol => Eol,
                RegexNode.Boundary => Boundary,
                RegexNode.ECMABoundary => ECMABoundary,
                RegexNode.Beginning => Beginning,
                RegexNode.Start => Start,
                RegexNode.EndZ => EndZ,
                RegexNode.End => End,
                _ => 0,
            };

#if DEBUG
        [ExcludeFromCodeCoverage]
        public static string AnchorDescription(int anchors)
        {
            StringBuilder sb = new StringBuilder();

            if (0 != (anchors & Beginning))
                sb.Append(", Beginning");
            if (0 != (anchors & Start))
                sb.Append(", Start");
            if (0 != (anchors & Bol))
                sb.Append(", Bol");
            if (0 != (anchors & Boundary))
                sb.Append(", Boundary");
            if (0 != (anchors & ECMABoundary))
                sb.Append(", ECMABoundary");
            if (0 != (anchors & Eol))
                sb.Append(", Eol");
            if (0 != (anchors & End))
                sb.Append(", End");
            if (0 != (anchors & EndZ))
                sb.Append(", EndZ");

            if (sb.Length >= 2)
                return (sb.ToString(2, sb.Length - 2));

            return "None";
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
                case RegexNode.Nonboundary:
                case RegexNode.ECMABoundary:
                case RegexNode.NonECMABoundary:
                case RegexNode.Beginning:
                case RegexNode.Start:
                case RegexNode.EndZ:
                case RegexNode.End:
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
