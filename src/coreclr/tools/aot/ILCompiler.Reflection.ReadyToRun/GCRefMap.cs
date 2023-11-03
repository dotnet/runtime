// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.IO;

namespace ILCompiler.Reflection.ReadyToRun
{
    public enum CORCOMPILE_GCREFMAP_TOKENS
    {
        GCREFMAP_SKIP = 0,
        GCREFMAP_REF = 1,
        GCREFMAP_INTERIOR = 2,
        GCREFMAP_METHOD_PARAM = 3,
        GCREFMAP_TYPE_PARAM = 4,
        GCREFMAP_VASIG_COOKIE = 5,
    }

    public struct GCRefMapEntry
    {
        public readonly int Offset;
        public readonly CORCOMPILE_GCREFMAP_TOKENS Token;

        public GCRefMapEntry(int offset, CORCOMPILE_GCREFMAP_TOKENS token)
        {
            Offset = offset;
            Token = token;
        }
    }

    public class GCRefMap
    {
        public const int GCREFMAP_LOOKUP_STRIDE = 1024;

        public const uint InvalidStackPop = ~0u;

        public readonly uint StackPop;
        public readonly GCRefMapEntry[] Entries;

        public GCRefMap()
        {
        }

        public GCRefMap(uint stackPop, GCRefMapEntry[] entries)
        {
            StackPop = stackPop;
            Entries = entries;
        }
    }

    /// <summary>
    /// Helper class for decoding the bit-oriented GC ref map format.
    /// </summary>
    public class GCRefMapDecoder
    {
        private readonly ReadyToRunReader _reader;
        private int _offset;
        private int _pendingByte;
        private int _pos;

        public GCRefMapDecoder(ReadyToRunReader reader, int offset)
        {
            _reader = reader;
            _offset = offset;
            _pendingByte = 0x80;
            _pos = 0;
        }

        public int GetBit()
        {
            int x = _pendingByte;
            if ((x & 0x80) != 0)
            {
                x = _reader.Image[_offset++];
                x |= ((x & 0x80) << 7);
            }
            _pendingByte = x >> 1;
            return x & 1;
        }

        public int GetTwoBit()
        {
            int result = GetBit();
            result |= GetBit() << 1;
            return result;
        }

        public int GetInt()
        {
            int result = 0;

            int bit = 0;
            do
            {
                result |= GetBit() << (bit++);
                result |= GetBit() << (bit++);
                result |= GetBit() << (bit++);
            }
            while (GetBit() != 0);

            return result;
        }

        public bool AtEnd()
        {
            return _pendingByte == 0;
        }

        public int GetOffset()
        {
            return _offset;
        }

        public uint ReadStackPop()
        {
            Debug.Assert(_reader.Machine == Machine.I386);

            int x = GetTwoBit();

            if (x == 3)
                x = GetInt() + 3;

            return (uint)x;
        }

        public int CurrentPos()
        {
            return _pos;
        }

        public CORCOMPILE_GCREFMAP_TOKENS ReadToken()
        {
            int val = GetTwoBit();
            if (val == 3)
            {
                int ext = GetInt();
                if ((ext & 1) == 0)
                {
                    _pos += (ext >> 1) + 4;
                    return CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_SKIP;
                }
                else
                {
                    _pos++;
                    return (CORCOMPILE_GCREFMAP_TOKENS)((ext >> 1) + 3);
                }
            }
            _pos++;
            return (CORCOMPILE_GCREFMAP_TOKENS)val;
        }

        public GCRefMap ReadMap()
        {
            TransitionBlock transitionBlock = TransitionBlock.FromReader(_reader);

            List<GCRefMapEntry> entries = new List<GCRefMapEntry>();
            uint stackPop = GCRefMap.InvalidStackPop;

            if (_reader.Machine == Machine.I386)
            {
                stackPop = ReadStackPop();
            }

            while (!AtEnd())
            {
                int pos = CurrentPos();
                CORCOMPILE_GCREFMAP_TOKENS token = ReadToken();
                if (token != CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_SKIP)
                {
                    int offset = transitionBlock.OffsetFromGCRefMapPos(pos);
                    entries.Add(new GCRefMapEntry(offset, token));
                }
            }

            if (stackPop != GCRefMap.InvalidStackPop || entries.Count > 0)
            {
                return new GCRefMap(stackPop, entries.ToArray());
            }

            return null;
        }
    }
}
