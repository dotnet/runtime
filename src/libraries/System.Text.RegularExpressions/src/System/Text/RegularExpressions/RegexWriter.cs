// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    /// <summary>Builds a block of regular expression codes (RegexCode) from a RegexTree parse tree.</summary>
    internal ref struct RegexWriter
    {
        // These must be unused RegexNode type bits.
        private const int BeforeChild = 64;
        private const int AfterChild = 128;

        // Distribution of common patterns indicates an average amount of 56 op codes. Since we're stackalloc'ing,
        // we can afford to make it a bit higher and a power of two for simplicity.
        private const int EmittedSize = 64;
        private const int IntStackSize = 32;

        private readonly Dictionary<string, int> _stringTable;
        private ValueListBuilder<int> _emitted;
        private ValueListBuilder<int> _intStack;
        private Hashtable? _caps;
        private int _trackCount;

        private RegexWriter(Span<int> emittedSpan, Span<int> intStackSpan)
        {
            _emitted = new ValueListBuilder<int>(emittedSpan);
            _intStack = new ValueListBuilder<int>(intStackSpan);
            _stringTable = new Dictionary<string, int>();
            _caps = null;
            _trackCount = 0;
        }

        /// <summary>
        /// This is the only function that should be called from outside.
        /// It takes a RegexTree and creates a corresponding RegexCode.
        /// </summary>
        public static RegexCode Write(RegexTree tree, CultureInfo culture)
        {
            var writer = new RegexWriter(stackalloc int[EmittedSize], stackalloc int[IntStackSize]);
            RegexCode code = writer.RegexCodeFromRegexTree(tree, culture);
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
        /// through the tree and calls EmitFragment to emit code before
        /// and after each child of an interior node and at each leaf.
        /// It also computes various information about the tree, such as
        /// prefix data to help with optimizations.
        /// </summary>
        public RegexCode RegexCodeFromRegexTree(RegexTree tree, CultureInfo culture)
        {
            // Construct sparse capnum mapping if some numbers are unused.
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
                {
                    _caps[tree.CapNumList[i]] = i;
                }
            }

            // Every written code begins with a lazy branch.  This will be back-patched
            // to point to the ending Stop after the whole expression has been written.
            Emit(RegexCode.Lazybranch, 0);

            // Emit every node.
            RegexNode curNode = tree.Root;
            int curChild = 0;
            while (true)
            {
                int curNodeChildCount = curNode.ChildCount();
                if (curNodeChildCount == 0)
                {
                    EmitFragment(curNode.Type, curNode, 0);
                }
                else if (curChild < curNodeChildCount)
                {
                    EmitFragment(curNode.Type | BeforeChild, curNode, curChild);

                    curNode = curNode.Child(curChild);
                    _intStack.Append(curChild);
                    curChild = 0;
                    continue;
                }

                if (_intStack.Length == 0)
                {
                    break;
                }

                curChild = _intStack.Pop();
                curNode = curNode.Next!;

                EmitFragment(curNode.Type | AfterChild, curNode, curChild);
                curChild++;
            }

            // Patch the starting Lazybranch, emit the final Stop, and get the resulting code array.
            PatchJump(0, _emitted.Length);
            Emit(RegexCode.Stop);
            int[] emitted = _emitted.AsSpan().ToArray();

            // Convert the string table into an ordered string array.
            var strings = new string[_stringTable.Count];
            foreach (KeyValuePair<string, int> stringEntry in _stringTable)
            {
                strings[stringEntry.Value] = stringEntry.Key;
            }

            // Return all that in a RegexCode object.
            return new RegexCode(tree, culture, emitted, strings, _trackCount, _caps, capsize);
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
            {
                _trackCount++;
            }

            _emitted.Append(op);
        }

        /// <summary>Emits a one-argument operation.</summary>
        private void Emit(int op, int opd1)
        {
            if (RegexCode.OpcodeBacktracks(op))
            {
                _trackCount++;
            }

            _emitted.Append(op);
            _emitted.Append(opd1);
        }

        /// <summary>Emits a two-argument operation.</summary>
        private void Emit(int op, int opd1, int opd2)
        {
            if (RegexCode.OpcodeBacktracks(op))
            {
                _trackCount++;
            }

            _emitted.Append(op);
            _emitted.Append(opd1);
            _emitted.Append(opd2);
        }

        /// <summary>
        /// Returns an index in the string table for a string;
        /// uses a dictionary to eliminate duplicates.
        /// </summary>
        private int StringCode(string str)
        {
#if REGEXGENERATOR
            if (!_stringTable.TryGetValue(str, out int i))
            {
                i = _stringTable.Count;
                _stringTable.Add(str, i);
            }
#else
            ref int i = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringTable, str, out bool exists);
            if (!exists)
            {
                i = _stringTable.Count - 1;
            }
#endif
            return i;
        }

        /// <summary>
        /// The main RegexCode generator. It does a depth-first walk
        /// through the tree and calls EmitFragment to emits code before
        /// and after each child of an interior node, and at each leaf.
        /// </summary>
        private void EmitFragment(int nodetype, RegexNode node, int curIndex)
        {
            int bits = 0;
            if ((node.Options & RegexOptions.RightToLeft) != 0)
            {
                bits |= RegexCode.Rtl;
            }
            if ((node.Options & RegexOptions.IgnoreCase) != 0)
            {
                bits |= RegexCode.Ci;
            }

            switch (nodetype)
            {
                case RegexNode.Concatenate | BeforeChild:
                case RegexNode.Concatenate | AfterChild:
                case RegexNode.Empty:
                    break;

                case RegexNode.Alternate | BeforeChild:
                    if (curIndex < node.ChildCount() - 1)
                    {
                        _intStack.Append(_emitted.Length);
                        Emit(RegexCode.Lazybranch, 0);
                    }
                    break;

                case RegexNode.Alternate | AfterChild:
                    {
                        if (curIndex < node.ChildCount() - 1)
                        {
                            int lazyBranchPos = _intStack.Pop();
                            _intStack.Append(_emitted.Length);
                            Emit(RegexCode.Goto, 0);
                            PatchJump(lazyBranchPos, _emitted.Length);
                        }
                        else
                        {
                            for (int i = 0; i < curIndex; i++)
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
                            Emit(RegexCode.Testref, RegexParser.MapCaptureNumber(node.M, _caps));
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
                                break;
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
                            break;
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
                    Emit(RegexCode.Capturemark, RegexParser.MapCaptureNumber(node.M, _caps), RegexParser.MapCaptureNumber(node.N, _caps));
                    break;

                case RegexNode.Require | BeforeChild:
                    Emit(RegexCode.Setjump); // causes lookahead/lookbehind to be non-backtracking
                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Require | AfterChild:
                    Emit(RegexCode.Getmark);
                    Emit(RegexCode.Forejump); // causes lookahead/lookbehind to be non-backtracking
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

                case RegexNode.Atomic | BeforeChild:
                    Emit(RegexCode.Setjump);
                    break;

                case RegexNode.Atomic | AfterChild:
                    Emit(RegexCode.Forejump);
                    break;

                case RegexNode.One:
                case RegexNode.Notone:
                    Emit(node.Type | bits, node.Ch);
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notoneloopatomic:
                case RegexNode.Notonelazy:
                case RegexNode.Oneloop:
                case RegexNode.Oneloopatomic:
                case RegexNode.Onelazy:
                    if (node.M > 0)
                    {
                        Emit(((node.Type == RegexNode.Oneloop || node.Type == RegexNode.Oneloopatomic || node.Type == RegexNode.Onelazy) ?
                              RegexCode.Onerep : RegexCode.Notonerep) | bits, node.Ch, node.M);
                    }
                    if (node.N > node.M)
                    {
                        Emit(node.Type | bits, node.Ch, node.N == int.MaxValue ? int.MaxValue : node.N - node.M);
                    }
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setloopatomic:
                case RegexNode.Setlazy:
                    {
                        int stringCode = StringCode(node.Str!);
                        if (node.M > 0)
                        {
                            Emit(RegexCode.Setrep | bits, stringCode, node.M);
                        }
                        if (node.N > node.M)
                        {
                            Emit(node.Type | bits, stringCode, (node.N == int.MaxValue) ? int.MaxValue : node.N - node.M);
                        }
                    }
                    break;

                case RegexNode.Multi:
                    Emit(node.Type | bits, StringCode(node.Str!));
                    break;

                case RegexNode.Set:
                    Emit(node.Type | bits, StringCode(node.Str!));
                    break;

                case RegexNode.Ref:
                    Emit(node.Type | bits, RegexParser.MapCaptureNumber(node.M, _caps));
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
                    Emit(node.Type);
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.UnexpectedOpcode, nodetype.ToString()));
            }
        }
    }
}
