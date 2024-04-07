// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfBuilder : ITypesDebugInfoWriter
    {
        private record struct SectionInfo(string SectionSymbolName, ulong Size);
        private record struct MemberFunctionTypeInfo(MemberFunctionTypeDescriptor MemberDescriptor, uint[] ArgumentTypes, bool IsStatic);
        public delegate (string SectionSymbolName, long Address) ResolveStaticVariable(string name);

        private readonly NameMangler _nameMangler;
        private readonly TargetArchitecture _architecture;
        private readonly byte _targetPointerSize;
        private readonly bool _useDwarf5;
        private readonly int _frameRegister;
        private readonly byte _minimumInstructionLength;
        private readonly RelocType _codeRelocType;

        private readonly Dictionary<int, DwarfLineSequenceWriter> _lineSequences = new();
        private readonly Dictionary<string, int> _fileNameMap = new(); // fileName -> _fileNames index (1-based)
        private readonly List<DwarfFileName> _fileNames = new();

        private readonly List<SectionInfo> _sections = new();

        private readonly List<MemberFunctionTypeInfo> _memberFunctionTypeInfos = new();
        private readonly List<DwarfMemberFunction> _memberFunctions = new();
        private readonly Dictionary<TypeFlags, uint> _primitiveDwarfTypes = new();
        private readonly Dictionary<(uint, uint), uint> _simpleArrayDwarfTypes = new(); // (elementTypeIndex, size) -> arrayTypeIndex
        private readonly List<DwarfStaticVariableInfo> _staticFields = new();

        private readonly List<DwarfInfo> _dwarfTypes = new();
        private uint[] _dwarfTypeOffsets;
        private readonly List<DwarfInfo> _dwarfSubprograms = new();

        public DwarfBuilder(
            NameMangler nameMangler,
            TargetDetails target,
            bool useDwarf5)
        {
            _nameMangler = nameMangler;
            _architecture = target.Architecture;
            _useDwarf5 = useDwarf5;
            _minimumInstructionLength = (byte)target.MinimumCodeAlignment;

            switch (target.Architecture)
            {
                case TargetArchitecture.ARM64:
                    _targetPointerSize = 8;
                    _frameRegister = 29; // FP
                    _codeRelocType = RelocType.IMAGE_REL_BASED_DIR64;
                    break;

                case TargetArchitecture.ARM:
                    _targetPointerSize = 4;
                    _frameRegister = 7; // R7
                    _codeRelocType = RelocType.IMAGE_REL_BASED_HIGHLOW;
                    break;

                case TargetArchitecture.X64:
                    _targetPointerSize = 8;
                    _frameRegister = 6; // RBP
                    _codeRelocType = RelocType.IMAGE_REL_BASED_DIR64;
                    break;

                case TargetArchitecture.X86:
                    _targetPointerSize = 4;
                    _frameRegister = 5; // EBP
                    _codeRelocType = RelocType.IMAGE_REL_BASED_HIGHLOW;
                    break;

                default:
                    throw new NotSupportedException("Unsupported architecture");
            }
        }

        public TargetArchitecture TargetArchitecture => _architecture;
        public byte TargetPointerSize => _targetPointerSize;
        public int FrameRegister => _frameRegister;

        public uint ResolveOffset(uint typeIndex) => typeIndex == 0 ? 0u : _dwarfTypeOffsets[typeIndex - 1];

        public void Write(
            SectionWriter infoSectionWriter,
            SectionWriter stringSectionWriter,
            SectionWriter abbrevSectionWriter,
            SectionWriter locSectionWriter,
            SectionWriter rangeSectionWriter,
            SectionWriter lineSectionWriter,
            SectionWriter arangeSectionWriter,
            ResolveStaticVariable resolveStaticVariable)
        {
            WriteInfoTable(
                infoSectionWriter,
                stringSectionWriter,
                abbrevSectionWriter,
                locSectionWriter,
                rangeSectionWriter,
                resolveStaticVariable);
            WriteLineInfoTable(lineSectionWriter);
            WriteAddressRangeTable(arangeSectionWriter);
        }

        public void WriteInfoTable(
            SectionWriter infoSectionWriter,
            SectionWriter stringSectionWriter,
            SectionWriter abbrevSectionWriter,
            SectionWriter locSectionWriter,
            SectionWriter rangeSectionWriter,
            ResolveStaticVariable resolveStaticVariable)
        {
            // Length
            byte[] sizeBuffer = new byte[sizeof(uint)];
            infoSectionWriter.EmitData(sizeBuffer);
            // Version
            infoSectionWriter.WriteLittleEndian<ushort>((ushort)(_useDwarf5 ? 5u : 4u));
            if (_useDwarf5)
            {
                // Unit type, Address Size
                infoSectionWriter.Write([DW_UT_compile, _targetPointerSize]);
                // Abbrev offset
                infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_abbrev", 0);
            }
            else
            {
                // Abbrev offset
                infoSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_abbrev", 0);
                // Address Size
                infoSectionWriter.Write([_targetPointerSize]);
            }

            using (DwarfInfoWriter dwarfInfoWriter = new(
                infoSectionWriter,
                stringSectionWriter,
                abbrevSectionWriter,
                locSectionWriter,
                rangeSectionWriter,
                this,
                _codeRelocType))
            {
                dwarfInfoWriter.WriteStartDIE(DwarfAbbrev.CompileUnit);

                // DW_AT_producer
                dwarfInfoWriter.WriteStringReference("NetRuntime");
                // DW_AT_language
                dwarfInfoWriter.WriteUInt16(DW_LANG_C_plus_plus);
                // DW_AT_name
                dwarfInfoWriter.WriteStringReference("il.cpp");
                // DW_AT_comp_dir
                dwarfInfoWriter.WriteStringReference("/_");
                // DW_AT_low_pc
                dwarfInfoWriter.WriteAddressSize(0);
                // DW_AT_ranges
                dwarfInfoWriter.WriteStartRangeList();
                foreach (var sectionInfo in _sections)
                {
                    dwarfInfoWriter.WriteRangeListEntry(sectionInfo.SectionSymbolName, 0, (uint)sectionInfo.Size);
                }
                dwarfInfoWriter.WriteEndRangeList();
                // DW_AT_stmt_list
                dwarfInfoWriter.WriteLineReference(0);

                _dwarfTypeOffsets = new uint[_dwarfTypes.Count];

                int typeIndex = 0;
                foreach (DwarfInfo type in _dwarfTypes)
                {
                    _dwarfTypeOffsets[typeIndex] = (uint)dwarfInfoWriter.Position;
                    type.Dump(dwarfInfoWriter);
                    typeIndex++;
                }

                foreach (DwarfInfo subprogram in _dwarfSubprograms)
                {
                    subprogram.Dump(dwarfInfoWriter);
                }

                foreach (DwarfStaticVariableInfo staticField in _staticFields)
                {
                    (string sectionSymbolName, long address) = resolveStaticVariable(staticField.Name);
                    if (sectionSymbolName is not null)
                    {
                        staticField.Dump(dwarfInfoWriter, sectionSymbolName, address);
                    }
                }

                dwarfInfoWriter.WriteEndDIE();
            }

            // End of compile unit
            infoSectionWriter.WriteByte(0);

            // Update the size
            BinaryPrimitives.WriteUInt32LittleEndian(sizeBuffer, (uint)(infoSectionWriter.Position - sizeof(uint)));
        }

        private void WriteLineInfoTable(SectionWriter lineSectionWriter)
        {
            using (var lineProgramTableWriter = new DwarfLineProgramTableWriter(
                lineSectionWriter,
                _fileNames,
                _targetPointerSize,
                _minimumInstructionLength,
                _codeRelocType))
            {
                foreach (DwarfLineSequenceWriter lineSequence in _lineSequences.Values)
                {
                    lineProgramTableWriter.WriteLineSequence(lineSequence);
                }
            }
        }

        private void WriteAddressRangeTable(SectionWriter arangeSectionWriter)
        {
            // Length
            var sizeBuffer = new byte[sizeof(uint)];
            arangeSectionWriter.EmitData(sizeBuffer);
            // Version
            arangeSectionWriter.WriteLittleEndian<ushort>(2);
            // Debug Info Offset
            arangeSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, ".debug_info", 0);
            // Address size, Segment selector size
            arangeSectionWriter.Write([_targetPointerSize, 0]);
            // Ranges have to be aligned
            arangeSectionWriter.EmitAlignment(_targetPointerSize * 2);
            foreach (var sectionInfo in _sections)
            {
                arangeSectionWriter.EmitSymbolReference(_codeRelocType, sectionInfo.SectionSymbolName, 0);
                switch (_targetPointerSize)
                {
                    case 8: arangeSectionWriter.WriteLittleEndian<ulong>(sectionInfo.Size); break;
                    case 4: arangeSectionWriter.WriteLittleEndian<uint>((uint)sectionInfo.Size); break;
                    default: throw new NotSupportedException();
                }
            }
            arangeSectionWriter.WritePadding(_targetPointerSize * 2);
            // Update the size
            BinaryPrimitives.WriteUInt32LittleEndian(sizeBuffer, (uint)(arangeSectionWriter.Position - sizeof(uint)));
        }

        public uint GetPrimitiveTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive, "it is not a primitive type");
            return GetPrimitiveTypeIndex(type.Category);
        }

        private uint GetPrimitiveTypeIndex(TypeFlags typeFlags)
        {
            if (_primitiveDwarfTypes.TryGetValue(typeFlags, out uint index))
            {
                return index;
            }

            _dwarfTypes.Add(new DwarfPrimitiveTypeInfo(typeFlags, _targetPointerSize));
            uint typeIndex = (uint)_dwarfTypes.Count;
            _primitiveDwarfTypes.Add(typeFlags, typeIndex);
            return typeIndex;
        }

        public uint GetPointerTypeIndex(PointerTypeDescriptor pointerDescriptor)
        {
            uint voidTypeIndex = GetPrimitiveTypeIndex(TypeFlags.Void);

            // Creating a pointer to what DWARF considers Void type (DW_TAG_unspecified_type -
            // per http://eagercon.com/dwarf/issues/minutes-001017.htm) leads to unhappiness
            // since debuggers don't really know how to handle that. The Clang symbol parser
            // in LLDB only handles DW_TAG_unspecified_type if it's named
            // "nullptr_t" or "decltype(nullptr)".
            //
            // We resort to this kludge to generate the exact same debug info for void* that
            // clang would generate (pointer type with no element type specified).
            if (pointerDescriptor.ElementType == voidTypeIndex)
            {
                _dwarfTypes.Add(new DwarfVoidPtrTypeInfo());
            }
            else
            {
                _dwarfTypes.Add(new DwarfPointerTypeInfo(pointerDescriptor));
            }

            return (uint)_dwarfTypes.Count;
        }

        private uint GetSimpleArrayTypeIndex(uint elementIndex, uint size)
        {
            if (_simpleArrayDwarfTypes.TryGetValue((elementIndex, size), out uint index))
            {
                return index;
            }

            _dwarfTypes.Add(new DwarfSimpleArrayTypeInfo(elementIndex, size));
            uint typeIndex = (uint)_dwarfTypes.Count;
            _simpleArrayDwarfTypes.Add((elementIndex, size), typeIndex);

            return typeIndex;
        }

        public uint GetArrayTypeIndex(
            ClassTypeDescriptor classDescriptor,
            ArrayTypeDescriptor arrayDescriptor)
        {
            // Create corresponding class info
            ClassTypeDescriptor arrayClassDescriptor = classDescriptor;

            List<DataFieldDescriptor> fieldDescriptors = new();
            ulong fieldOffset = _targetPointerSize;

            fieldDescriptors.Add(new DataFieldDescriptor
            {
                FieldTypeIndex = GetPrimitiveTypeIndex(TypeFlags.Int32),
                Offset = fieldOffset,
                Name = "m_NumComponents",
            });
            fieldOffset += _targetPointerSize;

            if (arrayDescriptor.IsMultiDimensional != 0)
            {
                fieldDescriptors.Add(new DataFieldDescriptor
                {
                    FieldTypeIndex = GetSimpleArrayTypeIndex(GetPrimitiveTypeIndex(TypeFlags.Int32), arrayDescriptor.Rank),
                    Offset = fieldOffset,
                    Name = "m_Bounds",
                });
                fieldOffset += 2u * 4u * (ulong)arrayDescriptor.Rank;
            }

            fieldDescriptors.Add(new DataFieldDescriptor
            {
                FieldTypeIndex = GetSimpleArrayTypeIndex(arrayDescriptor.ElementType, 0),
                Offset = fieldOffset,
                Name = "m_Data",
            });

            // We currently don't encode the size of the variable length data. The DWARF5
            // specification allows encoding variable length arrays through DW_AT_lower_bound,
            // DW_AT_upper_bound, and DW_AT_count expressions. There's potentially room
            // to improve the debugging information by a more substential restructuring.
            arrayClassDescriptor.InstanceSize = fieldOffset;

            ClassFieldsTypeDescriptor fieldsTypeDesc = new ClassFieldsTypeDescriptor
            {
                Size = _targetPointerSize,
                FieldsCount = arrayDescriptor.IsMultiDimensional != 0 ? 3 : 2,
            };

            return GetCompleteClassTypeIndex(arrayClassDescriptor, fieldsTypeDesc, fieldDescriptors.ToArray(), null);
        }

        public uint GetEnumTypeIndex(
            EnumTypeDescriptor typeDescriptor,
            EnumRecordTypeDescriptor[] typeRecords)
        {
            byte byteSize = ((DwarfPrimitiveTypeInfo)_dwarfTypes[(int)typeDescriptor.ElementType - 1]).ByteSize;
            _dwarfTypes.Add(new DwarfEnumTypeInfo(typeDescriptor, typeRecords, byteSize));
            return (uint)_dwarfTypes.Count;
        }

        public uint GetClassTypeIndex(ClassTypeDescriptor classDescriptor)
        {
            _dwarfTypes.Add(new DwarfClassTypeInfo(classDescriptor));
            return (uint)_dwarfTypes.Count;
        }

        public uint GetCompleteClassTypeIndex(
            ClassTypeDescriptor classTypeDescriptor,
            ClassFieldsTypeDescriptor classFieldsTypeDescriptor,
            DataFieldDescriptor[] fields,
            StaticDataFieldDescriptor[] statics)
        {
            var classInfo = new DwarfClassTypeInfo(classTypeDescriptor, classFieldsTypeDescriptor, fields, statics);
            _dwarfTypes.Add(classInfo);
            _staticFields.AddRange(classInfo.StaticVariables);
            return (uint)_dwarfTypes.Count;
        }

        public uint GetMemberFunctionTypeIndex(MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes)
        {
            _memberFunctionTypeInfos.Add(new MemberFunctionTypeInfo(
                memberDescriptor,
                argumentTypes,
                memberDescriptor.TypeIndexOfThisPointer == GetPrimitiveTypeIndex(TypeFlags.Void)));

            return (uint)_memberFunctionTypeInfos.Count;
        }

        public uint GetMemberFunctionId(MemberFunctionIdTypeDescriptor memberIdDescriptor)
        {
            MemberFunctionTypeInfo memberFunctionTypeInfo = _memberFunctionTypeInfos[(int)memberIdDescriptor.MemberFunction - 1];
            DwarfClassTypeInfo parentClass = (DwarfClassTypeInfo)_dwarfTypes[(int)(memberIdDescriptor.ParentClass - 1)];
            DwarfMemberFunction memberFunction = new DwarfMemberFunction(
                memberIdDescriptor.Name,
                memberFunctionTypeInfo.MemberDescriptor,
                memberFunctionTypeInfo.ArgumentTypes,
                memberFunctionTypeInfo.IsStatic);

            parentClass.AddMemberFunction(memberFunction);
            _memberFunctions.Add(memberFunction);

            return (uint)_memberFunctions.Count;
        }

        public string GetMangledName(TypeDesc type)
        {
            return _nameMangler.GetMangledTypeName(type);
        }

        public void EmitSubprogramInfo(
            string methodName,
            string sectionSymbolName,
            long methodAddress,
            int methodPCLength,
            uint methodTypeIndex,
            IEnumerable<(DebugVarInfoMetadata, uint)> debugVars,
            DebugEHClauseInfo[] debugEHClauseInfos)
        {
            if (methodTypeIndex == 0)
            {
                return;
            }

            DwarfMemberFunction memberFunction = _memberFunctions[(int)methodTypeIndex - 1];
            memberFunction.LinkageName = methodName;

            _dwarfSubprograms.Add(new DwarfSubprogramInfo(
                sectionSymbolName,
                methodAddress,
                methodPCLength,
                memberFunction,
                debugVars.ToArray(),
                debugEHClauseInfos));
        }

        public void EmitLineInfo(
            int sectionIndex,
            string sectionSymbolName,
            long methodAddress,
            IEnumerable<NativeSequencePoint> sequencePoints)
        {
            DwarfLineSequenceWriter lineSequence;

            // Create line sequence for every section so they can get the
            // base address relocated properly.
            if (!_lineSequences.TryGetValue(sectionIndex, out lineSequence))
            {
                lineSequence = new DwarfLineSequenceWriter(sectionSymbolName, _minimumInstructionLength);
                _lineSequences.Add(sectionIndex, lineSequence);
            }

            int fileNameIndex = 0;
            string lastFileName = null;

            foreach (NativeSequencePoint sequencePoint in sequencePoints)
            {
                if (lastFileName != sequencePoint.FileName)
                {
                    if (!_fileNameMap.TryGetValue(sequencePoint.FileName, out fileNameIndex))
                    {
                        var dwarfFileName = string.IsNullOrEmpty(sequencePoint.FileName) ?
                            new DwarfFileName("<stdin>", null) :
                            new DwarfFileName(Path.GetFileName(sequencePoint.FileName), Path.GetDirectoryName(sequencePoint.FileName));
                        _fileNames.Add(dwarfFileName);
                        fileNameIndex = _fileNames.Count;
                        _fileNameMap.Add(sequencePoint.FileName, fileNameIndex);
                    }
                    lastFileName = sequencePoint.FileName;
                }

                lineSequence.EmitLineInfo(fileNameIndex, methodAddress, sequencePoint);
            }
        }

        public void EmitSectionInfo(string sectionSymbolName, ulong size)
        {
            _sections.Add(new SectionInfo(sectionSymbolName, size));
        }
    }
}
