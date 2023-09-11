
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {LineSequences.Count,nq}")]
    public sealed class DwarfLineProgramTable : DwarfObject<DwarfLineSection>
    {
        private readonly Dictionary<string, uint> _directoryNameToIndex;
        private readonly Dictionary<DwarfFileName, uint> _fileNameToIndex;
        private readonly List<string> _directoryNames;
        private readonly List<DwarfLineSequence> _lineSequences;
        private readonly DwarfLine _stateLine;
        private byte _minimumInstructionLength;
        private byte _maximumOperationsPerInstruction;
        private readonly List<uint> _standardOpCodeLengths;

        public DwarfLineProgramTable()
        {
            FileNames = new List<DwarfFileName>();
            _lineSequences = new List<DwarfLineSequence>();
            _directoryNameToIndex = new Dictionary<string, uint>();
            _fileNameToIndex = new Dictionary<DwarfFileName, uint>();
            _directoryNames = new List<string>();
            _stateLine = new DwarfLine();
            Version = 2;
            LineBase = -5;
            LineRange = 14;
            _minimumInstructionLength = 1;
            _maximumOperationsPerInstruction = 1;
            _standardOpCodeLengths = new List<uint>();
            foreach (var stdOpCode in DefaultStandardOpCodeLengths)
            {
                _standardOpCodeLengths.Add(stdOpCode);
            }
        }

        public bool Is64BitEncoding { get; set; }

        public DwarfAddressSize AddressSize { get; set; }

        public ushort Version { get; set; }

        public sbyte LineBase { get; set; }
        
        public byte LineRange { get; set; }

        public ulong HeaderLength { get; private set; }

        public List<uint> StandardOpCodeLengths => _standardOpCodeLengths;

        public byte OpCodeBase
        {
            get => (byte)(StandardOpCodeLengths.Count + 1);
        }

        public byte MinimumInstructionLength
        {
            get => _minimumInstructionLength;
            set
            {
                if (value == 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0");
                _minimumInstructionLength = value;
            }
        }

        public byte MaximumOperationsPerInstruction
        {
            get => _maximumOperationsPerInstruction;
            set
            {
                if (value == 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0");
                _maximumOperationsPerInstruction = value;
            }
        } 

        public List<DwarfFileName> FileNames { get; }

        public IReadOnlyList<DwarfLineSequence> LineSequences => _lineSequences;

        public void AddLineSequence(DwarfLineSequence line)
        {
            _lineSequences.Add(this, line);
        }

        public void RemoveLineSequence(DwarfLineSequence line)
        {
            _lineSequences.Remove(this, line);
        }

        public DwarfLineSequence RemoveLineSequenceAt(int index)
        {
            return _lineSequences.RemoveAt(this, index);
        }

        protected override void Read(DwarfReader reader)
        {
            var log = reader.Log;
            var startOfSection = reader.Offset;

            reader.OffsetToLineProgramTable.Add(startOfSection, this);

            var unitLength = reader.ReadUnitLength();
            Is64BitEncoding = reader.Is64BitEncoding;
            AddressSize = reader.AddressSize;
            var startPosition = reader.Offset;
            var version = reader.ReadU16();

            if (version < 2 || version >= 5)
            {
                throw new NotSupportedException($"Version .debug_line {version} not supported");
            }

            Version = version;

            var header_length = reader.ReadUIntFromEncoding();
            HeaderLength = header_length;
            var minimum_instruction_length = reader.ReadU8();
            MinimumInstructionLength = minimum_instruction_length;

            byte maximum_operations_per_instruction = 1;
            if (version >= 4)
            {
                maximum_operations_per_instruction = reader.ReadU8();
            }
            MaximumOperationsPerInstruction = maximum_operations_per_instruction;

            var default_is_stmt = reader.ReadU8();
            var line_base = reader.ReadI8();
            LineBase = line_base;
            var line_range = reader.ReadU8();
            LineRange = line_range;
            var opcode_base = reader.ReadU8();
            
            if (log != null)
            {
                log.WriteLine();
                log.WriteLine($"  Offset:                      0x{startOfSection:x}");
                log.WriteLine($"  Length:                      {unitLength}");
                log.WriteLine($"  DWARF Version:               {Version}");
                log.WriteLine($"  Prologue Length:             {header_length}");
                log.WriteLine($"  Minimum Instruction Length:  {minimum_instruction_length}");
                if (Version >= 4)
                {
                    log.WriteLine($"  Maximum Operations Per Instruction:  {maximum_operations_per_instruction}");
                }
                log.WriteLine($"  Initial value of 'is_stmt':  {default_is_stmt}");
                log.WriteLine($"  Line Base:                   {line_base}");
                log.WriteLine($"  Line Range:                  {line_range}");
                log.WriteLine($"  Opcode Base:                 {opcode_base}");
            }
            
            _standardOpCodeLengths.Clear();
            for (int i = 1; i < opcode_base; i++)
            {
                var opcode_length = reader.ReadULEB128AsU32();
                _standardOpCodeLengths.Add((uint)opcode_length);
                if (i - 1 <= DefaultStandardOpCodeLengths.Length && opcode_length != DefaultStandardOpCodeLengths[i - 1])
                {
                    reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"The standard opcode length at [{i}] = {opcode_length} is different from the expected length {DefaultStandardOpCodeLengths[i - 1]}");
                }
            }

            if (log != null && opcode_base > 0)
            {
                log.WriteLine();
                log.WriteLine(" Opcodes:");
                for (int i = 0; i < _standardOpCodeLengths.Count; i++)
                {
                    var argCount = _standardOpCodeLengths[i];
                    log.WriteLine($"  Opcode {i + 1} has {argCount} {((argCount == 0 || argCount > 1) ? "args" : "arg")}");
                }
            }

            var directoriesOffset = reader.Offset;
            var directories = new List<string>();
            while (true)
            {
                var dir = reader.ReadStringUTF8NullTerminated();
                if (string.IsNullOrEmpty(dir))
                {
                    break;
                }

                directories.Add(dir);
            }

            if (log != null)
            {
                log.WriteLine();
                if (directories.Count > 0)
                {
                    log.WriteLine($" The Directory Table (offset 0x{directoriesOffset:x}):");
                    for (int i = 0; i < directories.Count; i++)
                    {
                        log.WriteLine($"  {i + 1}\t{directories[i]}");
                    }
                }
                else
                {
                    log.WriteLine(" The Directory Table is empty.");
                }
            }

            var fileNamesOffset = reader.Offset;
            bool printDumpHeader = true;
            while (true)
            {
                var name = reader.ReadStringUTF8NullTerminated();

                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                var fileName = new DwarfFileName {Name = name};

                var directoryIndex = reader.ReadULEB128();
                if (!name.Contains('/') && !name.Contains('\\') && directoryIndex > 0 && (directoryIndex - 1) < (ulong) directories.Count)
                {
                    fileName.Directory = directories[(int) directoryIndex - 1];
                }
                else
                {
                    // log error
                }

                fileName.Time = reader.ReadULEB128();
                fileName.Size = reader.ReadULEB128();

                if (log != null)
                {
                    if (printDumpHeader)
                    {
                        log.WriteLine();
                        log.WriteLine($" The File Name Table (offset 0x{fileNamesOffset:x}):");
                        log.WriteLine($"  Entry\tDir\tTime\tSize\tName");
                        printDumpHeader = false;
                    }
                    log.WriteLine($"  {FileNames.Count + 1}\t{directoryIndex}\t{fileName.Time}\t{fileName.Size}\t{name}");
                }

                FileNames.Add(fileName);
            }

            if (log != null && printDumpHeader)
            {
                log.WriteLine();
                log.WriteLine(" The File Name Table is empty.");
            }

            var state = _stateLine;
            state.Offset = reader.Offset;
            var firstFileName = FileNames.Count > 0 ? FileNames[0] : null;
            state.Reset(firstFileName, default_is_stmt != 0);

            var intFileNameCount = FileNames.Count;

            printDumpHeader = true;
            var currentSequence = new DwarfLineSequence {Offset = state.Offset};

            while (true)
            {
                var currentLength = reader.Offset - startPosition;
                if (currentLength >= unitLength)
                {
                    break;
                }

                if (log != null)
                {
                    if (printDumpHeader)
                    {
                        log.WriteLine();
                        log.WriteLine(" Line Number Statements:");
                        printDumpHeader = false;
                    }

                    log.Write($"  [0x{reader.Offset:x8}]");
                }

                var opcode = reader.ReadU8();
                switch (opcode)
                {
                    case DwarfNative.DW_LNS_copy:
                        currentSequence.Add(state.Clone());
                        state.Offset = reader.Offset;
                        state.SpecialReset();
                        if (log != null)
                        {
                            log.WriteLine("  Copy");
                        }
                        break;
                    case DwarfNative.DW_LNS_advance_pc:
                    {
                        var operation_advance = reader.ReadULEB128() * minimum_instruction_length;

                        ulong deltaAddress = operation_advance;
                        if (version >= 4)
                        {
                            deltaAddress = minimum_instruction_length * ((state.OperationIndex + operation_advance) / maximum_operations_per_instruction);
                            state.OperationIndex = (byte)((state.OperationIndex + operation_advance) % maximum_operations_per_instruction);
                        }
                        state.Address += deltaAddress;

                        if (log != null)
                        {
                            if (minimum_instruction_length == 1)
                            {
                                log.WriteLine($"  Advance PC by {deltaAddress} to 0x{state.Address:x}");
                            }
                            else
                            {
                                log.WriteLine($"  Advance PC by {deltaAddress} to 0x{state.Address:x}[{state.OperationIndex}]");
                            }
                        }
                        break;
                    }
                    case DwarfNative.DW_LNS_advance_line:
                        var deltaLine = reader.ReadILEB128();
                        state.Line = (uint) (state.Line + deltaLine);
                        if (log != null)
                        {
                            log.WriteLine($"  Advance Line by {deltaLine} to {state.Line}");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_file:
                        var fileIndex = reader.ReadLEB128AsI32();
                        if (fileIndex == 0 || (fileIndex - 1) >= FileNames.Count )
                        {
                            state.File = null;
                        }
                        else
                        {
                            state.File = FileNames[fileIndex - 1];
                        }
                        if (log != null)
                        {
                            log.WriteLine($"  Set File Name to entry {fileIndex} in the File Name Table");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_column:
                        state.Column = reader.ReadULEB128AsU32();
                        if (log != null)
                        {
                            log.WriteLine($"  Set column to {state.Column}");
                        }
                        break;
                    case DwarfNative.DW_LNS_negate_stmt:
                        state.IsStatement = !state.IsStatement;
                        if (log != null)
                        {
                            log.WriteLine($"  Set is_stmt to {(state.IsStatement ? 1 : 0)}");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_basic_block:
                        state.IsBasicBlock = true;
                        if (log != null)
                        {
                            log.WriteLine($"  Set basic block");
                        }
                        break;
                    case DwarfNative.DW_LNS_const_add_pc:
                    {
                        // Advance by opcode 255
                        var adjusted_opcode = 255 - opcode_base;
                        var operation_advance = (ulong) adjusted_opcode / line_range;

                        ulong deltaAddress = operation_advance;
                        if (version >= 4)
                        {
                            deltaAddress = minimum_instruction_length * ((state.OperationIndex + operation_advance) / maximum_operations_per_instruction);
                            state.OperationIndex = (byte)((state.OperationIndex + operation_advance) % maximum_operations_per_instruction);
                        }
                        else
                        {
                            deltaAddress *= minimum_instruction_length;
                        }
                        state.Address += deltaAddress;

                        if (log != null)
                        {
                            if (minimum_instruction_length == 1)
                            {
                                log.WriteLine($"  Advance PC by constant {deltaAddress} to 0x{state.Address:x}");
                            }
                            else
                            {
                                log.WriteLine($"  Advance PC by constant {deltaAddress} to 0x{state.Address:x}[{state.OperationIndex}]");
                            }
                        }
                        break;
                    }
                    case DwarfNative.DW_LNS_fixed_advance_pc:
                        var fixedDelta = reader.ReadU16();
                        state.Address += fixedDelta;
                        state.OperationIndex = 0;
                        if (log != null)
                        {
                            log.WriteLine($"  Advance PC by fixed size amount {fixedDelta} to 0x{state.Address:x}");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_prologue_end:  // DWARF 3
                        state.IsPrologueEnd = true;
                        if (log != null)
                        {
                            log.WriteLine($"  Set prologue_end to true");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_epilogue_begin:  // DWARF 3
                        state.IsEpilogueBegin = true;
                        if (log != null)
                        {
                            log.WriteLine($"  Set epilogue_begin to true");
                        }
                        break;
                    case DwarfNative.DW_LNS_set_isa: // DWARF 3
                        state.Isa = reader.ReadULEB128();
                        if (log != null)
                        {
                            log.WriteLine($"  Set ISA to {state.Isa}");
                        }
                        break;
                    case 0:
                        var sizeOfExtended = reader.ReadULEB128();
                        var lengthOffset = reader.Offset;
                        var endOffset = reader.Offset + sizeOfExtended;
                        bool hasValidOpCode = true;
                        if (reader.Offset < endOffset)
                        {
                            var sub_opcode = reader.ReadU8();

                            // extended opcode
                            if (log != null)
                            {
                                log.Write($"  Extended opcode {sub_opcode}: ");
                            }
                            
                            switch (sub_opcode)
                            {
                                case DwarfNative.DW_LNE_end_sequence:
                                    currentSequence.Add(state.Clone());
                                    currentSequence.Size = reader.Offset - currentSequence.Offset;
                                    AddLineSequence(currentSequence);

                                    currentSequence = new DwarfLineSequence() {Offset = reader.Offset};

                                    state.Offset = reader.Offset;
                                    state.Reset(firstFileName, default_is_stmt != 0);
                                    if (log != null)
                                    {
                                        log.WriteLine("End of Sequence");
                                        log.WriteLine();
                                    }
                                    break;
                                case DwarfNative.DW_LNE_set_address:
                                    state.Address = reader.ReadUInt();
                                    state.OperationIndex = 0;
                                    if (log != null)
                                    {
                                        log.WriteLine($"set Address to 0x{state.Address:x}");
                                    }
                                    break;
                                case DwarfNative.DW_LNE_define_file:
                                    var fileName = reader.ReadStringUTF8NullTerminated();
                                    var fileDirectoryIndex = reader.ReadLEB128AsI32();
                                    var fileTime = reader.ReadULEB128();
                                    var fileSize = reader.ReadULEB128();

                                    var debugFileName = new DwarfFileName() {Name = fileName};
                                    debugFileName.Directory = fileDirectoryIndex == 0 || fileDirectoryIndex >= directories.Count ? null : directories[fileDirectoryIndex - 1];
                                    debugFileName.Time = fileTime;
                                    debugFileName.Size = fileSize;

                                    state.File = debugFileName;

                                    if (log != null)
                                    {
                                        log.WriteLine("define new File Table entry");
                                        log.WriteLine($"  Entry\tDir\tTime\tSize\tName");
                                        intFileNameCount++;
                                        log.WriteLine($"  {intFileNameCount + 1}\t{fileDirectoryIndex}\t{debugFileName.Time}\t{debugFileName.Size,-7}\t{fileName}");
                                        log.WriteLine();
                                    }
                                    break;
                                case DwarfNative.DW_LNE_set_discriminator: // DWARF 4
                                    state.Discriminator = reader.ReadULEB128();
                                    if (log != null)
                                    {
                                        log.WriteLine($"set Discriminator to {state.Discriminator}");
                                    }
                                    break;
                                default:
                                    if (log != null)
                                    {
                                        log.WriteLine($"Unknown opcode");
                                    }

                                    hasValidOpCode = false;
                                    // TODO: Add support for pluggable handling of extensions
                                    reader.Diagnostics.Warning(DiagnosticId.DWARF_WRN_UnsupportedLineExtendedCode, $"Unsupported line extended opcode 0x{sub_opcode:x}");
                                    break;
                            }

                        }

                        // Log a warning if the end offset doesn't match what we are expecting
                        if (hasValidOpCode && reader.Offset != endOffset)
                        {
                            reader.Diagnostics.Warning(DiagnosticId.DWARF_WRN_InvalidExtendedOpCodeLength, $"Invalid length {sizeOfExtended} at offset 0x{lengthOffset:x}");
                        }

                        reader.Offset = endOffset;
                        break;
                    default:
                        if (opcode < opcode_base)
                        {
                            // If this is a standard opcode but not part of DWARF ("extension")
                            // we still want to be able to continue debugging
                            Debug.Assert(opcode > 0);
                            var numberOfLEB128Args = _standardOpCodeLengths[opcode - 1];
                            for (ulong i = 0; i < numberOfLEB128Args; i++)
                            {
                                reader.ReadULEB128();
                            }

                            if (log != null)
                            {
                                log.WriteLine("Unsupported standard opcode with {numberOfLEB128Args} LEB128 args skipped");
                            }
                        }
                        else
                        {
                            // Special opcode
                            var adjusted_opcode = opcode - opcode_base;
                            var operation_advance = (ulong)adjusted_opcode / line_range;
                            var line_inc = line_base + (adjusted_opcode % line_range);
                            state.Line = (uint)(state.Line + line_inc);

                            ulong deltaAddress;

                            if (version >= 4)
                            {
                                deltaAddress = minimum_instruction_length * ((state.OperationIndex + operation_advance) / maximum_operations_per_instruction);
                                state.Address += deltaAddress;
                                state.OperationIndex = (byte)((state.OperationIndex + operation_advance) % maximum_operations_per_instruction);
                            }
                            else
                            {
                                deltaAddress = operation_advance;
                                state.Address = state.Address + operation_advance;
                            }

                            if (log != null)
                            {
                                if (minimum_instruction_length == 1)
                                {
                                    log.Write($"  Special opcode {adjusted_opcode}: advance Address by {deltaAddress} to 0x{state.Address:x}");
                                }
                                else
                                {
                                    log.Write($"  Special opcode {adjusted_opcode}: advance Address by {deltaAddress} to 0x{state.Address:x}[{state.OperationIndex}]");
                                }

                                // TODO: Make verbose version
                                log.WriteLine($" and Line by {line_inc} to {state.Line}");
                            }

                            currentSequence.Add(state.Clone());
                            state.Offset = reader.Offset;
                            state.SpecialReset();
                        }

                        break;
                }
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            if (Version < 2 || Version >= 5)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version .debug_line {Version} not supported");
            }

            if (AddressSize == DwarfAddressSize.None)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Address size for .debug_line cannot be None/0");
            }

            if (StandardOpCodeLengths.Count < DefaultStandardOpCodeLengths.Length)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidNumberOfStandardOpCodeLengths, $"Invalid length {StandardOpCodeLengths.Count} of {nameof(StandardOpCodeLengths)}. Expecting standard opcode length >= {DefaultStandardOpCodeLengths.Length} for {this}.");
            }
            else
            {
                for (int i = 0; i < DefaultStandardOpCodeLengths.Length; i++)
                {
                    var opCodeLength = StandardOpCodeLengths[i];
                    var expectedOpCodeLength = DefaultStandardOpCodeLengths[i];

                    if (opCodeLength != expectedOpCodeLength)
                    {
                        diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidStandardOpCodeLength, $"Invalid Standard OpCode Length {opCodeLength} for OpCode {i+1}. Expecting {expectedOpCodeLength} for {this}.");
                    }
                }
            }

            var startLine = LineBase;
            var endLine = LineBase + LineRange;
            if (startLine > 0 || endLine <= 0)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidStandardOpCodeLength, $"Invalid value for {nameof(LineBase)} = {LineBase} and/or {nameof(LineRange)} = {LineRange}. Expecting the range to cover the Line 0 for {this}");
            }
            else
            {
                // TODO: take into account MaximumOperationsPerInstruction 
                var maxAdjustedOpCode = 255 - OpCodeBase;
                int maxAddressIncrement = maxAdjustedOpCode / LineRange;
                if (maxAdjustedOpCode <= 0 || maxAddressIncrement < MinimumInstructionLength)
                {
                    diagnostics.Error(DiagnosticId.DWARF_WRN_CannotEncodeAddressIncrement, $"Cannot encode properly address increment with {nameof(LineBase)} = {LineBase}, {nameof(LineRange)} = {LineRange} and {nameof(StandardOpCodeLengths)}. The combination of {nameof(LineRange)} and {nameof(OpCodeBase)} are not making enough room for encoding address increment for {this}");
                }
            }

            if (MaximumOperationsPerInstruction > 1 && Version < 4)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidMaximumOperationsPerInstruction, $"Invalid {nameof(MaximumOperationsPerInstruction)} = {MaximumOperationsPerInstruction}. Must be == 1 for {this}");
            }

            for (var i = 0; i < FileNames.Count; i++)
            {
                var fileName = FileNames[i];
                if (fileName == null)
                {
                    diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidNullFileNameEntry, $"Invalid null {nameof(FileNames)} entry at [{i}] for {this}");
                }
                else
                {
                    if (fileName.Name == null)
                    {
                        diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidFileName, $"Invalid null filename for {nameof(FileNames)} entry at [{i}] for {this}");
                    }
                }
            }

            // Check that address increment is positive
            foreach (var lineSequence in _lineSequences)
            {
                var lines = lineSequence.Lines;
                ulong previousAddress = 0;
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var deltaAddress = (long)line.Address - (long)previousAddress;
                    if (deltaAddress < 0)
                    {
                        diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidNegativeAddressDelta, $"Invalid address 0x{line.Address:x} after previous 0x{previousAddress:x} for debug line entry at [{i}]. The increment must be positive. for {this}");
                    }
                    previousAddress = line.Address;

                    if (line.OperationIndex >= MaximumOperationsPerInstruction)
                    {
                        diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidOperationIndex, $"Invalid operation index {line.OperationIndex} must be < {MaximumOperationsPerInstruction} for debug line entry at [{i}] for {this}");
                    }
                }
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            ulong sizeOf = 0;

            // unit_length
            sizeOf += DwarfHelper.SizeOfUnitLength(Is64BitEncoding);

            sizeOf += 2; // version (uhalf)

            // header length (calculated just after file names and size added as well)
            sizeOf += DwarfHelper.SizeOfUInt(Is64BitEncoding);
            ulong headerLengthStart = sizeOf;
            
            // minimum_instruction_length
            sizeOf++;

            if (Version >= 4)
            {
                // maximum_operations_per_instruction
                sizeOf++;
            }

            // default_is_stmt
            // line_base
            // line_range
            // opcode_base
            sizeOf += 4;

            // StandardOpCodeLengths
            foreach (var opcodeLength in _standardOpCodeLengths)
            {
                sizeOf += DwarfHelper.SizeOfULEB128(opcodeLength);
            }
            
            // Write directory names
            _directoryNameToIndex.Clear();
            _directoryNames.Clear();
            _fileNameToIndex.Clear();

            foreach (var fileName in FileNames)
            {
                uint dirIndex = 0;
                if (fileName.Directory != null)
                {
                    var directoryName = fileName.Directory;
                    RecordDirectory(directoryName, ref sizeOf, out dirIndex);
                }

                _fileNameToIndex.Add(fileName, (uint)_fileNameToIndex.Count + 1);

                sizeOf += (ulong)Encoding.UTF8.GetByteCount(fileName.Name) + 1;
                sizeOf += DwarfHelper.SizeOfULEB128(dirIndex);
                sizeOf += DwarfHelper.SizeOfULEB128(fileName.Time);
                sizeOf += DwarfHelper.SizeOfULEB128(fileName.Size);
            }
            // byte 0 => end of directory names + end of file names
            sizeOf += 2;

            HeaderLength = sizeOf - headerLengthStart;

            LayoutDebugLineOpCodes(ref sizeOf, OpCodeBase);

            Size = sizeOf;
        }

        private void RecordDirectory(string directoryName, ref ulong sizeOf, out uint dirIndex)
        {
            if (!_directoryNameToIndex.TryGetValue(directoryName, out dirIndex))
            {
                dirIndex = (uint) _directoryNames.Count + 1;
                _directoryNameToIndex.Add(directoryName, dirIndex);
                sizeOf += (ulong) Encoding.UTF8.GetByteCount(directoryName) + 1;
                _directoryNames.Add(directoryName);
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;

            writer.Is64BitEncoding = Is64BitEncoding;
            writer.WriteUnitLength(Size - DwarfHelper.SizeOfUnitLength(Is64BitEncoding));
            
            writer.WriteU16(Version);
            writer.WriteUIntFromEncoding(HeaderLength);

            var startOfHeader = writer.Offset;

            writer.WriteU8(MinimumInstructionLength);

            if (Version >= 4)
            {
                writer.WriteU8(MaximumOperationsPerInstruction);
            }

            // default_is_stmt
            writer.WriteU8(1);

            // line_base
            writer.WriteI8(LineBase);

            // line_range
            writer.WriteU8(LineRange);

            // opcode_base
            writer.WriteU8(OpCodeBase);

            // standard_opcode_lengths
            foreach (var opcodeLength in StandardOpCodeLengths)
            {
                writer.WriteULEB128(opcodeLength);
            }

            // Write directory names
            foreach (var directoryName in _directoryNames)
            {
                writer.WriteStringUTF8NullTerminated(directoryName);
            }
            // empty string
            writer.WriteU8(0);

            // Write filenames
            foreach (var fileName in FileNames)
            {
                writer.WriteStringUTF8NullTerminated(fileName.Name);

                uint directoryIndex = 0;
                if (fileName.Directory != null)
                {
                    directoryIndex = _directoryNameToIndex[fileName.Directory];
                }

                writer.WriteULEB128(directoryIndex);
                writer.WriteULEB128(fileName.Time);
                writer.WriteULEB128(fileName.Size);
            }
            // empty string
            writer.WriteU8(0);

            var headSizeWritten = writer.Offset - startOfHeader;
            Debug.Assert(HeaderLength == headSizeWritten, $"Expected Header Length: {HeaderLength} != Written Header Length: {headSizeWritten}");
            
            WriteDebugLineOpCodes(writer, OpCodeBase);

            Debug.Assert(Size == writer.Offset - startOffset, $"Expected Size: {Size} != Written Size: {writer.Offset - startOffset}");
        }

        private void WriteDebugLineOpCodes(DwarfWriter writer, uint opCodeBase)
        {
            var previousLineState = new DwarfLineState();
            var firstFile = FileNames.Count > 0 ? FileNames[0] : null;
            previousLineState.Reset(firstFile, true);
            var initialState = previousLineState;

            uint maxDeltaAddressPerSpecialCode;
            byte maxOperationAdvance = (byte) ((255 - OpCodeBase) / LineRange);
            if (Version >= 4)
            {
                maxDeltaAddressPerSpecialCode = (uint)maxOperationAdvance / MaximumOperationsPerInstruction;
            }
            else
            {
                maxDeltaAddressPerSpecialCode = maxOperationAdvance;
            }
            maxDeltaAddressPerSpecialCode *= MinimumInstructionLength;

            bool hasSetAddress;

            foreach (var lineSequence in _lineSequences)
            {
                var lines = lineSequence.Lines;

                hasSetAddress = false;

                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var debugLine = lines[lineIndex];
                    ulong deltaAddress;
                    int deltaOperationIndex;
                    bool fileNameChanged;
                    int deltaLine;
                    int deltaColumn;
                    bool isStatementChanged;
                    bool isBasicBlockChanged;
                    bool isEndSequenceChanged;
                    bool isPrologueEndChanged;
                    bool isEpilogueBeginChanged;
                    bool isaChanged;
                    bool isDiscriminatorChanged;

                    bool hasGeneratedRow = false;

                    var debugLineState = debugLine.ToState();

                    previousLineState.Delta(debugLineState, out deltaAddress,
                        out deltaOperationIndex,
                        out fileNameChanged,
                        out deltaLine,
                        out deltaColumn,
                        out isStatementChanged,
                        out isBasicBlockChanged,
                        out isEndSequenceChanged,
                        out isPrologueEndChanged,
                        out isEpilogueBeginChanged,
                        out isaChanged,
                        out isDiscriminatorChanged);

                    Debug.Assert(debugLine.Offset == writer.Offset, $"Expected Debug Line Offset: {debugLine.Offset} != Written Offset: {writer.Offset}");

                    // DW_LNS_set_column
                    if (deltaColumn != 0)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_set_column);
                        writer.WriteULEB128(debugLine.Column);
                    }

                    // DW_LNS_set_file or DW_LNE_define_file
                    if (fileNameChanged)
                    {
                        var fileName = debugLine.File;

                        // DW_LNS_set_file
                        if (_fileNameToIndex.TryGetValue(fileName, out var fileIndex))
                        {
                            writer.WriteU8(DwarfNative.DW_LNS_set_file);
                            writer.WriteULEB128(fileIndex);
                        }
                        else
                        {
                            // DW_LNE_define_file
                            writer.WriteU8(0);
                            uint dirIndex = fileName.Directory != null && _directoryNameToIndex.ContainsKey(fileName.Directory) ? _directoryNameToIndex[fileName.Directory] : 0;

                            ulong sizeOfInlineFileName = 1;
                            sizeOfInlineFileName += (ulong) Encoding.UTF8.GetByteCount(fileName.Name) + 1;
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(dirIndex);
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(fileName.Time);
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(fileName.Size);

                            writer.WriteULEB128(sizeOfInlineFileName);

                            writer.WriteU8(DwarfNative.DW_LNE_define_file);
                            writer.WriteStringUTF8NullTerminated(fileName.Name);
                            writer.WriteULEB128(dirIndex);
                            writer.WriteULEB128(fileName.Time);
                            writer.WriteULEB128(fileName.Size);
                        }
                    }

                    // DW_LNS_copy
                    if (isBasicBlockChanged && !debugLine.IsBasicBlock ||
                        isPrologueEndChanged && !debugLine.IsPrologueEnd ||
                        isEpilogueBeginChanged && !debugLine.IsEpilogueBegin)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_copy);
                        isDiscriminatorChanged = debugLine.Discriminator != 0;
                        hasGeneratedRow = true;
                    }

                    // DW_LNS_set_basic_block
                    if (isBasicBlockChanged && debugLine.IsBasicBlock)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_set_basic_block);
                    }

                    // DW_LNS_set_prologue_end
                    if (isPrologueEndChanged && debugLine.IsPrologueEnd)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_set_prologue_end);
                    }

                    // DW_LNS_set_epilogue_begin
                    if (isEpilogueBeginChanged && debugLine.IsEpilogueBegin)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_set_epilogue_begin);
                    }

                    // DW_LNS_set_isa
                    if (isaChanged)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_set_isa);
                        writer.WriteULEB128(debugLine.Isa);
                    }

                    // DW_LNE_set_discriminator
                    if (isDiscriminatorChanged)
                    {
                        writer.WriteU8(0);
                        writer.WriteULEB128(1 + DwarfHelper.SizeOfULEB128(debugLine.Discriminator));
                        writer.WriteU8(DwarfNative.DW_LNE_set_discriminator);
                        writer.WriteULEB128(debugLine.Discriminator);
                    }

                    // DW_LNS_negate_stmt
                    if (isStatementChanged)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_negate_stmt);
                    }

                    bool isEndOfSequence = lineIndex + 1 == lines.Count;
                    bool canEncodeSpecial = !isEndOfSequence;
                    bool canEncodeLineInSpecialCode = canEncodeSpecial && deltaLine >= LineBase && deltaLine < LineBase + LineRange;

                    bool operationAdvancedEncoded = false;

                    // Pre-encode address if necessary
                    if (!hasSetAddress)
                    {
                        writer.WriteU8(0);
                        writer.WriteULEB128(1 + DwarfHelper.SizeOfUInt(writer.AddressSize));
                        writer.WriteU8(DwarfNative.DW_LNE_set_address);
                        writer.WriteAddress(DwarfRelocationTarget.Code, debugLine.Address);
                        operationAdvancedEncoded = true;
                        deltaAddress = 0;
                        hasSetAddress = true;
                    }

                    // DW_LNS_advance_line
                    // In case we can't encode the line advance via special code
                    if (!canEncodeLineInSpecialCode)
                    {
                        if (deltaLine != 0)
                        {
                            writer.WriteU8(DwarfNative.DW_LNS_advance_line);
                            writer.WriteILEB128(deltaLine);
                            deltaLine = 0;
                        }
                    }


                    if (deltaAddress > maxDeltaAddressPerSpecialCode && deltaAddress <= (2U * maxDeltaAddressPerSpecialCode))
                    {
                        ulong deltaAddressSpecialOpCode255;

                        if (Version >= 4)
                        {
                            deltaAddressSpecialOpCode255 = (((ulong) previousLineState.OperationIndex + maxOperationAdvance) / MaximumOperationsPerInstruction);
                            deltaOperationIndex = debugLine.OperationIndex - (byte) ((previousLineState.OperationIndex + maxOperationAdvance) % MaximumOperationsPerInstruction);
                        }
                        else
                        {
                            deltaAddressSpecialOpCode255 = maxOperationAdvance;
                            deltaOperationIndex = 0;
                        }

                        Debug.Assert(deltaAddressSpecialOpCode255 * MinimumInstructionLength < deltaAddress);
                        deltaAddress -= deltaAddressSpecialOpCode255 * MinimumInstructionLength;

                        writer.WriteU8(DwarfNative.DW_LNS_const_add_pc);
                    }

                    var operation_advance = deltaAddress * MaximumOperationsPerInstruction / MinimumInstructionLength + debugLine.OperationIndex;

                    bool canEncodeAddressInSpecialCode = false;
                    ulong opcode = 256;
                    if (canEncodeSpecial && (operation_advance > 0 || deltaOperationIndex != 0 || deltaLine != 0))
                    {
                        opcode = operation_advance * LineRange + opCodeBase + (ulong) (deltaLine - LineBase);
                        if (opcode > 255)
                        {
                            if (deltaLine != 0)
                            {
                                opcode = opCodeBase + (ulong) (deltaLine - LineBase);
                            }
                        }
                        else
                        {
                            canEncodeAddressInSpecialCode = true;
                        }
                    }

                    if (!operationAdvancedEncoded && !canEncodeAddressInSpecialCode)
                    {
                        if (deltaAddress > 0 || deltaOperationIndex != 0)
                        {
                            writer.WriteU8(DwarfNative.DW_LNS_advance_pc);
                            writer.WriteULEB128(operation_advance);
                        }
                    }

                    // Special opcode
                    if (opcode <= 255)
                    {
                        writer.WriteU8((byte) opcode);
                        debugLineState.SpecialReset();
                        hasGeneratedRow = true;
                    }

                    if (isEndOfSequence)
                    {
                        writer.WriteU8(0);
                        writer.WriteULEB128(1);
                        writer.WriteU8(DwarfNative.DW_LNE_end_sequence);

                        hasGeneratedRow = true;

                        hasSetAddress = false;
                        previousLineState = initialState;
                        previousLineState.Reset(firstFile, true);
                    }
                    else
                    {
                        previousLineState = debugLineState;
                    }

                    if (!hasGeneratedRow)
                    {
                        writer.WriteU8(DwarfNative.DW_LNS_copy);
                    }

                    Debug.Assert(debugLine.Size == writer.Offset - debugLine.Offset, $"Expected Debug Line Size: {debugLine.Size} != Written Size: {writer.Offset - debugLine.Offset}");
                }
            }
        }

        private void LayoutDebugLineOpCodes(ref ulong sizeOf, uint opCodeBase)
        {
            var previousLineState = new DwarfLineState();
            var firstFile = FileNames.Count > 0 ? FileNames[0] : null;
            previousLineState.Reset(firstFile, true);
            var initialState = previousLineState;

            uint maxDeltaAddressPerSpecialCode;
            byte maxOperationAdvance = (byte)((255 - OpCodeBase) / LineRange);
            if (Version >= 4)
            {
                maxDeltaAddressPerSpecialCode = (uint)maxOperationAdvance / MaximumOperationsPerInstruction;
            }
            else
            {
                maxDeltaAddressPerSpecialCode = maxOperationAdvance;
            }
            maxDeltaAddressPerSpecialCode *= MinimumInstructionLength;

            bool hasSetAddress;

            foreach (var lineSequence in _lineSequences)
            {
                var lines = lineSequence.Lines;

                lineSequence.Offset = Offset + sizeOf;
                hasSetAddress = false;

                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var debugLine = lines[lineIndex];
                    ulong deltaAddress;
                    int deltaOperationIndex;
                    bool fileNameChanged;
                    int deltaLine;
                    int deltaColumn;
                    bool isStatementChanged;
                    bool isBasicBlockChanged;
                    bool isEndSequenceChanged;
                    bool isPrologueEndChanged;
                    bool isEpilogueBeginChanged;
                    bool isaChanged;
                    bool isDiscriminatorChanged;

                    bool hasGeneratedRow = false;

                    var debugLineState = debugLine.ToState();

                    previousLineState.Delta(debugLineState, out deltaAddress,
                        out deltaOperationIndex,
                        out fileNameChanged,
                        out deltaLine,
                        out deltaColumn,
                        out isStatementChanged,
                        out isBasicBlockChanged,
                        out isEndSequenceChanged,
                        out isPrologueEndChanged,
                        out isEpilogueBeginChanged,
                        out isaChanged,
                        out isDiscriminatorChanged);

                    debugLine.Offset = Offset + sizeOf;

                    // DW_LNS_set_column
                    if (deltaColumn != 0)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_column);
                        sizeOf += DwarfHelper.SizeOfULEB128(debugLine.Column); //writer.WriteLEB128(debugLine.Column));
                    }

                    // DW_LNS_set_file or DW_LNE_define_file
                    if (fileNameChanged)
                    {
                        var fileName = debugLine.File;

                        // DW_LNS_set_file
                        if (_fileNameToIndex.TryGetValue(fileName, out var fileIndex))
                        {
                            sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_file);
                            sizeOf += DwarfHelper.SizeOfULEB128(fileIndex); // writer.WriteLEB128(fileIndex);
                        }
                        else
                        {
                            // DW_LNE_define_file
                            sizeOf += 1; // writer.WriteU8(0);
                            uint dirIndex = fileName.Directory != null && _directoryNameToIndex.ContainsKey(fileName.Directory) ? _directoryNameToIndex[fileName.Directory] : 0;

                            ulong sizeOfInlineFileName = 1;
                            sizeOfInlineFileName += (ulong) Encoding.UTF8.GetByteCount(fileName.Name) + 1;
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(dirIndex);
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(fileName.Time);
                            sizeOfInlineFileName += DwarfHelper.SizeOfULEB128(fileName.Size);

                            sizeOf += DwarfHelper.SizeOfULEB128(sizeOfInlineFileName);
                            sizeOf += sizeOfInlineFileName;
                        }
                    }

                    // DW_LNS_copy
                    if (isBasicBlockChanged && !debugLine.IsBasicBlock ||
                        isPrologueEndChanged && !debugLine.IsPrologueEnd ||
                        isEpilogueBeginChanged && !debugLine.IsEpilogueBegin)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_copy);
                        isDiscriminatorChanged = debugLine.Discriminator != 0;
                        hasGeneratedRow = true;
                    }

                    // DW_LNS_set_basic_block
                    if (isBasicBlockChanged && debugLine.IsBasicBlock)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_basic_block);
                    }

                    // DW_LNS_set_prologue_end
                    if (isPrologueEndChanged && debugLine.IsPrologueEnd)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_prologue_end);
                    }

                    // DW_LNS_set_epilogue_begin
                    if (isEpilogueBeginChanged && debugLine.IsEpilogueBegin)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_epilogue_begin);
                    }

                    // DW_LNS_set_isa
                    if (isaChanged)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_set_isa);
                        sizeOf += DwarfHelper.SizeOfULEB128(debugLine.Isa); // writer.WriteLEB128(debugLine.Isa);
                    }

                    // DW_LNE_set_discriminator
                    if (isDiscriminatorChanged)
                    {
                        sizeOf += 1; // writer.WriteU8(0);
                        var sizeOfDiscriminator = DwarfHelper.SizeOfULEB128(debugLine.Discriminator);
                        sizeOf += DwarfHelper.SizeOfULEB128(1 + sizeOfDiscriminator); // writer.WriteLEB128(1 + DwarfHelper.SizeOfLEB128(debugLine.Discriminator));
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNE_set_discriminator);
                        sizeOf += sizeOfDiscriminator; // writer.WriteLEB128(debugLine.Discriminator);
                    }

                    // DW_LNS_negate_stmt
                    if (isStatementChanged)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_negate_stmt);
                    }

                    bool isEndOfSequence = lineIndex + 1 == lines.Count;
                    bool canEncodeSpecial = !isEndOfSequence;
                    bool canEncodeLineInSpecialCode = canEncodeSpecial && deltaLine >= LineBase && deltaLine < LineBase + LineRange;
                    bool operationAdvancedEncoded = false;

                    if (!hasSetAddress)
                    {
                        sizeOf += 1; // writer.WriteU8(0);
                        var sizeOfAddress = DwarfHelper.SizeOfUInt(AddressSize);
                        sizeOf += DwarfHelper.SizeOfULEB128(1 + sizeOfAddress); // writer.WriteLEB128(DwarfHelper.SizeOfNativeInt(writer.IsTargetAddress64Bit));
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNE_set_address);
                        sizeOf += sizeOfAddress; // writer.WriteLEB128(debugLine.Address);
                        operationAdvancedEncoded = true;
                        deltaAddress = 0;
                        hasSetAddress = true;
                    }
                    else if (deltaAddress > maxDeltaAddressPerSpecialCode && deltaAddress <= (2U * maxDeltaAddressPerSpecialCode))
                    {
                        ulong deltaAddressSpecialOpCode255;

                        if (Version >= 4)
                        {
                            deltaAddressSpecialOpCode255 = (((ulong) previousLineState.OperationIndex + maxOperationAdvance) / MaximumOperationsPerInstruction);
                            deltaOperationIndex = debugLine.OperationIndex - (byte) ((previousLineState.OperationIndex + maxOperationAdvance) % MaximumOperationsPerInstruction);
                        }
                        else
                        {
                            deltaAddressSpecialOpCode255 = maxOperationAdvance;
                            deltaOperationIndex = 0;
                        }

                        Debug.Assert(deltaAddressSpecialOpCode255 * MinimumInstructionLength < deltaAddress);
                        deltaAddress -= deltaAddressSpecialOpCode255 * MinimumInstructionLength;

                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_const_add_pc);
                    }

                    // DW_LNS_advance_line
                    if (!canEncodeLineInSpecialCode)
                    {
                        if (deltaLine != 0)
                        {
                            sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_advance_line);
                            sizeOf += DwarfHelper.SizeOfILEB128(deltaLine); // writer.WriteSignedLEB128(deltaLine);
                            deltaLine = 0;
                        }
                    }

                    var operation_advance = deltaAddress * MaximumOperationsPerInstruction / MinimumInstructionLength + debugLine.OperationIndex;

                    bool canEncodeAddress = false;
                    ulong opcode = 256;
                    if (canEncodeSpecial && (operation_advance > 0 || deltaOperationIndex > 0 || deltaLine != 0))
                    {
                        opcode = operation_advance * LineRange + opCodeBase + (ulong) (deltaLine - LineBase);
                        if (opcode > 255)
                        {
                            if (deltaLine != 0)
                            {
                                opcode = opCodeBase + (ulong) (deltaLine - LineBase);
                            }
                        }
                        else
                        {
                            canEncodeAddress = true;
                        }
                    }

                    if (!operationAdvancedEncoded && !canEncodeAddress)
                    {
                        if (deltaAddress > 0 || deltaOperationIndex > 0)
                        {
                            sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_advance_pc);
                            sizeOf += DwarfHelper.SizeOfULEB128(operation_advance); // writer.WriteLEB128(operation_advance);
                        }
                    }

                    // Special opcode
                    if (opcode <= 255)
                    {
                        sizeOf += 1; // writer.WriteU8((byte)opcode);
                        debugLineState.SpecialReset();
                        hasGeneratedRow = true;
                    }

                    if (isEndOfSequence)
                    {
                        sizeOf += 3; // writer.WriteU8(0);
                        // writer.WriteLEB128(1);
                        // writer.WriteU8(DwarfNative.DW_LNE_end_sequence);
                        previousLineState = initialState;
                        previousLineState.Reset(firstFile, true);
                        hasGeneratedRow = true;
                        hasSetAddress = false;
                    }
                    else
                    {
                        previousLineState = debugLineState;
                    }

                    if (!hasGeneratedRow)
                    {
                        sizeOf += 1; // writer.WriteU8(DwarfNative.DW_LNS_copy);
                    }

                    debugLine.Size = Offset + sizeOf - debugLine.Offset;
                }
                lineSequence.Size = Offset + sizeOf - lineSequence.Offset;
            }
        }
        
        private static ReadOnlySpan<byte> DefaultStandardOpCodeLengths => new ReadOnlySpan<byte>(new byte[12]
        {
            0, // DwarfNative.DW_LNS_copy
            1, // DwarfNative.DW_LNS_advance_pc
            1, // DwarfNative.DW_LNS_advance_line
            1, // DwarfNative.DW_LNS_set_file
            1, // DwarfNative.DW_LNS_set_column
            0, // DwarfNative.DW_LNS_negate_stmt
            0, // DwarfNative.DW_LNS_set_basic_block
            0, // DwarfNative.DW_LNS_const_add_pc
            1, // DwarfNative.DW_LNS_fixed_advance_pc
            0, // DwarfNative.DW_LNS_set_prologue_end
            0, // DwarfNative.DW_LNS_set_epilogue_begin
            1, // DwarfNative.DW_LNS_set_isa
        });

        public override string ToString()
        {
            return $"Section .debug_line, {nameof(Version)}: {Version}, {nameof(Is64BitEncoding)}: {Is64BitEncoding}, {nameof(FileNames)}: {FileNames.Count}, {nameof(LineSequences)}: {LineSequences.Count}";
        }
    }
}