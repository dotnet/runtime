// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// The symbol table. When used it must be the first entry in <see cref="ArArchiveFile.Files"/>.
    /// </summary>
    public sealed class ArSymbolTable : ArFile
    {
        public const string DefaultBSDSymbolTableName = "__.SYMDEF";
        public const string DefaultGNUSymbolTableName = "/";
        
        public ArSymbolTable()
        {
            Symbols = new List<ArSymbol>();
        }

        public override string Name
        {
            get
            {
                if (Parent == null) return "<undefined>";

                return Parent.Kind == ArArchiveKind.BSD ? DefaultBSDSymbolTableName : DefaultGNUSymbolTableName;
            }
            set => base.Name = value;
        }

        public override bool IsSystem => true;

        /// <summary>
        /// Gets the symbols associated to this table.
        /// </summary>
        public List<ArSymbol> Symbols { get; }

        protected override void Read(ArArchiveFileReader reader)
        {
            long startOffset = reader.Stream.Position;

            bool isBSD = reader.ArArchiveFile.Kind == ArArchiveKind.BSD;

            // A 32-bit big endian integer, giving the number of entries in the table.
            uint entryCount = reader.Stream.ReadU32(false);

            // A set of 32-bit big endian integers. One for each symbol, recording the position within the archive of the header for the file containing this symbol.
            for (uint i = 0; i < entryCount; i++)
            {
                uint stringOffset = 0;

                if (isBSD)
                {
                    stringOffset = reader.Stream.ReadU32(false);
                }

                uint offsetOfFile = reader.Stream.ReadU32(false);

                var symbol = new ArSymbol
                {
                    NameOffset = stringOffset,
                    FileOffset = offsetOfFile,
                };
                Symbols.Add(symbol);
            }

            // A set of Zero-terminated strings. Each is a symbol name, and occurs in the same order as the list of positions in part 2.
            var startStringTableOffset = isBSD ? reader.Stream.Position : 0;

            for (uint i = 0; i < entryCount; i++)
            {
                bool hasError = false;
                var symbol = Symbols[(int)i];

                var absoluteStringOffset = startStringTableOffset + symbol.NameOffset;

                if (isBSD && absoluteStringOffset >= startOffset + (long)Size)
                {
                    hasError = true;
                }
                else
                {
                    // Only BSD requires to position correctly
                    if (isBSD)
                    {
                        reader.Stream.Position = absoluteStringOffset;
                    }

                    var text = reader.ReadStringUTF8NullTerminated();
                    symbol.Name = text;
                    Symbols[(int)i] = symbol;
                }

                if (hasError)
                {
                    reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected EOF while trying to read the string name [{i}] at file offset {absoluteStringOffset} in {this}");
                    return;
                }
            }

            var sizeRead = (ulong) (reader.Stream.Position - startOffset);
            if ((sizeRead & 1) != 0)
            {
                if (reader.Stream.ReadByte() >= 0)
                {
                    sizeRead++;
                }
            }
            Debug.Assert(Size == sizeRead);
        }

        protected override void AfterRead(DiagnosticBag diagnostics)
        {
            var offsets = new Dictionary<ulong, ArFile>();
            foreach (var fileEntry in Parent.Files)
            {
                offsets[fileEntry.Offset] = fileEntry;
            }

            for (var i = 0; i < Symbols.Count; i++)
            {
                var symbol = Symbols[i];
                if (offsets.TryGetValue(symbol.FileOffset, out var fileEntry))
                {
                    symbol.File = fileEntry;
                    Symbols[i] = symbol;
                }
                else
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidFileOffsetInSystemVSymbolLookupTable, $"Unable to find file at offset {symbol.FileOffset} for symbol entry [{i}] in {this}");
                }
            }
        }

        protected override void Write(ArArchiveFileWriter writer)
        {
            long startOffset = writer.Stream.Position;
            writer.Stream.WriteU32(false, (uint)Symbols.Count);

            uint stringOffset = 0;
            bool isBSD = Parent.Kind == ArArchiveKind.BSD;
            foreach (var symbol in Symbols)
            {
                if (isBSD)
                {
                    writer.Stream.WriteU32(false, stringOffset);
                }

                writer.Stream.WriteU32(false, (uint)symbol.File.Offset);

                if (isBSD)
                {
                    stringOffset += (uint) Encoding.UTF8.GetByteCount(symbol.Name) + 1;
                }
            }

            foreach (var symbol in Symbols)
            {
                writer.WriteStringUTF8NullTerminated(symbol.Name);
            }

            var sizeWritten = writer.Stream.Position - startOffset;
            if ((sizeWritten & 1) != 0)
            {
                writer.Stream.WriteByte(0);
                sizeWritten++;
            }

            // Double check that the size is actually matching what we have been serializing
            Debug.Assert(sizeWritten == (long)Size);
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);
            for (var i = 0; i < Symbols.Count; i++)
            {
                var symbol = Symbols[i];
                if (string.IsNullOrEmpty(symbol.Name))
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidNullOrEmptySymbolName, $"Invalid null or empty symbol name [{i}] in {this}");
                }

                if (symbol.File == null)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidNullFileForSymbol, $"Invalid null file for symbol `{symbol.Name}` [{i}] in {this}");
                }
                else if (symbol.File.Parent == null)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidNullParentFileForSymbol, $"Invalid null Parent for file `{symbol.File}` for symbol `{symbol.Name}` [{i}] in {this}");
                }
                else if (symbol.File.Parent != Parent)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidParentFileForSymbol, $"Invalid parent for file `{symbol.File}` for symbol `{symbol.Name}` [{i}] in {this}. The parent {nameof(ArArchiveFile)} is not the same instance as this symbol table");
                }
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(Symbols)} Count: {Symbols.Count}";
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            if (Parent == null) return;

            // number of entries (both for BSD and GNU)
            ulong sizeOfTable = sizeof(uint);

            foreach (var symbol in Symbols)
            {
                if (symbol.Name != null)
                {
                    sizeOfTable += (ulong)Encoding.UTF8.GetByteCount(symbol.Name) + 1;

                    // uint file_offset
                    sizeOfTable += sizeof(uint);

                    if (Parent.Kind == ArArchiveKind.BSD)
                    {
                        // uint string_offset
                        sizeOfTable += sizeof(uint);
                    }
                }
            }

            if ((sizeOfTable & 1) != 0)
            {
                sizeOfTable++;
            }

            Size = sizeOfTable;
        }
    }
}