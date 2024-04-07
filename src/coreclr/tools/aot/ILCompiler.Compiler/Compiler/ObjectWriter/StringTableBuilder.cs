// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace ILCompiler.ObjectWriter
{
    internal class StringTableBuilder
    {
        private readonly MemoryStream _stream = new();
        private readonly SortedSet<string> _reservedStrings = new(StringComparer.Ordinal);
        private Dictionary<string, uint> _stringToOffset = new(StringComparer.Ordinal);

        public void Write(FileStream stream)
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

        public void ReserveString(string text)
        {
            if (text is object && !_stringToOffset.ContainsKey(text))
            {
                _reservedStrings.Add(text);
            }
        }

        private void FlushReservedStrings()
        {
            string[] reservedStrings = _reservedStrings.ToArray();

            // Pre-sort the string based on their matching suffix
            MultiKeySort(reservedStrings, 0);

            // Add the strings to string table
            string lastText = null;
            for (int i = 0; i < reservedStrings.Length; i++)
            {
                var text = reservedStrings[i];
                uint index;
                if (lastText is not null && lastText.EndsWith(text, StringComparison.Ordinal))
                {
                    // Suffix matches the last symbol
                    index = (uint)(_stream.Length - Encoding.UTF8.GetByteCount(text) - 1);
                    _stringToOffset.Add(text, index);
                }
                else
                {
                    lastText = text;
                    CreateIndex(text);
                }
            }

            _reservedStrings.Clear();

            static char TailCharacter(string str, int pos)
            {
                int index = str.Length - pos - 1;
                if ((uint)index < str.Length)
                    return str[index];
                return '\0';
            }

            static void MultiKeySort(Span<string> input, int pos)
            {
                if (!MultiKeySortSmallInput(input, pos))
                {
                    MultiKeySortLargeInput(input, pos);
                }
            }

            static void MultiKeySortLargeInput(Span<string> input, int pos)
            {
            tailcall:
                char pivot = TailCharacter(input[0], pos);
                int l = 0, h = input.Length;
                for (int i = 1; i < h;)
                {
                    char c = TailCharacter(input[i], pos);
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

            static bool MultiKeySortSmallInput(Span<string> input, int pos)
            {
                if (input.Length <= 1)
                    return true;

                // Optimize comparing two strings
                if (input.Length == 2)
                {
                    while (true)
                    {
                        char c0 = TailCharacter(input[0], pos);
                        char c1 = TailCharacter(input[1], pos);
                        if (c0 < c1)
                        {
                            (input[0], input[1]) = (input[1], input[0]);
                            break;
                        }
                        else if (c0 > c1 || c0 == (char)0)
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

        private uint CreateIndex(string text)
        {
            uint offset = (uint)_stream.Position;
            int reservedBytes = Encoding.UTF8.GetByteCount(text) + 1;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(reservedBytes);
            var span = new Span<byte>(buffer, 0, reservedBytes);
            Encoding.UTF8.GetBytes(text, span);
            span[reservedBytes - 1] = 0;
            _stream.Write(span);
            ArrayPool<byte>.Shared.Return(buffer);
            _stringToOffset[text] = offset;
            return offset;
        }

        public uint GetStringOffset(string text)
        {
            if (_reservedStrings.Count > 0)
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
