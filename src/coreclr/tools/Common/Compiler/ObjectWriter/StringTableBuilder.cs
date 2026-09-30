// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    internal class StringTableBuilder
    {
        private readonly MemoryStream _stream = new();
        private readonly Dictionary<Utf8String, uint> _stringToOffset = new();
        private List<Utf8String> _reservedStrings;

        public void Write(Stream stream)
        {
            _stream.Position = 0;
            _stream.CopyTo(stream);
        }

        public uint Size
        {
            get
            {
                FlushReservedStrings();
                return (uint)_stream.Length;
            }
        }

        public void ReserveString(Utf8String text)
        {
            if (!text.IsNull && _stringToOffset.TryAdd(text, uint.MaxValue))
            {
                (_reservedStrings ??= new List<Utf8String>()).Add(text);
            }
        }

        private void FlushReservedStrings()
        {
            if (_reservedStrings is not List<Utf8String> reservedStrings)
            {
                return;
            }

            Span<Utf8String> reservedStringsSpan = CollectionsMarshal.AsSpan(reservedStrings);

            // Establish a deterministic order before the in-place suffix sort.
            reservedStringsSpan.Sort();

            // Sort strings so matching suffixes are adjacent.
            MultiKeySort(reservedStringsSpan, 0);

            // Add the strings to string table
            Utf8String lastText = default;
            for (int i = 0; i < reservedStringsSpan.Length; i++)
            {
                var text = reservedStringsSpan[i];
                uint index;
                if (!lastText.IsNull && lastText.AsSpan().EndsWith(text.AsSpan()))
                {
                    // Suffix matches the last symbol
                    index = (uint)(_stream.Length - text.Length - 1);
                    _stringToOffset[text] = index;
                }
                else
                {
                    lastText = text;
                    CreateIndex(text);
                }
            }

            _reservedStrings = null;

            static byte TailCharacter(Utf8String str, int pos)
            {
                int index = str.Length - pos - 1;
                if ((uint)index < str.Length)
                    return str.AsSpan()[index];
                return 0;
            }

            static void MultiKeySort(Span<Utf8String> input, int pos)
            {
                if (!MultiKeySortSmallInput(input, pos))
                {
                    MultiKeySortLargeInput(input, pos);
                }
            }

            static void MultiKeySortLargeInput(Span<Utf8String> input, int pos)
            {
            tailcall:
                byte pivot = TailCharacter(input[0], pos);
                int l = 0, h = input.Length;
                for (int i = 1; i < h;)
                {
                    byte c = TailCharacter(input[i], pos);
                    if (c > pivot)
                    {
                        (input[l], input[i]) = (input[i], input[l]);
                        l++; i++;
                    }
                    else if (c < pivot)
                    {
                        h--;
                        (input[h], input[i]) = (input[i], input[h]);
                    }
                    else
                    {
                        i++;
                    }
                }

                MultiKeySort(input.Slice(0, l), pos);
                MultiKeySort(input.Slice(h), pos);
                if (pivot != '\0')
                {
                    // Use a loop as a poor man's tailcall
                    // MultiKeySort(input.Slice(l, h - l), pos + 1);
                    pos++;
                    input = input.Slice(l, h - l);
                    if (!MultiKeySortSmallInput(input, pos))
                    {
                        goto tailcall;
                    }
                }
            }

            static bool MultiKeySortSmallInput(Span<Utf8String> input, int pos)
            {
                if (input.Length <= 1)
                    return true;

                // Optimize comparing two strings
                if (input.Length == 2)
                {
                    while (true)
                    {
                        byte c0 = TailCharacter(input[0], pos);
                        byte c1 = TailCharacter(input[1], pos);
                        if (c0 < c1)
                        {
                            (input[0], input[1]) = (input[1], input[0]);
                            break;
                        }
                        else if (c0 > c1 || c0 == 0)
                        {
                            break;
                        }
                        pos++;
                    }
                    return true;
                }

                return false;
            }
        }

        private uint CreateIndex(Utf8String text)
        {
            uint offset = (uint)_stream.Position;

            _stream.Write(text.AsSpan());
            _stream.WriteByte(0);

            _stringToOffset[text] = offset;
            return offset;
        }

        public uint GetStringOffset(Utf8String text)
        {
            if (_reservedStrings is not null)
            {
                FlushReservedStrings();
            }

            if (_stringToOffset.TryGetValue(text, out uint index))
            {
                return index;
            }

            return CreateIndex(text);
        }
    }
}
