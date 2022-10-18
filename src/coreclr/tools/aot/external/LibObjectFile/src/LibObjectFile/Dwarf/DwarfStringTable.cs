// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfStringTable : DwarfSection
    {
        private readonly Dictionary<string, ulong> _stringToOffset;
        private readonly Dictionary<ulong, string> _offsetToString;
        private Stream _stream;

        public DwarfStringTable() : this(new MemoryStream())
        {
        }

        public DwarfStringTable(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _stringToOffset = new Dictionary<string, ulong>();
            _offsetToString = new Dictionary<ulong, string>();
        }

        public Stream Stream
        {
            get => _stream;
            set => _stream = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string GetStringFromOffset(ulong offset)
        {
            if (_offsetToString.TryGetValue(offset, out var text))
            {
                return text;
            }

            Stream.Position = (long) offset;
            text = Stream.ReadStringUTF8NullTerminated();
            _offsetToString[offset] = text;
            _stringToOffset[text] = offset;
            return text;
        }

        public bool Contains(string text)
        {
            if (text == null) return false;
            return _stringToOffset.ContainsKey(text);
        }

        public ulong GetOrCreateString(string text)
        {
            if (text == null) return 0;

            ulong offset;
            if (_stringToOffset.TryGetValue(text, out offset))
            {
                return offset;
            }

            Stream.Position = Stream.Length;
            offset = (ulong)Stream.Position;
            Stream.WriteStringUTF8NullTerminated(text);
            _offsetToString[offset] = text;
            _stringToOffset[text] = offset;
            return offset;
        }

        protected override void Read(DwarfReader reader)
        {
            Stream = reader.ReadAsStream((ulong) reader.Stream.Length);
            Size = reader.Offset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            writer.Write(Stream);
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            Size = (ulong?)(Stream?.Length) ?? 0UL;
        }
    }
}