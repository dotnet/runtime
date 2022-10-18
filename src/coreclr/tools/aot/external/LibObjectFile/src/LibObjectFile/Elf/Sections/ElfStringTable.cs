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
            // Always create an empty string
            GetOrCreateIndex(string.Empty);
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

        public uint GetOrCreateIndex(string text)
        {
            // Same as empty string
            if (text == null) return 0;

            if (_mapStringToIndex.TryGetValue(text, out uint index))
            {
                return index;
            }

            index = (uint) _table.Length;
            _mapIndexToString.Add(index, text);
            _mapStringToIndex.Add(text, index);

            if (text.Length == 0)
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
                
                // Register all subsequent strings
                while (text.Length > 0)
                {
                    text = text.Substring(1);
                    if (_mapStringToIndex.ContainsKey(text))
                    {
                        break;
                    }
                    var offset = reservedBytes - Encoding.UTF8.GetByteCount(text) - 1;
                    var subIndex = index + (uint) offset;
                    _mapStringToIndex.Add(text, subIndex);
                    _mapIndexToString.Add(subIndex, text);
                }
            }

            return index;
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

            // Always create an empty string
            GetOrCreateIndex(string.Empty);
        }
    }
}