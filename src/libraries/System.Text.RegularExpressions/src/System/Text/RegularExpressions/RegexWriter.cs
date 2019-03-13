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
    internal ref struct RegexWriter
    {
        private const int BeforeChild = 64;
        private const int AfterChild = 128;
        // Distribution of common patterns indicates an average amount of 56 op codes.
        private const int EmittedSize = 56;
        private const int IntStackSize = 32;

        private ValueListBuilder<int> _emitted;
        private ValueListBuilder<int> _intStack;
        private readonly Dictionary<string, int> _stringHash;
        private readonly List<string> _stringTable;
        private Hashtable _caps;
        private int _trackCount;

        private RegexWriter(Span<int> emittedSpan, Span<int> intStackSpan)
        {
            _emitted = new ValueListBuilder<int>(emittedSpan);
            _intStack = new ValueListBuilder<int>(intStackSpan);
            _stringHash = new Dictionary<string, int>();
            _stringTable = new List<string>();
            _caps = null;
            _trackCount = 0;
        }

        /// <summary>
        /// This is the only function that should be called from outside.
        /// It takes a RegexTree and creates a corresponding RegexCode.
        /// </summary>
        public static RegexCode Write(RegexTree tree)
        {
            Span<int> emittedSpan = stackalloc int[EmittedSize];
            Span<int> intStackSpan = stackalloc int[IntStackSize];

            var writer = new RegexWriter(emittedSpan, intStackSpan);
            RegexCode code = writer.RegexCodeFromRegexTree(tree);
            writer.Dispose();

#if DEBUG
            if (tree.Debug)
            {
                tree.Dump();
                code.Dump();
            }
#endif

            return code;
        }

        /// <summary>
        /// Return rented buffers.
        /// </summary>
        public void Dispose()
        {
            _emitted.Dispose();
            _intStack.Dispose();
        }

        /// <summary>
        /// The top level RegexCode generator. It does a depth-first walk
        /// through the tree and calls EmitFragment to emits code before
        /// and after each child of an interior node, and at each leaf.
        /// </summary>
        public RegexCode RegexCodeFromRegexTree(RegexTree tree)
        {
            // construct sparse capnum mapping if some numbers are unused
            int capsize;
            if (tree.CapNumList == null || tree.CapTop == tree.CapNumList.Length)
            {
                capsize = tree.CapTop;
                _caps = null;
            }
            else
            {
                capsize = tree.CapNumList.Length;
                _caps = tree.Caps;
                for (int i = 0; i < tree.CapNumList.Length; i++)
                    _caps[tree.CapNumList[i]] = i;
            }

            RegexNode curNode = tree.Root;
            int curChild = 0;

            Emit(RegexCode.Lazybranch, 0);

            for (; ; )
            {
                if (curNode.Children == null)
                {
                    EmitFragment(curNode.NType, curNode, 0);
                }
                else if (curChild < curNode.Children.Count)
                {
                    EmitFragment(curNode.NType | BeforeChild, curNode, curChild);

                    curNode = curNode.Children[curChild];
                    _intStack.Append(curChild);
                    curChild = 0;
                    continue;
                }

                if (_intStack.Length == 0)
                    break;

                curChild = _intStack.Pop();
                curNode = curNode.Next;

                EmitFragment(curNode.NType | AfterChild, curNode, curChild);
                curChild++;
            }

            PatchJump(0, _emitted.Length);
            Emit(RegexCode.Stop);

            RegexPrefix? fcPrefix = RegexFCD.FirstChars(tree);
            RegexPrefix prefix = RegexFCD.Prefix(tree);
            bool rtl = ((tree.Options & RegexOptions.RightToLeft) != 0);

            CultureInfo culture = (tree.Options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            RegexBoyerMoore bmPrefix;

            if (prefix.Prefix.Length > 0)
                bmPrefix = new RegexBoyerMoore(prefix.Prefix, prefix.CaseInsensitive, rtl, culture);
            else
                bmPrefix = null;

            int anchors = RegexFCD.Anchors(tree);
            int[] emitted = _emitted.AsSpan().ToArray();

            return new RegexCode(emitted, _stringTable, _trackCount, _caps, capsize, bmPrefix, fcPrefix, anchors, rtl);
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
            if (RegexCode.OpcodeBacktracks(op))
                _trackCount++;

            _emitted.Append(op);
        }

        /// <summary>
        /// Emits a one-argument operation.
        /// </summary>
        private void Emit(int op, int opd1)
        {
            if (RegexCode.OpcodeBacktracks(op))
                _trackCount++;

            _emitted.Append(op);
            _emitted.Append(opd1);
        }

        /// <summary>
        /// Emits a two-argument operation.
        /// </summary>
        private void Emit(int op, int opd1, int opd2)
        {
            if (RegexCode.OpcodeBacktracks(op))
                _trackCount++;

            _emitted.Append(op);
            _emitted.Append(opd1);
            _emitted.Append(opd2);
        }

        /// <summary>
        /// Returns an index in the string table for a string;
        /// uses a hashtable to eliminate duplicates.
        /// </summary>
        private int StringCode(string str)
        {
            if (str == null)
                str = string.Empty;

            int i;
            if (!_stringHash.TryGetValue(str, out i))
            {
                i = _stringTable.Count;
                _stringHash[str] = i;
                _stringTable.Add(str);
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
                if ((node.Options & RegexOptions.IgnoreCase) != 0)
                    bits |= RegexCode.Ci;
            }

            switch (nodetype)
            {
                case RegexNode.Concatenate | BeforeChild:
                case RegexNode.Concatenate | AfterChild:
                case RegexNode.Empty:
                    break;

                case RegexNode.Alternate | BeforeChild:
                    if (curIndex < node.Children.Count - 1)
                    {
                        _intStack.Append(_emitted.Length);
                        Emit(RegexCode.Lazybranch, 0);
                    }
                    break;

                case RegexNode.Alternate | AfterChild:
                    {
                        if (curIndex < node.Children.Count - 1)
                        {
                            int LBPos = _intStack.Pop();
                            _intStack.Append(_emitted.Length);
                            Emit(RegexCode.Goto, 0);
                            PatchJump(LBPos, _emitted.Length);
                        }
                        else
                        {
                            int I;
                            for (I = 0; I < curIndex; I++)
                            {
                                PatchJump(_intStack.Pop(), _emitted.Length);
                            }
                        }
                        break;
                    }

                case RegexNode.Testref | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexCode.Setjump);
                            _intStack.Append(_emitted.Length);
                            Emit(RegexCode.Lazybranch, 0);
                            Emit(RegexCode.Testref, MapCapnum(node.M));
                            Emit(RegexCode.Forejump);
                            break;
                    }
                    break;

                case RegexNode.Testref | AfterChild:
                    switch (curIndex)
                    {
                        case 0:
                            {
                                int Branchpos = _intStack.Pop();
                                _intStack.Append(_emitted.Length);
                                Emit(RegexCode.Goto, 0);
                                PatchJump(Branchpos, _emitted.Length);
                                Emit(RegexCode.Forejump);
                                if (node.Children.Count > 1)
                                    break;
                                // else fallthrough
                                goto case 1;
                            }
                        case 1:
                            PatchJump(_intStack.Pop(), _emitted.Length);
                            break;
                    }
                    break;

                case RegexNode.Testgroup | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexCode.Setjump);
                            Emit(RegexCode.Setmark);
                            _intStack.Append(_emitted.Length);
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
                            int Branchpos = _intStack.Pop();
                            _intStack.Append(_emitted.Length);
                            Emit(RegexCode.Goto, 0);
                            PatchJump(Branchpos, _emitted.Length);
                            Emit(RegexCode.Getmark);
                            Emit(RegexCode.Forejump);

                            if (node.Children.Count > 2)
                                break;
                            // else fallthrough
                            goto case 2;
                        case 2:
                            PatchJump(_intStack.Pop(), _emitted.Length);
                            break;
                    }
                    break;

                case RegexNode.Loop | BeforeChild:
                case RegexNode.Lazyloop | BeforeChild:

                    if (node.N < int.MaxValue || node.M > 1)
                        Emit(node.M == 0 ? RegexCode.Nullcount : RegexCode.Setcount, node.M == 0 ? 0 : 1 - node.M);
                    else
                        Emit(node.M == 0 ? RegexCode.Nullmark : RegexCode.Setmark);

                    if (node.M == 0)
                    {
                        _intStack.Append(_emitted.Length);
                        Emit(RegexCode.Goto, 0);
                    }
                    _intStack.Append(_emitted.Length);
                    break;

                case RegexNode.Loop | AfterChild:
                case RegexNode.Lazyloop | AfterChild:
                    {
                        int StartJumpPos = _emitted.Length;
                        int Lazy = (nodetype - (RegexNode.Loop | AfterChild));

                        if (node.N < int.MaxValue || node.M > 1)
                            Emit(RegexCode.Branchcount + Lazy, _intStack.Pop(), node.N == int.MaxValue ? int.MaxValue : node.N - node.M);
                        else
                            Emit(RegexCode.Branchmark + Lazy, _intStack.Pop());

                        if (node.M == 0)
                            PatchJump(_intStack.Pop(), StartJumpPos);
                    }
                    break;

                case RegexNode.Group | BeforeChild:
                case RegexNode.Group | AfterChild:
                    break;

                case RegexNode.Capture | BeforeChild:
                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Capture | AfterChild:
                    Emit(RegexCode.Capturemark, MapCapnum(node.M), MapCapnum(node.N));
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
                    _intStack.Append(_emitted.Length);
                    Emit(RegexCode.Lazybranch, 0);
                    break;

                case RegexNode.Prevent | AfterChild:
                    Emit(RegexCode.Backjump);
                    PatchJump(_intStack.Pop(), _emitted.Length);
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
                    Emit(node.NType | bits, node.Ch);
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notonelazy:
                case RegexNode.Oneloop:
                case RegexNode.Onelazy:
                    if (node.M > 0)
                        Emit(((node.NType == RegexNode.Oneloop || node.NType == RegexNode.Onelazy) ?
                              RegexCode.Onerep : RegexCode.Notonerep) | bits, node.Ch, node.M);
                    if (node.N > node.M)
                        Emit(node.NType | bits, node.Ch, node.N == int.MaxValue ?
                             int.MaxValue : node.N - node.M);
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setlazy:
                    if (node.M > 0)
                        Emit(RegexCode.Setrep | bits, StringCode(node.Str), node.M);
                    if (node.N > node.M)
                        Emit(node.NType | bits, StringCode(node.Str),
                             (node.N == int.MaxValue) ? int.MaxValue : node.N - node.M);
                    break;

                case RegexNode.Multi:
                    Emit(node.NType | bits, StringCode(node.Str));
                    break;

                case RegexNode.Set:
                    Emit(node.NType | bits, StringCode(node.Str));
                    break;

                case RegexNode.Ref:
                    Emit(node.NType | bits, MapCapnum(node.M));
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
                    Emit(node.NType);
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, nodetype.ToString()));
            }
        }
    }
}
