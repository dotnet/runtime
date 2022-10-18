// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Iced.Intel;
using LibObjectFile.Ar;
using LibObjectFile.Elf;
using Decoder = Iced.Intel.Decoder;

namespace LibObjectFile.Disasm
{
    public class ObjDisasmApp
    {
        public ObjDisasmApp()
        {
            FunctionRegexFilters = new List<Regex>();
            Files = new List<string>();
            Output = Console.Out;
        }

        public List<Regex> FunctionRegexFilters { get; }
        
        public List<string> Files { get; }

        public bool Verbose { get; set; }

        public bool Listing { get; set; }

        public TextWriter Output { get; set; }

        public void Run()
        {
            foreach (var file in Files)
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);

                if (ArArchiveFile.IsAr(stream))
                {
                    var options = new ArArchiveFileReaderOptions(ArArchiveKind.GNU);

                    var archive = ArArchiveFile.Read(stream, options);

                    foreach (var objFile in archive.Files)
                    {
                        if (objFile is ArElfFile elfFile)
                        {
                            ProcessElf(objFile.Name, elfFile.ElfObjectFile);
                        }
                    }
                }
                else if (ElfObjectFile.IsElf(stream))
                {
                    var elfObjectFile = ElfObjectFile.Read(stream, new ElfReaderOptions() {ReadOnly = true});
                    ProcessElf(Path.GetFileName(file), elfObjectFile);
                }
            }
        }

        private void ProcessElf(string name, ElfObjectFile elfObjectFile)
        {
            foreach(var symbolTable in elfObjectFile.Sections.OfType<ElfSymbolTable>())
            {
                foreach(var symbol in symbolTable.Entries)
                {
                    if (symbol.Type != ElfSymbolType.Function) continue;
                    if (symbol.Bind == ElfSymbolBind.Local) continue;

                    if (FunctionRegexFilters.Count > 0)
                    {
                        foreach (var functionRegexFilter in FunctionRegexFilters)
                        {
                            if (functionRegexFilter.Match(symbol.Name).Success)
                            {
                                DumpFunction(symbol);
                                break;
                            }
                        }
                    }
                    else
                    {
                        DumpFunction(symbol);
                    }
                }
            }
        }

        private void DumpFunction(ElfSymbol symbol)
        {
            var functionSize = symbol.Size;
            var section = symbol.Section.Section;
            Output.WriteLine($"Function: {symbol.Name}");

            if (section is ElfBinarySection binarySection)
            {
                binarySection.Stream.Position = (long)symbol.Value;

                Disasm(binarySection.Stream, (uint)functionSize, Output);
                Output.WriteLine();
            }
        }

        private static void Disasm(Stream stream, uint size, TextWriter writer, Formatter formatter = null)
        {
            var buffer = ArrayPool<byte>.Shared.Rent((int)size);
            var startPosition = stream.Position;
            stream.Read(buffer, 0, (int) size);
            stream.Position = startPosition;
            
            // You can also pass in a hex string, eg. "90 91 929394", or you can use your own CodeReader
            // reading data from a file or memory etc
            var codeReader = new StreamCodeReader(stream, size);
            var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
            decoder.IP = (ulong) 0;
            ulong endRip = decoder.IP + (uint)size;

            // This list is faster than List<Instruction> since it uses refs to the Instructions
            // instead of copying them (each Instruction is 32 bytes in size). It has a ref indexer,
            // and a ref iterator. Add() uses 'in' (ref readonly).
            var instructions = new InstructionList();
            while (decoder.IP < endRip)
            {
                // The method allocates an uninitialized element at the end of the list and
                // returns a reference to it which is initialized by Decode().
                decoder.Decode(out instructions.AllocUninitializedElement());
            }

            // Formatters: Masm*, Nasm* and Gas* (AT&T)
            if (formatter == null)
            {
                formatter = new NasmFormatter();
                formatter.Options.DigitSeparator = "";
                formatter.Options.FirstOperandCharIndex = 10;
            }

            var output = new StringOutput();
            // Use InstructionList's ref iterator (C# 7.3) to prevent copying 32 bytes every iteration
            foreach (ref var instr in instructions)
            {
                // Don't use instr.ToString(), it allocates more, uses masm syntax and default options
                formatter.Format(instr, output);
                writer.Write($"{instr.IP:X16} ");
                for (int i = 0; i < instr.Length; i++)
                {
                    writer.Write(buffer[(int)instr.IP + i].ToString("X2"));
                }
                writer.Write(new string(' ', 16 * 2 - instr.Length * 2));
                writer.WriteLine($"{output.ToStringAndReset()}");
            }
        }

        private class StreamCodeReader : CodeReader
        {
            private readonly Stream _stream;
            private long _size;

            public StreamCodeReader(Stream stream, uint size)
            {
                _stream = stream;
                _size = size;
            }

            public override int ReadByte()
            {
                if (_size < 0)
                {
                    return -1;
                }

                _size--;
                return _stream.ReadByte();
            }
        }
    }
}