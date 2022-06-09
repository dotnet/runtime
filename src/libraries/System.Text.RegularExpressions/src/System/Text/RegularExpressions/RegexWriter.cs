// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    /// <summary>Builds a block of regular expression codes (RegexCode) from a RegexTree parse tree.</summary>
    internal ref struct RegexWriter
    {
        // These must be unused RegexNode type bits.
        private const RegexNodeKind BeforeChild = (RegexNodeKind)64;
        private const RegexNodeKind AfterChild = (RegexNodeKind)128;

        // Distribution of common patterns indicates an average amount of 56 op codes. Since we're stackalloc'ing,
        // we can afford to make it a bit higher and a power of two for simplicity.
        private const int EmittedSize = 64;
        private const int IntStackSize = 32;

        private readonly RegexTree _tree;
        private readonly Dictionary<string, int> _stringTable;
        private ValueListBuilder<int> _emitted;
        private ValueListBuilder<int> _intStack;
        private int _trackCount;

#if DEBUG
        static RegexWriter()
        {
            Debug.Assert(!Enum.IsDefined(typeof(RegexNodeKind), BeforeChild));
            Debug.Assert(!Enum.IsDefined(typeof(RegexNodeKind), AfterChild));
        }
#endif

        private RegexWriter(RegexTree tree, Span<int> emittedSpan, Span<int> intStackSpan)
        {
            _tree = tree;
            _emitted = new ValueListBuilder<int>(emittedSpan);
            _intStack = new ValueListBuilder<int>(intStackSpan);
            _stringTable = new Dictionary<string, int>();
            _trackCount = 0;
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
        /// This is the only function that should be called from outside.
        /// It takes a <see cref="RegexTree"/> and creates a corresponding <see cref="RegexInterpreterCode"/>.
        /// </summary>
        public static RegexInterpreterCode Write(RegexTree tree)
        {
            using var writer = new RegexWriter(tree, stackalloc int[EmittedSize], stackalloc int[IntStackSize]);
            return writer.EmitCode();
        }

        /// <summary>
        /// The top level RegexInterpreterCode generator. It does a depth-first walk
        /// through the tree and calls EmitFragment to emit code before
        /// and after each child of an interior node and at each leaf.
        /// It also computes various information about the tree, such as
        /// prefix data to help with optimizations.
        /// </summary>
        private RegexInterpreterCode EmitCode()
        {
            // Every written code begins with a lazy branch.  This will be back-patched
            // to point to the ending Stop after the whole expression has been written.
            Emit(RegexOpcode.Lazybranch, 0);

            // Emit every node.
            RegexNode curNode = _tree.Root;
            int curChild = 0;
            while (true)
            {
                int curNodeChildCount = curNode.ChildCount();
                if (curNodeChildCount == 0)
                {
                    EmitFragment(curNode.Kind, curNode, 0);
                }
                else if (curChild < curNodeChildCount)
                {
                    EmitFragment(curNode.Kind | BeforeChild, curNode, curChild);

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
                curNode = curNode.Parent!;

                EmitFragment(curNode.Kind | AfterChild, curNode, curChild);
                curChild++;
            }

            // Patch the starting Lazybranch, emit the final Stop, and get the resulting code array.
            PatchJump(0, _emitted.Length);
            Emit(RegexOpcode.Stop);
            int[] emitted = _emitted.AsSpan().ToArray();

            // Convert the string table into an ordered string array.
            var strings = new string[_stringTable.Count];
            foreach (KeyValuePair<string, int> stringEntry in _stringTable)
            {
                strings[stringEntry.Value] = stringEntry.Key;
            }

            // Return all that in a RegexCode object.
            return new RegexInterpreterCode(_tree.FindOptimizations, _tree.Options, emitted, strings, _trackCount);
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
        private void Emit(RegexOpcode op)
        {
            if (RegexInterpreterCode.OpcodeBacktracks(op))
            {
                _trackCount++;
            }

            _emitted.Append((int)op);
        }

        /// <summary>Emits a one-argument operation.</summary>
        private void Emit(RegexOpcode op, int opd1)
        {
            if (RegexInterpreterCode.OpcodeBacktracks(op))
            {
                _trackCount++;
            }

            _emitted.Append((int)op);
            _emitted.Append(opd1);
        }

        /// <summary>Emits a two-argument operation.</summary>
        private void Emit(RegexOpcode op, int opd1, int opd2)
        {
            if (RegexInterpreterCode.OpcodeBacktracks(op))
            {
                _trackCount++;
            }

            _emitted.Append((int)op);
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
        private void EmitFragment(RegexNodeKind nodeType, RegexNode node, int curIndex)
        {
            RegexOpcode bits = 0;
            if ((node.Options & RegexOptions.RightToLeft) != 0)
            {
                bits |= RegexOpcode.RightToLeft;
            }
            if ((node.Options & RegexOptions.IgnoreCase) != 0)
            {
                bits |= RegexOpcode.CaseInsensitive;
            }

            switch (nodeType)
            {
                case RegexNodeKind.Concatenate | BeforeChild:
                case RegexNodeKind.Concatenate | AfterChild:
                case RegexNodeKind.Empty:
                    break;

                case RegexNodeKind.Alternate | BeforeChild:
                    if (curIndex < node.ChildCount() - 1)
                    {
                        _intStack.Append(_emitted.Length);
                        Emit(RegexOpcode.Lazybranch, 0);
                    }
                    break;

                case RegexNodeKind.Alternate | AfterChild:
                    {
                        if (curIndex < node.ChildCount() - 1)
                        {
                            int lazyBranchPos = _intStack.Pop();
                            _intStack.Append(_emitted.Length);
                            Emit(RegexOpcode.Goto, 0);
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

                case RegexNodeKind.BackreferenceConditional | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexOpcode.Setjump);
                            _intStack.Append(_emitted.Length);
                            Emit(RegexOpcode.Lazybranch, 0);
                            Emit(RegexOpcode.TestBackreference, RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping));
                            Emit(RegexOpcode.Forejump);
                            break;
                    }
                    break;

                case RegexNodeKind.BackreferenceConditional | AfterChild:
                    switch (curIndex)
                    {
                        case 0:
                            {
                                int Branchpos = _intStack.Pop();
                                _intStack.Append(_emitted.Length);
                                Emit(RegexOpcode.Goto, 0);
                                PatchJump(Branchpos, _emitted.Length);
                                Emit(RegexOpcode.Forejump);
                                break;
                            }
                        case 1:
                            PatchJump(_intStack.Pop(), _emitted.Length);
                            break;
                    }
                    break;

                case RegexNodeKind.ExpressionConditional | BeforeChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexOpcode.Setjump);
                            Emit(RegexOpcode.Setmark);
                            _intStack.Append(_emitted.Length);
                            Emit(RegexOpcode.Lazybranch, 0);
                            break;
                    }
                    break;

                case RegexNodeKind.ExpressionConditional | AfterChild:
                    switch (curIndex)
                    {
                        case 0:
                            Emit(RegexOpcode.Getmark);
                            Emit(RegexOpcode.Forejump);
                            break;
                        case 1:
                            int Branchpos = _intStack.Pop();
                            _intStack.Append(_emitted.Length);
                            Emit(RegexOpcode.Goto, 0);
                            PatchJump(Branchpos, _emitted.Length);
                            Emit(RegexOpcode.Getmark);
                            Emit(RegexOpcode.Forejump);
                            break;
                        case 2:
                            PatchJump(_intStack.Pop(), _emitted.Length);
                            break;
                    }
                    break;

                case RegexNodeKind.Loop | BeforeChild:
                case RegexNodeKind.Lazyloop | BeforeChild:

                    if (node.N < int.MaxValue || node.M > 1)
                        Emit(node.M == 0 ? RegexOpcode.Nullcount : RegexOpcode.Setcount, node.M == 0 ? 0 : 1 - node.M);
                    else
                        Emit(node.M == 0 ? RegexOpcode.Nullmark : RegexOpcode.Setmark);

                    if (node.M == 0)
                    {
                        _intStack.Append(_emitted.Length);
                        Emit(RegexOpcode.Goto, 0);
                    }
                    _intStack.Append(_emitted.Length);
                    break;

                case RegexNodeKind.Loop | AfterChild:
                case RegexNodeKind.Lazyloop | AfterChild:
                    {
                        int StartJumpPos = _emitted.Length;
                        int Lazy = (nodeType - (RegexNodeKind.Loop | AfterChild));

                        if (node.N < int.MaxValue || node.M > 1)
                            Emit(RegexOpcode.Branchcount + Lazy, _intStack.Pop(), node.N == int.MaxValue ? int.MaxValue : node.N - node.M);
                        else
                            Emit(RegexOpcode.Branchmark + Lazy, _intStack.Pop());

                        if (node.M == 0)
                            PatchJump(_intStack.Pop(), StartJumpPos);
                    }
                    break;

                case RegexNodeKind.Capture | BeforeChild:
                    Emit(RegexOpcode.Setmark);
                    break;

                case RegexNodeKind.Capture | AfterChild:
                    Emit(RegexOpcode.Capturemark, RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping), RegexParser.MapCaptureNumber(node.N, _tree.CaptureNumberSparseMapping));
                    break;

                case RegexNodeKind.PositiveLookaround | BeforeChild:
                    Emit(RegexOpcode.Setjump); // causes lookahead/lookbehind to be non-backtracking
                    Emit(RegexOpcode.Setmark);
                    break;

                case RegexNodeKind.PositiveLookaround | AfterChild:
                    Emit(RegexOpcode.Getmark);
                    Emit(RegexOpcode.Forejump); // causes lookahead/lookbehind to be non-backtracking
                    break;

                case RegexNodeKind.NegativeLookaround | BeforeChild:
                    Emit(RegexOpcode.Setjump);
                    _intStack.Append(_emitted.Length);
                    Emit(RegexOpcode.Lazybranch, 0);
                    break;

                case RegexNodeKind.NegativeLookaround | AfterChild:
                    Emit(RegexOpcode.Backjump);
                    PatchJump(_intStack.Pop(), _emitted.Length);
                    Emit(RegexOpcode.Forejump);
                    break;

                case RegexNodeKind.Atomic | BeforeChild:
                    Emit(RegexOpcode.Setjump);
                    break;

                case RegexNodeKind.Atomic | AfterChild:
                    Emit(RegexOpcode.Forejump);
                    break;

                case RegexNodeKind.One:
                case RegexNodeKind.Notone:
                    Emit((RegexOpcode)node.Kind | bits, node.Ch);
                    break;

                case RegexNodeKind.Notoneloop:
                case RegexNodeKind.Notoneloopatomic:
                case RegexNodeKind.Notonelazy:
                case RegexNodeKind.Oneloop:
                case RegexNodeKind.Oneloopatomic:
                case RegexNodeKind.Onelazy:
                    if (node.M > 0)
                    {
                        Emit(((node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy) ?
                              RegexOpcode.Onerep : RegexOpcode.Notonerep) | bits, node.Ch, node.M);
                    }
                    if (node.N > node.M)
                    {
                        Emit((RegexOpcode)node.Kind | bits, node.Ch, node.N == int.MaxValue ? int.MaxValue : node.N - node.M);
                    }
                    break;

                case RegexNodeKind.Setloop:
                case RegexNodeKind.Setloopatomic:
                case RegexNodeKind.Setlazy:
                    {
                        int stringCode = StringCode(node.Str!);
                        if (node.M > 0)
                        {
                            Emit(RegexOpcode.Setrep | bits, stringCode, node.M);
                        }
                        if (node.N > node.M)
                        {
                            Emit((RegexOpcode)node.Kind | bits, stringCode, (node.N == int.MaxValue) ? int.MaxValue : node.N - node.M);
                        }
                    }
                    break;

                case RegexNodeKind.Multi:
                    Emit((RegexOpcode)node.Kind | bits, StringCode(node.Str!));
                    break;

                case RegexNodeKind.Set:
                    Emit((RegexOpcode)node.Kind | bits, StringCode(node.Str!));
                    break;

                case RegexNodeKind.Backreference:
                    Emit((RegexOpcode)node.Kind | bits, RegexParser.MapCaptureNumber(node.M, _tree.CaptureNumberSparseMapping));
                    break;

                case RegexNodeKind.Nothing:
                case RegexNodeKind.Bol:
                case RegexNodeKind.Eol:
                case RegexNodeKind.Boundary:
                case RegexNodeKind.NonBoundary:
                case RegexNodeKind.ECMABoundary:
                case RegexNodeKind.NonECMABoundary:
                case RegexNodeKind.Beginning:
                case RegexNodeKind.Start:
                case RegexNodeKind.EndZ:
                case RegexNodeKind.End:
                case RegexNodeKind.UpdateBumpalong:
                    Emit((RegexOpcode)node.Kind);
                    break;

                default:
                    Debug.Fail($"Unexpected node: {nodeType}");
                    break;
            }
        }
    }
}
