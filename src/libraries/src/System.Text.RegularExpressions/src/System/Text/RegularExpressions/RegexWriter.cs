// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexWriter class is internal to the Regex package.
// It builds a block of regular expression codes (RegexCode)
// from a RegexTree parse tree.

// Implementation notes:
//
// This step is as simple as walking the tree and emitting
// sequences of codes.
//

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexWriter
    {
        private int[] _intStack;
        private int _depth;
        private int[] _emitted;
        private int _curpos;
        private readonly Dictionary<string, int> _stringhash;
        private readonly List<string> _stringtable;
        private bool _counting;
        private int _count;
        private int _trackcount;
        private Hashtable _caps;

        private const int BeforeChild = 64;
        private const int AfterChild = 128;

        /// <summary>
        /// This is the only function that should be called from outside.
        /// It takes a RegexTree and creates a corresponding RegexCode.
        /// </summary>
        internal static RegexCode Write(RegexTree t)
        {
            RegexWriter w = new RegexWriter();
            RegexCode retval = w.RegexCodeFromRegexTree(t);
#if DEBUG
            if (t.Debug)
            {
                t.Dump();
                retval.Dump();
            }
#endif
            return retval;
        }

        // Private constructor; can't be created outside
        private RegexWriter()
        {
            _intStack = new int[32];
            _emitted = new int[32];
            _stringhash = new Dictionary<string, int>();
            _stringtable = new List<string>();
        }

        /// <summary>
        /// To avoid recursion, we use a simple integer stack.
        /// This is the push.
        /// </summary>
        private void PushInt(int i)
        {
            if (_depth >= _intStack.Length)
            {
                int[] expanded = new int[_depth * 2];

                Array.Copy(_intStack, 0, expanded, 0, _depth);

                _intStack = expanded;
            }

            _intStack[_depth++] = i;
        }

        /// <summary>
        /// <c>true</c> if the stack is empty.
        /// </summary>
        private bool EmptyStack()
        {
            return _depth == 0;
        }

        /// <summary>
        /// This is the pop.
        /// </summary>
        private int PopInt()
        {
            return _intStack[--_depth];
        }

        /// <summary>
        /// Returns the current position in the emitted code.
        /// </summary>
        private int CurPos()
        {
            return _curpos;
        }

        /// <summary>
        /// Fixes up a jump instruction at the specified offset
        /// so that it jumps to the specified jumpDest.
        /// </summary>
        private void PatchJump(int offset, int jumpDest)
        {
            _emitted[offset + 1] = jumpDest;
        }

        /// <summary>
        /// Emits a zero-argument operation. Note that the emit
        /// functions all run in two modes: they can emit code, or
        /// they can just count the size of the code.
        /// </summary>
        private void Emit(int op)
        {
            if (_counting)
            {
                _count += 1;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
        }

        /// <summary>
        /// Emits a one-argument operation.
        /// </summary>
        private void Emit(int op, int opd1)
        {
            if (_counting)
            {
                _count += 2;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
            _emitted[_curpos++] = opd1;
        }

        /// <summary>
        /// Emits a two-argument operation.
        /// </summary>
        private void Emit(int op, int opd1, int opd2)
        {
            if (_counting)
            {
                _count += 3;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
            _emitted[_curpos++] = opd1;
            _emitted[_curpos++] = opd2;
        }

        /// <summary>
        /// Returns an index in the string table for a string;
        /// uses a hashtable to eliminate duplicates.
        /// </summary>
        private int StringCode(string str)
        {
            if (_counting)
                return 0;

            if (str == null)
                str = string.Empty;

            int i;
            if (!_stringhash.TryGetValue(str, out i))
            {
                i = _stringtable.Count;
                _stringhash[str] = i;
                _stringtable.Add(str);
            }

            return i;
        }

        /// <summary>
        /// When generating code on a regex that uses a sparse set
        /// of capture slots, we hash them to a dense set of indices
        /// for an array of capture slots. Instead of doing the hash
        /// at match time, it's done at compile time, here.
        /// </summary>
        private int MapCapnum(int capnum)
        {
            if (capnum == -1)
                return -1;

            if (_caps != null)
                return (int)_caps[capnum];
            else
                return capnum;
        }

        /// <summary>
        /// The top level RegexCode generator. It does a depth-first walk
        /// through the tree and calls EmitFragment to emits code before
        /// and after each child of an interior node, and at each leaf.
        ///
        /// It runs two passes, first to count the size of the generated
        /// code, and second to generate the code.
        ///
        /// We should time it against the alternative, which is
        /// to just generate the code and grow the array as we go.
        /// </summary>
        private RegexCode RegexCodeFromRegexTree(RegexTree tree)
        {
            RegexNode curNode;
            int curChild;
            int capsize;
            RegexPrefix fcPrefix;
            RegexPrefix prefix;
            int anchors;
            RegexBoyerMoore bmPrefix;
            bool rtl;

            // construct sparse capnum mapping if some numbers are unused

            if (tree._capnumlist == null || tree._captop == tree._capnumlist.Length)
            {
                capsize = tree._captop;
                _caps = null;
            }
            else
            {
                capsize = tree._capnumlist.Length;
                _caps = tree._caps;
                for (int i = 0; i < tree._capnumlist.Length; i++)
                    _caps[tree._capnumlist[i]] = i;
            }

            _counting = true;

            for (; ;)
            {
                if (!_counting)
                    _emitted = new int[_count];

                curNode = tree._root;
                curChild = 0;

                Emit(RegexCode.Lazybranch, 0);

                for (; ;)
                {
                    if (curNode._children == null)
                    {
                        EmitFragment(curNode._type, curNode, 0);
                    }
                    else if (curChild < curNode._children.Count)
                    {
                        EmitFragment(curNode._type | BeforeChild, curNode, curChild);

                        curNode = curNode._children[curChild];
                        PushInt(curChild);
                        curChild = 0;
                        continue;
                    }

                    if (EmptyStack())
                        break;

                    curChild = PopInt();
                    curNode = curNode._next;

                    EmitFragment(curNode._type | AfterChild, curNode, curChild);
                    curChild++;
                }

                PatchJump(0, CurPos());
                Emit(RegexCode.Stop);

                if (!_counting)
                    break;

                _counting = false;
            }

            fcPrefix = RegexFCD.FirstChars(tree);

            prefix = RegexFCD.Prefix(tree);
            rtl = ((tree._options & RegexOptions.RightToLeft) != 0);

            CultureInfo culture = (tree._options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            if (prefix != null && prefix.Prefix.Length > 0)
                bmPrefix = new RegexBoyerMoore(prefix.Prefix, prefix.CaseInsensitive, rtl, culture);
            else
                bmPrefix = null;

            anchors = RegexFCD.Anchors(tree);

            return new RegexCode(_emitted, _stringtable, _trackcount, _caps, capsize, bmPrefix, fcPrefix, anchors, rtl);
        }

        /// <summary>
        /// The main RegexCode generator. It does a depth-first walk
        /// through the tree and calls EmitFragment to emits code before
        /// and after each child of an interior node, and at each leaf.
        /// </summary>
        private void EmitFragment(int nodetype, RegexNode node, int curIndex)
        {
            int bits = 0;

            if (nodetype <= RegexNode.Ref)
            {
                if (node.UseOptionR())
                    bits |= RegexCode.Rtl;
                if ((node._options & RegexOptions.IgnoreCase) != 0)
                    bits |= RegexCode.Ci;
            }

            switch (nodetype)
            {
                case RegexNode.Concatenate | BeforeChild:
                case RegexNode.Concatenate | AfterChild:
                case RegexNode.Empty:
                    break;

                case RegexNode.Alternate | BeforeChild:
                    if (curIndex < node._children.Count - 1)
                    {
                        PushInt(CurPos());
                        Emit(RegexCode.Lazybranch, 0);
                    }
                    break;

                case RegexNode.Alternate | AfterChild:
                    {
                        if (curIndex < node._children.Count - 1)
                        {
                            int LBPos = PopInt();
                            PushInt(CurPos());
                            Emit(RegexCode.Goto, 0);
                            PatchJump(LBPos, CurPos());
                        }
                        else
                        {
                            int I;
                            for (I = 0; I < curIndex; I++)
                            {
                                PatchJump(PopInt(), CurPos());
                            }
                        }
                        break;
                    }

                case RegexNode.Testref | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexCode.Setjump);
                            PushInt(CurPos());
                            Emit(RegexCode.Lazybranch, 0);
                            Emit(RegexCode.Testref, MapCapnum(node._m));
                            Emit(RegexCode.Forejump);
                            break;
                    }
                    break;

                case RegexNode.Testref | AfterChild:
                    switch (curIndex)
                    {
                        case 0:
                            {
                                int Branchpos = PopInt();
                                PushInt(CurPos());
                                Emit(RegexCode.Goto, 0);
                                PatchJump(Branchpos, CurPos());
                                Emit(RegexCode.Forejump);
                                if (node._children.Count > 1)
                                    break;
                                // else fallthrough
                                goto case 1;
                            }
                        case 1:
                            PatchJump(PopInt(), CurPos());
                            break;
                    }
                    break;

                case RegexNode.Testgroup | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexCode.Setjump);
                            Emit(RegexCode.Setmark);
                            PushInt(CurPos());
                            Emit(RegexCode.Lazybranch, 0);
                            break;
                    }
                    break;

                case RegexNode.Testgroup | AfterChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexCode.Getmark);
                            Emit(RegexCode.Forejump);
                            break;
                        case 1:
                            int Branchpos = PopInt();
                            PushInt(CurPos());
                            Emit(RegexCode.Goto, 0);
                            PatchJump(Branchpos, CurPos());
                            Emit(RegexCode.Getmark);
                            Emit(RegexCode.Forejump);

                            if (node._children.Count > 2)
                                break;
                            // else fallthrough
                            goto case 2;
                        case 2:
                            PatchJump(PopInt(), CurPos());
                            break;
                    }
                    break;

                case RegexNode.Loop | BeforeChild:
                case RegexNode.Lazyloop | BeforeChild:

                    if (node._n < int.MaxValue || node._m > 1)
                        Emit(node._m == 0 ? RegexCode.Nullcount : RegexCode.Setcount, node._m == 0 ? 0 : 1 - node._m);
                    else
                        Emit(node._m == 0 ? RegexCode.Nullmark : RegexCode.Setmark);

                    if (node._m == 0)
                    {
                        PushInt(CurPos());
                        Emit(RegexCode.Goto, 0);
                    }
                    PushInt(CurPos());
                    break;

                case RegexNode.Loop | AfterChild:
                case RegexNode.Lazyloop | AfterChild:
                    {
                        int StartJumpPos = CurPos();
                        int Lazy = (nodetype - (RegexNode.Loop | AfterChild));

                        if (node._n < int.MaxValue || node._m > 1)
                            Emit(RegexCode.Branchcount + Lazy, PopInt(), node._n == int.MaxValue ? int.MaxValue : node._n - node._m);
                        else
                            Emit(RegexCode.Branchmark + Lazy, PopInt());

                        if (node._m == 0)
                            PatchJump(PopInt(), StartJumpPos);
                    }
                    break;

                case RegexNode.Group | BeforeChild:
                case RegexNode.Group | AfterChild:
                    break;

                case RegexNode.Capture | BeforeChild:
                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Capture | AfterChild:
                    Emit(RegexCode.Capturemark, MapCapnum(node._m), MapCapnum(node._n));
                    break;

                case RegexNode.Require | BeforeChild:
                    // NOTE: the following line causes lookahead/lookbehind to be
                    // NON-BACKTRACKING. It can be commented out with (*)
                    Emit(RegexCode.Setjump);


                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Require | AfterChild:
                    Emit(RegexCode.Getmark);

                    // NOTE: the following line causes lookahead/lookbehind to be
                    // NON-BACKTRACKING. It can be commented out with (*)
                    Emit(RegexCode.Forejump);

                    break;

                case RegexNode.Prevent | BeforeChild:
                    Emit(RegexCode.Setjump);
                    PushInt(CurPos());
                    Emit(RegexCode.Lazybranch, 0);
                    break;

                case RegexNode.Prevent | AfterChild:
                    Emit(RegexCode.Backjump);
                    PatchJump(PopInt(), CurPos());
                    Emit(RegexCode.Forejump);
                    break;

                case RegexNode.Greedy | BeforeChild:
                    Emit(RegexCode.Setjump);
                    break;

                case RegexNode.Greedy | AfterChild:
                    Emit(RegexCode.Forejump);
                    break;

                case RegexNode.One:
                case RegexNode.Notone:
                    Emit(node._type | bits, node._ch);
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notonelazy:
                case RegexNode.Oneloop:
                case RegexNode.Onelazy:
                    if (node._m > 0)
                        Emit(((node._type == RegexNode.Oneloop || node._type == RegexNode.Onelazy) ?
                              RegexCode.Onerep : RegexCode.Notonerep) | bits, node._ch, node._m);
                    if (node._n > node._m)
                        Emit(node._type | bits, node._ch, node._n == int.MaxValue ?
                             int.MaxValue : node._n - node._m);
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setlazy:
                    if (node._m > 0)
                        Emit(RegexCode.Setrep | bits, StringCode(node._str), node._m);
                    if (node._n > node._m)
                        Emit(node._type | bits, StringCode(node._str),
                             (node._n == int.MaxValue) ? int.MaxValue : node._n - node._m);
                    break;

                case RegexNode.Multi:
                    Emit(node._type | bits, StringCode(node._str));
                    break;

                case RegexNode.Set:
                    Emit(node._type | bits, StringCode(node._str));
                    break;

                case RegexNode.Ref:
                    Emit(node._type | bits, MapCapnum(node._m));
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
                    Emit(node._type);
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, nodetype.ToString(CultureInfo.CurrentCulture)));
            }
        }
    }
}
