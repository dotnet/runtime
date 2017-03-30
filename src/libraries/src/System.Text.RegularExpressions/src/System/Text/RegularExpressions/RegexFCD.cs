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

using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexFCD
    {
        private int[] _intStack;
        private int _intDepth;
        private RegexFC[] _fcStack;
        private int _fcDepth;
        private bool _skipAllChildren;      // don't process any more children at the current level
        private bool _skipchild;            // don't process the current child.
        private bool _failed = false;

        private const int BeforeChild = 64;
        private const int AfterChild = 128;

        // where the regex can be pegged

        internal const int Beginning = 0x0001;
        internal const int Bol = 0x0002;
        internal const int Start = 0x0004;
        internal const int Eol = 0x0008;
        internal const int EndZ = 0x0010;
        internal const int End = 0x0020;
        internal const int Boundary = 0x0040;
        internal const int ECMABoundary = 0x0080;

        /*
         * This is the one of the only two functions that should be called from outside.
         * It takes a RegexTree and computes the set of chars that can start it.
         */
        internal static RegexPrefix FirstChars(RegexTree t)
        {
            RegexFCD s = new RegexFCD();
            RegexFC fc = s.RegexFCFromRegexTree(t);

            if (fc == null || fc._nullable)
                return null;

            CultureInfo culture = ((t._options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            return new RegexPrefix(fc.GetFirstChars(culture), fc.IsCaseInsensitive());
        }

        /*
         * This is a related computation: it takes a RegexTree and computes the
         * leading substring if it see one. It's quite trivial and gives up easily.
         */
        internal static RegexPrefix Prefix(RegexTree tree)
        {
            RegexNode curNode;
            RegexNode concatNode = null;
            int nextChild = 0;

            curNode = tree._root;

            for (; ;)
            {
                switch (curNode._type)
                {
                    case RegexNode.Concatenate:
                        if (curNode.ChildCount() > 0)
                        {
                            concatNode = curNode;
                            nextChild = 0;
                        }
                        break;

                    case RegexNode.Greedy:
                    case RegexNode.Capture:
                        curNode = curNode.Child(0);
                        concatNode = null;
                        continue;

                    case RegexNode.Oneloop:
                    case RegexNode.Onelazy:
                        if (curNode._m > 0)
                        {
                            string pref = string.Empty.PadRight(curNode._m, curNode._ch);
                            return new RegexPrefix(pref, 0 != (curNode._options & RegexOptions.IgnoreCase));
                        }
                        else
                            return RegexPrefix.Empty;

                    case RegexNode.One:
                        return new RegexPrefix(curNode._ch.ToString(), 0 != (curNode._options & RegexOptions.IgnoreCase));

                    case RegexNode.Multi:
                        return new RegexPrefix(curNode._str, 0 != (curNode._options & RegexOptions.IgnoreCase));

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

        /*
         * Yet another related computation: it takes a RegexTree and computes the
         * leading anchors that it encounters.
         */
        internal static int Anchors(RegexTree tree)
        {
            RegexNode curNode;
            RegexNode concatNode = null;
            int nextChild = 0;
            int result = 0;

            curNode = tree._root;

            for (; ;)
            {
                switch (curNode._type)
                {
                    case RegexNode.Concatenate:
                        if (curNode.ChildCount() > 0)
                        {
                            concatNode = curNode;
                            nextChild = 0;
                        }
                        break;

                    case RegexNode.Greedy:
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
                        return result | AnchorFromType(curNode._type);

                    case RegexNode.Empty:
                    case RegexNode.Require:
                    case RegexNode.Prevent:
                        break;

                    default:
                        return result;
                }

                if (concatNode == null || nextChild >= concatNode.ChildCount())
                    return result;

                curNode = concatNode.Child(nextChild++);
            }
        }

        /*
         * Convert anchor type to anchor bit.
         */
        private static int AnchorFromType(int type)
        {
            switch (type)
            {
                case RegexNode.Bol: return Bol;
                case RegexNode.Eol: return Eol;
                case RegexNode.Boundary: return Boundary;
                case RegexNode.ECMABoundary: return ECMABoundary;
                case RegexNode.Beginning: return Beginning;
                case RegexNode.Start: return Start;
                case RegexNode.EndZ: return EndZ;
                case RegexNode.End: return End;
                default: return 0;
            }
        }

#if DEBUG
        internal static string AnchorDescription(int anchors)
        {
            StringBuilder sb = new StringBuilder();

            if (0 != (anchors & Beginning)) sb.Append(", Beginning");
            if (0 != (anchors & Start)) sb.Append(", Start");
            if (0 != (anchors & Bol)) sb.Append(", Bol");
            if (0 != (anchors & Boundary)) sb.Append(", Boundary");
            if (0 != (anchors & ECMABoundary)) sb.Append(", ECMABoundary");
            if (0 != (anchors & Eol)) sb.Append(", Eol");
            if (0 != (anchors & End)) sb.Append(", End");
            if (0 != (anchors & EndZ)) sb.Append(", EndZ");

            if (sb.Length >= 2)
                return (sb.ToString(2, sb.Length - 2));

            return "None";
        }
#endif

        /*
         * private constructor; can't be created outside
         */
        private RegexFCD()
        {
            _fcStack = new RegexFC[32];
            _intStack = new int[32];
        }

        /*
         * To avoid recursion, we use a simple integer stack.
         * This is the push.
         */
        private void PushInt(int I)
        {
            if (_intDepth >= _intStack.Length)
            {
                int[] expanded = new int[_intDepth * 2];

                Array.Copy(_intStack, 0, expanded, 0, _intDepth);

                _intStack = expanded;
            }

            _intStack[_intDepth++] = I;
        }

        /*
         * True if the stack is empty.
         */
        private bool IntIsEmpty()
        {
            return _intDepth == 0;
        }

        /*
         * This is the pop.
         */
        private int PopInt()
        {
            return _intStack[--_intDepth];
        }

        /*
          * We also use a stack of RegexFC objects.
          * This is the push.
          */
        private void PushFC(RegexFC fc)
        {
            if (_fcDepth >= _fcStack.Length)
            {
                RegexFC[] expanded = new RegexFC[_fcDepth * 2];

                Array.Copy(_fcStack, 0, expanded, 0, _fcDepth);
                _fcStack = expanded;
            }

            _fcStack[_fcDepth++] = fc;
        }

        /*
         * True if the stack is empty.
         */
        private bool FCIsEmpty()
        {
            return _fcDepth == 0;
        }

        /*
         * This is the pop.
         */
        private RegexFC PopFC()
        {
            return _fcStack[--_fcDepth];
        }

        /*
         * This is the top.
         */
        private RegexFC TopFC()
        {
            return _fcStack[_fcDepth - 1];
        }

        /*
         * The main FC computation. It does a shortcutted depth-first walk
         * through the tree and calls CalculateFC to emits code before
         * and after each child of an interior node, and at each leaf.
         */
        private RegexFC RegexFCFromRegexTree(RegexTree tree)
        {
            RegexNode curNode;
            int curChild;

            curNode = tree._root;
            curChild = 0;

            for (; ;)
            {
                if (curNode._children == null)
                {
                    // This is a leaf node
                    CalculateFC(curNode._type, curNode, 0);
                }
                else if (curChild < curNode._children.Count && !_skipAllChildren)
                {
                    // This is an interior node, and we have more children to analyze
                    CalculateFC(curNode._type | BeforeChild, curNode, curChild);

                    if (!_skipchild)
                    {
                        curNode = curNode._children[curChild];
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
                curNode = curNode._next;

                CalculateFC(curNode._type | AfterChild, curNode, curChild);
                if (_failed)
                    return null;

                curChild++;
            }

            if (FCIsEmpty())
                return null;

            return PopFC();
        }

        /*
         * Called in Beforechild to prevent further processing of the current child
         */
        private void SkipChild()
        {
            _skipchild = true;
        }

        /*
         * FC computation and shortcut cases for each node type
         */
        private void CalculateFC(int NodeType, RegexNode node, int CurIndex)
        {
            bool ci = false;
            bool rtl = false;

            if (NodeType <= RegexNode.Ref)
            {
                if ((node._options & RegexOptions.IgnoreCase) != 0)
                    ci = true;
                if ((node._options & RegexOptions.RightToLeft) != 0)
                    rtl = true;
            }

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
                    if (node._m == 0)
                        TopFC()._nullable = true;
                    break;

                case RegexNode.Group | BeforeChild:
                case RegexNode.Group | AfterChild:
                case RegexNode.Capture | BeforeChild:
                case RegexNode.Capture | AfterChild:
                case RegexNode.Greedy | BeforeChild:
                case RegexNode.Greedy | AfterChild:
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
                    PushFC(new RegexFC(node._ch, NodeType == RegexNode.Notone, false, ci));
                    break;

                case RegexNode.Oneloop:
                case RegexNode.Onelazy:
                    PushFC(new RegexFC(node._ch, false, node._m == 0, ci));
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notonelazy:
                    PushFC(new RegexFC(node._ch, true, node._m == 0, ci));
                    break;

                case RegexNode.Multi:
                    if (node._str.Length == 0)
                        PushFC(new RegexFC(true));
                    else if (!rtl)
                        PushFC(new RegexFC(node._str[0], false, false, ci));
                    else
                        PushFC(new RegexFC(node._str[node._str.Length - 1], false, false, ci));
                    break;

                case RegexNode.Set:
                    PushFC(new RegexFC(node._str, false, ci));
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setlazy:
                    PushFC(new RegexFC(node._str, node._m == 0, ci));
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
        internal RegexCharClass _cc;
        internal bool _nullable;
        internal bool _caseInsensitive;

        internal RegexFC(bool nullable)
        {
            _cc = new RegexCharClass();
            _nullable = nullable;
        }

        internal RegexFC(char ch, bool not, bool nullable, bool caseInsensitive)
        {
            _cc = new RegexCharClass();

            if (not)
            {
                if (ch > 0)
                    _cc.AddRange('\0', (char)(ch - 1));
                if (ch < 0xFFFF)
                    _cc.AddRange((char)(ch + 1), '\uFFFF');
            }
            else
            {
                _cc.AddRange(ch, ch);
            }

            _caseInsensitive = caseInsensitive;
            _nullable = nullable;
        }

        internal RegexFC(string charClass, bool nullable, bool caseInsensitive)
        {
            _cc = RegexCharClass.Parse(charClass);

            _nullable = nullable;
            _caseInsensitive = caseInsensitive;
        }

        internal bool AddFC(RegexFC fc, bool concatenate)
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

            _caseInsensitive |= fc._caseInsensitive;
            _cc.AddCharClass(fc._cc);
            return true;
        }

        internal String GetFirstChars(CultureInfo culture)
        {
            if (_caseInsensitive)
                _cc.AddLowercase(culture);

            return _cc.ToStringClass();
        }

        internal bool IsCaseInsensitive()
        {
            return _caseInsensitive;
        }
    }

    internal sealed class RegexPrefix
    {
        internal string _prefix;
        internal bool _caseInsensitive;

        internal static RegexPrefix _empty = new RegexPrefix(string.Empty, false);

        internal RegexPrefix(string prefix, bool ci)
        {
            _prefix = prefix;
            _caseInsensitive = ci;
        }

        internal string Prefix
        {
            get
            {
                return _prefix;
            }
        }

        internal bool CaseInsensitive
        {
            get
            {
                return _caseInsensitive;
            }
        }
        internal static RegexPrefix Empty
        {
            get
            {
                return _empty;
            }
        }
    }
}
