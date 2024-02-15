// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal abstract class DwarfInfo
    {
        public abstract void Dump(DwarfInfoWriter writer);
    }

    internal sealed class DwarfPrimitiveTypeInfo : DwarfInfo
    {
        private readonly TypeFlags _typeFlags;
        private readonly int _targetPointerSize;

        public DwarfPrimitiveTypeInfo(TypeFlags typeFlags, int targetPointerSize)
        {
            _typeFlags = typeFlags;
            _targetPointerSize = targetPointerSize;
        }

        public byte ByteSize => _typeFlags switch
        {
            TypeFlags.Boolean => 1,
            TypeFlags.Char => 2,
            TypeFlags.SByte => 1,
            TypeFlags.Byte => 1,
            TypeFlags.Int16 => 2,
            TypeFlags.UInt16 => 2,
            TypeFlags.Int32 => 4,
            TypeFlags.UInt32 => 4,
            TypeFlags.Int64 => 8,
            TypeFlags.UInt64 => 8,
            TypeFlags.IntPtr => (byte)_targetPointerSize,
            TypeFlags.UIntPtr => (byte)_targetPointerSize,
            TypeFlags.Single => 4,
            TypeFlags.Double => 8,
            _ => 0,
        };

        public override void Dump(DwarfInfoWriter writer)
        {
            if (_typeFlags == TypeFlags.Void)
            {
                writer.WriteStartDIE(DwarfAbbrev.VoidType);
                writer.WriteStringReference("void");
                writer.WriteEndDIE();
            }
            else
            {
                var (name, encoding, byteSize) = _typeFlags switch
                {
                    TypeFlags.Boolean => ("bool", DW_ATE_boolean, 1),
                    TypeFlags.Char => ("char16_t", DW_ATE_UTF, 2),
                    TypeFlags.SByte => ("sbyte", DW_ATE_signed, 1),
                    TypeFlags.Byte => ("byte", DW_ATE_unsigned, 1),
                    TypeFlags.Int16 => ("short", DW_ATE_signed, 2),
                    TypeFlags.UInt16 => ("ushort", DW_ATE_unsigned, 2),
                    TypeFlags.Int32 => ("int", DW_ATE_signed, 4),
                    TypeFlags.UInt32 => ("uint", DW_ATE_unsigned, 4),
                    TypeFlags.Int64 => ("long", DW_ATE_signed, 8),
                    TypeFlags.UInt64 => ("ulong", DW_ATE_unsigned, 8),
                    TypeFlags.IntPtr => ("nint", DW_ATE_signed, _targetPointerSize),
                    TypeFlags.UIntPtr => ("nuint", DW_ATE_unsigned, _targetPointerSize),
                    TypeFlags.Single => ("float", DW_ATE_float, 4),
                    TypeFlags.Double => ("double", DW_ATE_float, 8),
                    _ => ("", 0, 0),
                };

                writer.WriteStartDIE(DwarfAbbrev.BaseType);
                writer.WriteStringReference(name);
                writer.Write([(byte)encoding, (byte)byteSize]);
                writer.WriteEndDIE();
            }
        }
    }

    internal sealed class DwarfEnumTypeInfo : DwarfInfo
    {
        private readonly EnumTypeDescriptor _typeDescriptor;
        private readonly EnumRecordTypeDescriptor[] _typeRecords;
        private readonly byte _byteSize;

        public DwarfEnumTypeInfo(
            EnumTypeDescriptor typeDescriptor,
            EnumRecordTypeDescriptor[] typeRecords,
            byte byteSize)
        {
            _typeDescriptor = typeDescriptor;
            _typeRecords = typeRecords;
            _byteSize = byteSize;
        }

        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(_typeRecords.Length > 0 ? DwarfAbbrev.EnumerationType : DwarfAbbrev.EnumerationTypeNoChildren);
            writer.WriteStringReference(_typeDescriptor.Name);
            writer.WriteInfoReference(_typeDescriptor.ElementType);
            writer.Write([_byteSize]);

            if (_typeRecords.Length > 0)
            {
                DwarfAbbrev abbrev = _byteSize switch {
                    1 => DwarfAbbrev.Enumerator1,
                    2 => DwarfAbbrev.Enumerator2,
                    4 => DwarfAbbrev.Enumerator4,
                    8 => DwarfAbbrev.Enumerator8,
                    _ => throw new NotSupportedException()
                };

                foreach (EnumRecordTypeDescriptor typeRecord in _typeRecords)
                {
                    writer.WriteStartDIE(abbrev);
                    writer.WriteStringReference(typeRecord.Name);
                    switch (_byteSize)
                    {
                        case 1: writer.WriteUInt8((byte)typeRecord.Value); break;
                        case 2: writer.WriteUInt16((ushort)typeRecord.Value); break;
                        case 4: writer.WriteUInt32((uint)typeRecord.Value); break;
                        case 8: writer.WriteUInt64(typeRecord.Value); break;
                    };
                    writer.WriteEndDIE();
                }
            }

            writer.WriteEndDIE();
        }
    }

    internal sealed class DwarfPointerTypeInfo : DwarfInfo
    {
        private PointerTypeDescriptor _typeDescriptor;

        public DwarfPointerTypeInfo(PointerTypeDescriptor typeDescriptor)
        {
            _typeDescriptor = typeDescriptor;
        }

        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(_typeDescriptor.IsReference != 0 ? DwarfAbbrev.ReferenceType : DwarfAbbrev.PointerType);
            writer.WriteInfoReference(_typeDescriptor.ElementType);
            writer.Write([_typeDescriptor.Is64Bit != 0 ? (byte)8 : (byte)4]);
            writer.WriteEndDIE();
        }
    }

    internal sealed class DwarfVoidPtrTypeInfo : DwarfInfo
    {
        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(DwarfAbbrev.VoidPointerType);
            writer.WriteEndDIE();
        }
    }

    internal sealed class DwarfSimpleArrayTypeInfo : DwarfInfo
    {
        private readonly uint _elementType;
        private readonly ulong _size;

        public DwarfSimpleArrayTypeInfo(uint elementType, ulong size)
        {
            _elementType = elementType;
            _size = size;
        }

        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(DwarfAbbrev.ArrayType);

            // DW_AT_type
            writer.WriteInfoReference(_elementType);

            writer.WriteStartDIE(DwarfAbbrev.SubrangeType);
            // DW_AT_upper_bound
            // NOTE: This produces garbage for _size == 0
            writer.WriteULEB128(_size - 1);
            writer.WriteEndDIE();

            writer.WriteEndDIE();
        }
    }

    internal sealed class DwarfMemberFunction
    {
        public string Name { get; private set; }
        public string LinkageName { get; set; }
        public MemberFunctionTypeDescriptor Descriptor { get; private set; }
        public uint[] ArgumentTypes { get; private set; }
        public bool IsStatic { get; private set; }
        public long InfoOffset { get; set; }

        public DwarfMemberFunction(
            string name,
            MemberFunctionTypeDescriptor descriptor,
            uint[] argumentTypes,
            bool isStatic)
        {
            Name = name;
            LinkageName = name;
            Descriptor = descriptor;
            ArgumentTypes = argumentTypes;
            IsStatic = isStatic;
        }
    }

    internal sealed class DwarfClassTypeInfo : DwarfInfo
    {
        private readonly bool _isForwardDecl;
        private readonly ClassTypeDescriptor _typeDescriptor;
        private readonly ClassFieldsTypeDescriptor _classFieldsTypeDescriptor;
        private readonly DataFieldDescriptor[] _fields;
        private readonly List<DwarfStaticVariableInfo> _statics;
        private List<DwarfMemberFunction> _methods;

        public DwarfClassTypeInfo(ClassTypeDescriptor typeDescriptor)
        {
            _isForwardDecl = true;
            _typeDescriptor = typeDescriptor;
        }

        public DwarfClassTypeInfo(
            ClassTypeDescriptor typeDescriptor,
            ClassFieldsTypeDescriptor classFieldsTypeDescriptor,
            DataFieldDescriptor[] fields,
            StaticDataFieldDescriptor[] statics)
        {
            _typeDescriptor = typeDescriptor;
            _classFieldsTypeDescriptor = classFieldsTypeDescriptor;
            _fields = fields;
            if (statics is not null)
            {
                _statics = new(statics.Length);
                foreach (StaticDataFieldDescriptor staticDescriptor in statics)
                {
                    _statics.Add(new DwarfStaticVariableInfo(staticDescriptor));
                }
            }
        }

        public void AddMemberFunction(DwarfMemberFunction memberFunction)
        {
            _methods ??= new();
            _methods.Add(memberFunction);
        }

        private bool HasChildren =>
            _typeDescriptor.BaseClassId != 0 ||
            _fields.Length > 0 ||
            _methods is not null;

        public IReadOnlyList<DwarfStaticVariableInfo> StaticVariables => _statics ?? [];

        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(
                _isForwardDecl ? DwarfAbbrev.ClassTypeDecl :
                HasChildren ? DwarfAbbrev.ClassType : DwarfAbbrev.ClassTypeNoChildren);

            // DW_AT_name
            writer.WriteStringReference(_typeDescriptor.Name);

            if (!_isForwardDecl)
            {
                // DW_AT_byte_size
                writer.WriteUInt32((uint)_typeDescriptor.InstanceSize);

                if (_typeDescriptor.BaseClassId != 0)
                {
                    writer.WriteStartDIE(DwarfAbbrev.ClassInheritance);
                    // DW_AT_type
                    writer.WriteInfoReference(_typeDescriptor.BaseClassId);
                    // DW_AT_data_member_location
                    writer.Write([0]);
                    writer.WriteEndDIE();
                }

                int staticIndex = 0;
                foreach (DataFieldDescriptor fieldDescriptor in _fields)
                {
                    if (fieldDescriptor.Offset != 0xFFFFFFFFu)
                    {
                        writer.WriteStartDIE(DwarfAbbrev.ClassMember);
                        // DW_AT_name
                        writer.WriteStringReference(fieldDescriptor.Name);
                        // DW_AT_type
                        writer.WriteInfoReference(fieldDescriptor.FieldTypeIndex);
                        // DW_AT_data_member_location
                        writer.WriteUInt32((uint)fieldDescriptor.Offset);
                        writer.WriteEndDIE();
                    }
                    else
                    {
                        _statics[staticIndex].InfoOffset = writer.Position;
                        writer.WriteStartDIE(DwarfAbbrev.ClassMemberStatic);
                        // DW_AT_name
                        writer.WriteStringReference(fieldDescriptor.Name);
                        // DW_AT_type
                        writer.WriteInfoReference(fieldDescriptor.FieldTypeIndex);
                        staticIndex++;
                        writer.WriteEndDIE();
                    }
                }

                if (_methods is not null)
                {
                    foreach (DwarfMemberFunction method in _methods)
                    {
                        method.InfoOffset = writer.Position;
                        writer.WriteStartDIE(
                            !method.IsStatic ? DwarfAbbrev.SubprogramSpec :
                            method.ArgumentTypes.Length > 0 ? DwarfAbbrev.SubprogramStaticSpec : DwarfAbbrev.SubprogramStaticNoChildrenSpec);
                        // DW_AT_name
                        writer.WriteStringReference(method.Name);
                        // DW_AT_linkage_name
                        writer.WriteStringReference(method.LinkageName);
                        // DW_AT_decl_file, DW_AT_decl_line
                        writer.Write([1, 1]);
                        // DW_AT_type
                        writer.WriteInfoReference(method.Descriptor.ReturnType);

                        if (!method.IsStatic)
                        {
                            // DW_AT_object_pointer
                            writer.WriteInfoAbsReference(writer.Position + sizeof(uint));

                            writer.WriteStartDIE(DwarfAbbrev.FormalParameterThisSpec);
                            // DW_AT_type
                            writer.WriteInfoReference(method.Descriptor.TypeIndexOfThisPointer);
                            writer.WriteEndDIE();
                        }

                        foreach (uint argumentType in method.ArgumentTypes)
                        {
                            writer.WriteStartDIE(DwarfAbbrev.FormalParameterSpec);
                            // DW_AT_type
                            writer.WriteInfoReference(argumentType);
                            writer.WriteEndDIE();
                        }

                        writer.WriteEndDIE();
                    }
                }
            }

            writer.WriteEndDIE();
        }
    }

    internal sealed class DwarfSubprogramInfo : DwarfInfo
    {
        private readonly string _sectionSymbolName;
        private readonly long _methodAddress;
        private readonly int _methodSize;
        private readonly DwarfMemberFunction _memberFunction;
        private readonly IEnumerable<(DebugVarInfoMetadata, uint)> _debugVars;
        private readonly IEnumerable<DebugEHClauseInfo> _debugEHClauseInfos;
        private readonly bool _isStatic;
        private readonly bool _hasChildren;

        public DwarfSubprogramInfo(
            string sectionSymbolName,
            long methodAddress,
            int methodSize,
            DwarfMemberFunction memberFunction,
            IEnumerable<(DebugVarInfoMetadata, uint)> debugVars,
            IEnumerable<DebugEHClauseInfo> debugEHClauseInfos)
        {
            _sectionSymbolName = sectionSymbolName;
            _methodAddress = methodAddress;
            _methodSize = methodSize;
            _memberFunction = memberFunction;
            _isStatic = memberFunction.IsStatic;
            _hasChildren = !_isStatic || debugVars.Any() || debugEHClauseInfos.Any();
            _debugVars = debugVars;
            _debugEHClauseInfos = debugEHClauseInfos;
        }

        public override void Dump(DwarfInfoWriter writer)
        {
            writer.WriteStartDIE(
                !_isStatic ? DwarfAbbrev.Subprogram :
                _hasChildren ? DwarfAbbrev.SubprogramStatic : DwarfAbbrev.SubprogramStaticNoChildren);

            // DW_AT_specification
            writer.WriteInfoAbsReference(_memberFunction.InfoOffset);

            // DW_AT_low_pc
            writer.WriteCodeReference(_sectionSymbolName, _methodAddress);

            // DW_AT_high_pc
            writer.WriteAddressSize((ulong)_methodSize);

            // DW_AT_frame_base
            writer.WriteULEB128(1);
            writer.Write([(byte)(DW_OP_reg0 + writer.FrameRegister)]);

            if (!_isStatic)
            {
                // DW_AT_object_pointer
                writer.WriteInfoAbsReference(writer.Position + sizeof(uint));
            }

            /// At the moment, the lexical scope reflects IL, not C#, meaning that
            /// there is only one scope for the whole method. We could be more precise
            /// in the future by pulling the scope information from the PDB.
            foreach ((DebugVarInfoMetadata debugVar, uint typeIndex) in _debugVars)
            {
                bool isThis = debugVar.IsParameter && debugVar.DebugVarInfo.VarNumber == 0 && !_isStatic;
                DumpVar(writer, debugVar, typeIndex, isThis);
            }

            // EH clauses
            foreach (DebugEHClauseInfo clause in _debugEHClauseInfos)
            {
                writer.WriteStartDIE(DwarfAbbrev.TryBlock);
                // DW_AT_low_pc
                writer.WriteCodeReference(_sectionSymbolName, _methodAddress + clause.TryOffset);
                // DW_AT_high_pc
                writer.WriteAddressSize(clause.TryLength);
                writer.WriteEndDIE();

                writer.WriteStartDIE(DwarfAbbrev.CatchBlock);
                // DW_AT_low_pc
                writer.WriteCodeReference(_sectionSymbolName, _methodAddress + clause.HandlerOffset);
                // DW_AT_high_pc
                writer.WriteAddressSize(clause.HandlerLength);
                writer.WriteEndDIE();
            }

            writer.WriteEndDIE();
        }

        private void DumpVar(DwarfInfoWriter writer, DebugVarInfoMetadata metadataInfo, uint typeIndex, bool isThis)
        {
            bool usesDebugLoc = metadataInfo.DebugVarInfo.Ranges.Length != 1;

            if (metadataInfo.IsParameter)
            {
                if (isThis)
                {
                    writer.WriteStartDIE(usesDebugLoc ? DwarfAbbrev.FormalParameterThisLoc : DwarfAbbrev.FormalParameterThis);
                }
                else
                {
                    writer.WriteStartDIE(usesDebugLoc ? DwarfAbbrev.FormalParameterLoc : DwarfAbbrev.FormalParameter);
                }
            }
            else
            {
                writer.WriteStartDIE(usesDebugLoc ? DwarfAbbrev.VariableLoc : DwarfAbbrev.Variable);
            }

            // DW_AT_name
            writer.WriteStringReference(isThis ? "this" : metadataInfo.Name);

            // DW_AT_decl_file, DW_AT_decl_line
            writer.Write([1, 1]);

            // DW_AT_type
            writer.WriteInfoReference(typeIndex);

            // DW_AT_location
            if (usesDebugLoc)
            {
                writer.WriteStartLocationList();
                foreach (var range in metadataInfo.DebugVarInfo.Ranges)
                {
                    var expressionBuilder = writer.GetExpressionBuilder();
                    DumpVarLocation(expressionBuilder, range.VarLoc);
                    writer.WriteLocationListExpression(
                        _sectionSymbolName,
                        _methodAddress + range.StartOffset,
                        _methodAddress + range.EndOffset,
                        expressionBuilder);
                }
                writer.WriteEndLocationList();
            }
            else
            {
                var expressionBuilder = writer.GetExpressionBuilder();
                DumpVarLocation(expressionBuilder, metadataInfo.DebugVarInfo.Ranges[0].VarLoc);
                writer.WriteExpression(expressionBuilder);
            }

            writer.WriteEndDIE();
        }

        private static void DumpVarLocation(DwarfExpressionBuilder e, VarLoc loc)
        {
            switch (loc.LocationType)
            {
                case VarLocType.VLT_REG:
                case VarLocType.VLT_REG_FP:
                    e.OpReg(loc.B);
                    break;
                case VarLocType.VLT_REG_BYREF:
                    e.OpBReg(loc.B);
                    break;
                case VarLocType.VLT_STK:
                case VarLocType.VLT_STK2:
                case VarLocType.VLT_FPSTK:
                case VarLocType.VLT_STK_BYREF:
                    e.OpBReg(loc.B, loc.C);
                    if (loc.LocationType == VarLocType.VLT_STK_BYREF)
                    {
                        e.OpDeref();
                    }
                    break;
                case VarLocType.VLT_REG_REG:
                    e.OpReg(loc.C);
                    e.OpPiece();
                    e.OpReg(loc.B);
                    e.OpPiece();
                    break;
                case VarLocType.VLT_REG_STK:
                    e.OpReg(loc.B);
                    e.OpPiece();
                    e.OpBReg(loc.C, loc.D);
                    e.OpPiece();
                    break;
                case VarLocType.VLT_STK_REG:
                    e.OpBReg(loc.B, loc.C);
                    e.OpPiece();
                    e.OpReg(loc.D);
                    e.OpPiece();
                    break;
                default:
                    // Unsupported
                    Debug.Assert(loc.LocationType != VarLocType.VLT_FIXED_VA);
                    break;
            }
        }
    }

    internal sealed class DwarfStaticVariableInfo
    {
        private readonly StaticDataFieldDescriptor _descriptor;

        public long InfoOffset { get; set; }
        public string Name => _descriptor.StaticDataName;

        public DwarfStaticVariableInfo(StaticDataFieldDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public void Dump(DwarfInfoWriter writer, string sectionSymbolName, long address)
        {
            writer.WriteStartDIE(DwarfAbbrev.VariableStatic);

            // DW_AT_specification
            writer.WriteInfoAbsReference(InfoOffset);
            // DW_AT_location
            uint length = 1 + (uint)writer.TargetPointerSize; // DW_OP_addr <addr>
            if (_descriptor.IsStaticDataInObject != 0)
                length += 2; // DW_OP_deref, DW_OP_deref
            if (_descriptor.StaticOffset != 0)
                length += 1 + DwarfHelper.SizeOfULEB128(_descriptor.StaticOffset); // DW_OP_plus_uconst <const>
            writer.WriteULEB128(length);
            writer.Write([DW_OP_addr]);
            writer.WriteCodeReference(sectionSymbolName, address);
            if (_descriptor.IsStaticDataInObject != 0)
                writer.Write([DW_OP_deref, DW_OP_deref]);
            if (_descriptor.StaticOffset != 0)
            {
                writer.Write([DW_OP_plus_uconst]);
                writer.WriteULEB128(_descriptor.StaticOffset);
            }

            writer.WriteEndDIE();
        }
    }
}
