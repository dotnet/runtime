// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A string table section with the type <see cref="ElfSectionType.StringTable"/>.
    /// </summary>
    public class ElfStringTable : ElfSection
    {
        private readonly MemoryStream _table;
        private readonly List<string> _reservedStrings;
        private readonly Dictionary<string, uint> _mapStringToIndex;
        private readonly Dictionary<uint, string> _mapIndexToString;

        public const string DefaultName = ".strtab";

        public const int DefaultCapacity = 256;

        public ElfStringTable() : this(DefaultCapacity)
        {
        }

        public ElfStringTable(int capacityInBytes) : base(ElfSectionType.StringTable)
        {
            if (capacityInBytes < 0) throw new ArgumentOutOfRangeException(nameof(capacityInBytes));
            Name = DefaultName;
            _table = new MemoryStream(capacityInBytes);
            _mapStringToIndex = new Dictionary<string, uint>();
            _mapIndexToString = new Dictionary<uint, string>();
            _reservedStrings = new List<string>();
            // Always create an empty string
            CreateIndex(string.Empty);
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.StringTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfStringTable)}`. Only `{ElfSectionType.StringTable}` is valid");
                }
                base.Type = value;
            }
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            if (_reservedStrings.Count > 0) FlushReservedStrings();
            Size = (ulong)_table.Length;
        }

        protected override void Read(ElfReader reader)
        {
            Debug.Assert(_table.Position == 1 && _table.Length == 1);
            var length = (long) base.Size;
            _table.SetLength(length);
            var buffer = _table.GetBuffer();
            reader.Stream.Read(buffer, 0, (int)length);
            _table.Position = _table.Length;
        }

        protected override void Write(ElfWriter writer)
        {
            writer.Stream.Write(_table.GetBuffer(), 0, (int)_table.Length);
        }

        internal void ReserveString(string text)
        {
            if (text is object && !_mapStringToIndex.ContainsKey(text))
            {
                _reservedStrings.Add(text);
            }
        }

        internal void FlushReservedStrings()
        {
            // TODO: Use CollectionsMarshal.AsSpan
            string[] reservedStrings = _reservedStrings.ToArray();

            // Pre-sort the string based on their matching suffix
            MultiKeySort(reservedStrings, 0);

            // Add the strings to string table
            string lastText = null;
            for (int i = 0; i < reservedStrings.Length; i++)
            {
                var text = reservedStrings[i];
                uint index;
                if (lastText != null && lastText.EndsWith(text, StringComparison.Ordinal))
                {
                    // Suffix matches the last symbol
                    index = (uint)(_table.Length - Encoding.UTF8.GetByteCount(text) - 1);
                    _mapIndexToString.Add(index, text);
                    _mapStringToIndex.Add(text, index);
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
            uint index = (uint) _table.Length;
            _mapIndexToString.Add(index, text);
            _mapStringToIndex.Add(text, index);

            if (index == 0)
            {
                Debug.Assert(index == 0);
                _table.WriteByte(0);
            }
            else
            {
                var reservedBytes = Encoding.UTF8.GetByteCount(text) + 1;
                var buffer = ArrayPool<byte>.Shared.Rent(reservedBytes);
                var span = new Span<byte>(buffer, 0, reservedBytes);
                Encoding.UTF8.GetEncoder().GetBytes(text, span, true);
                span[reservedBytes - 1] = 0;
                if (_table.Position != index)
                {
                    _table.Position = index;
                }
                _table.Write(span);
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return index;
        }

        public uint GetOrCreateIndex(string text)
        {
            // Same as empty string
            if (text == null) return 0;

            if (_reservedStrings.Count > 0) FlushReservedStrings();

            if (_mapStringToIndex.TryGetValue(text, out uint index))
            {
                return index;
            }

            return CreateIndex(text);
        }

        public bool TryResolve(ElfString inStr, out ElfString outStr)
        {
            outStr = inStr;
            if (inStr.Value != null)
            {
                outStr = inStr.WithIndex(GetOrCreateIndex(inStr.Value));
            }
            else
            {
                if (TryFind(inStr.Index, out var text))
                {
                    outStr = inStr.WithName(text);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool TryFind(uint index, out string text)
        {
            if (index == 0)
            {
                text = string.Empty;
                return true;
            }

            if (_reservedStrings.Count > 0) FlushReservedStrings();

            if (_mapIndexToString.TryGetValue(index, out text))
            {
                return true;
            }

            if (index >= _table.Length)
            {
                return false;
            }

            _table.Position = index;

            var buffer = _table.GetBuffer();
            var indexOfByte0 = Array.IndexOf(buffer, (byte)0, (int)index);

            if (indexOfByte0 < 0 || indexOfByte0 >= _table.Length)
            {
                indexOfByte0 = (int)(_table.Length - 1);
            }

            var strLength = (int)(indexOfByte0 - index);
            text = Encoding.UTF8.GetString(buffer, (int)index, strLength);
            _mapIndexToString.Add(index, text);

            // Don't try to override an existing mapping
            if (!_mapStringToIndex.TryGetValue(text, out var existingIndex))
            {
                _mapStringToIndex.Add(text, index);
            }

            return true;
        }

        public void Reset()
        {
            _table.SetLength(0);
            _mapStringToIndex.Clear();
            _mapIndexToString.Clear();
            _reservedStrings.Clear();

            // Always create an empty string
            CreateIndex(string.Empty);
        }
    }
}