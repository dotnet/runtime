// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;

using static ILCompiler.ObjectWriter.CodeViewNative;
using static ILCompiler.ObjectWriter.CodeViewNative.CodeViewPointer;
using static ILCompiler.ObjectWriter.CodeViewNative.CodeViewPropertyFlags;
using static ILCompiler.ObjectWriter.CodeViewNative.CodeViewType;
using static ILCompiler.ObjectWriter.CodeViewNative.LeafRecordType;

namespace ILCompiler.ObjectWriter
{
    internal sealed class CodeViewTypesBuilder : ITypesDebugInfoWriter
    {
        private NameMangler _nameMangler;
        private TargetArchitecture _architecture;
        private Stream _outputStream;
        private int _targetPointerSize;

        private uint _classVTableTypeIndex;
        private uint _vfuncTabTypeIndex;
        private List<(string, uint)> _userDefinedTypes = new();

        private uint _nextTypeIndex = 0x1000;

        public IList<(string, uint)> UserDefinedTypes => _userDefinedTypes;

        public CodeViewTypesBuilder(NameMangler nameMangler, TargetArchitecture targetArchitecture, Stream outputStream)
        {
            _nameMangler = nameMangler;
            _architecture = targetArchitecture;
            _outputStream = outputStream;
            _targetPointerSize = targetArchitecture switch
            {
                TargetArchitecture.ARM => 4,
                TargetArchitecture.X86 => 4,
                _ => 8,
            };

            // Write CodeView version header
            Span<byte> versionBuffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(versionBuffer, 4);
            _outputStream.Write(versionBuffer);

            // We pretend that the MethodTable pointer in System.Object is VTable shape.
            // We use the same "Vtable" for all types because the vtable shape debug
            // record is not expressive enough to capture our vtable shape (where the
            // vtable slots don't start at the beginning of the vtable).
            using (LeafRecordWriter record = StartLeafRecord(LF_VTSHAPE))
            {
                record.Write((ushort)0); // Number of entries in vfunctable
            }
            _classVTableTypeIndex = _nextTypeIndex++;

            using (LeafRecordWriter record = StartLeafRecord(LF_POINTER))
            {
                record.Write(_classVTableTypeIndex);
                record.Write((uint)((_targetPointerSize == 8 ? CV_PTR_64 : CV_PTR_NEAR32) | CV_PTR_MODE_LVREF));
            }
            _vfuncTabTypeIndex = _nextTypeIndex++;
        }

        public uint GetPrimitiveTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive, "it is not a primitive type");
            return GetPrimitiveTypeIndex(type.Category);
        }

        private uint GetPrimitiveTypeIndex(TypeFlags typeFlags)
        {
            // CodeView uses predefined codes for simple types
            return (uint)(typeFlags switch
            {
                TypeFlags.Boolean => T_BOOL08,
                TypeFlags.Char => T_WCHAR,
                TypeFlags.SByte => T_INT1,
                TypeFlags.Byte => T_UINT1,
                TypeFlags.Int16 => T_INT2,
                TypeFlags.UInt16 => T_UINT2,
                TypeFlags.Int32 => T_INT4,
                TypeFlags.UInt32 => T_UINT4,
                TypeFlags.Int64 => T_INT8,
                TypeFlags.UInt64 => T_UINT8,
                TypeFlags.IntPtr => _targetPointerSize == 8 ? T_INT8 : T_INT4,
                TypeFlags.UIntPtr => _targetPointerSize == 8 ? T_UINT8 : T_UINT4,
                TypeFlags.Single => T_REAL32,
                TypeFlags.Double => T_REAL64,
                _ => T_NOTYPE,
            });
        }

        public uint GetPointerTypeIndex(PointerTypeDescriptor pointerDescriptor)
        {
            using (LeafRecordWriter record = StartLeafRecord(LF_POINTER))
            {
                record.Write(pointerDescriptor.ElementType);
                record.Write((uint)(
                    (pointerDescriptor.Is64Bit == 1 ? CV_PTR_64 : CV_PTR_NEAR32) |
                    (pointerDescriptor.IsReference == 1 ? CV_PTR_MODE_LVREF : CV_PTR_MODE_PTR) |
                    (pointerDescriptor.IsConst == 1 ? CV_PTR_IS_CONST : 0)));
            }

            return _nextTypeIndex++;
        }

        public uint GetArrayTypeIndex(
            ClassTypeDescriptor classDescriptor,
            ArrayTypeDescriptor arrayDescriptor)
        {
            uint memberCount = 0;
            uint offset = 0;

            using (LeafRecordWriter arrayRecord = StartLeafRecord(LF_ARRAY))
            {
                arrayRecord.Write(arrayDescriptor.ElementType);
                arrayRecord.Write(T_INT4);
                arrayRecord.Write(arrayDescriptor.Size);
                arrayRecord.Write("");
            }

            uint arrayRecordTypeIndex = _nextTypeIndex++;

            using (LeafRecordWriter fieldListRecord = StartLeafRecord(LF_FIELDLIST))
            {
                if (classDescriptor.BaseClassId != 0)
                {
                    fieldListRecord.Write((ushort)LF_BCLASS);
                    fieldListRecord.Write((ushort)0); // TODO: Attributes
                    fieldListRecord.Write(classDescriptor.BaseClassId);
                    fieldListRecord.WriteEncodedInteger(0); // Offset
                    fieldListRecord.WritePadding();
                    memberCount++;
                    offset += (uint)_targetPointerSize;
                }

                fieldListRecord.Write((ushort)LF_MEMBER);
                fieldListRecord.Write((ushort)0); // TODO: Attributes
                fieldListRecord.Write(T_INT4);
                fieldListRecord.WriteEncodedInteger(offset);
                fieldListRecord.Write("count");
                fieldListRecord.WritePadding();
                memberCount++;
                offset += (uint)_targetPointerSize;

                if (arrayDescriptor.IsMultiDimensional == 1)
                {
                    for (uint i = 0; i < arrayDescriptor.Rank; ++i)
                    {
                        fieldListRecord.Write((ushort)LF_MEMBER);
                        fieldListRecord.Write((ushort)0); // TODO: Attributes
                        fieldListRecord.Write(T_INT4);
                        fieldListRecord.WriteEncodedInteger(offset);
                        fieldListRecord.Write($"length{i}");
                        fieldListRecord.WritePadding();
                        memberCount++;
                        offset += 4;
                    }

                    for (uint i = 0; i < arrayDescriptor.Rank; ++i)
                    {
                        fieldListRecord.Write((ushort)LF_MEMBER);
                        fieldListRecord.Write((ushort)0); // TODO: Attributes
                        fieldListRecord.Write(T_INT4);
                        fieldListRecord.WriteEncodedInteger(offset);
                        fieldListRecord.Write($"bounds{i}");
                        fieldListRecord.WritePadding();
                        memberCount++;
                        offset += 4;
                    }
                }

                fieldListRecord.Write((ushort)LF_MEMBER);
                fieldListRecord.Write((ushort)0); // TODO: Attributes
                fieldListRecord.Write(arrayRecordTypeIndex);
                fieldListRecord.WriteEncodedInteger(offset);
                fieldListRecord.Write("values");
                fieldListRecord.WritePadding();
                memberCount++;
            }

            uint fieldListTypeIndex = _nextTypeIndex++;

            Debug.Assert(classDescriptor.IsStruct == 0);
            using (LeafRecordWriter record = StartLeafRecord(LF_CLASS))
            {
                record.Write((ushort)memberCount); // Number of elements in class
                record.Write((ushort)0); // TODO: Options
                record.Write(fieldListTypeIndex); // Field descriptor index
                record.Write((uint)0); // Derived-from descriptor index
                record.Write((uint)0); // Vtshape descriptor index
                record.Write((ushort)arrayDescriptor.Size); // Size
                record.Write(classDescriptor.Name);
            }

            uint typeIndex = _nextTypeIndex++;
            _userDefinedTypes.Add((classDescriptor.Name, typeIndex));

            return typeIndex;
        }

        public uint GetEnumTypeIndex(
            EnumTypeDescriptor typeDescriptor,
            EnumRecordTypeDescriptor[] typeRecords)
        {
            using (LeafRecordWriter fieldListRecord = StartLeafRecord(LF_FIELDLIST))
            {
                foreach (EnumRecordTypeDescriptor record in typeRecords)
                {
                    fieldListRecord.Write((ushort)LF_ENUMERATE);
                    fieldListRecord.Write((ushort)0); // TODO: Attributes
                    fieldListRecord.WriteEncodedInteger(record.Value);
                    fieldListRecord.Write(record.Name);
                    fieldListRecord.WritePadding();
                }
            }

            uint fieldListTypeIndex = _nextTypeIndex++;

            using (LeafRecordWriter record = StartLeafRecord(LF_ENUM))
            {
                record.Write((ushort)typeRecords.Length); // Number of elements in class
                record.Write((ushort)0); // TODO: Attributes
                record.Write(typeDescriptor.ElementType);
                record.Write(fieldListTypeIndex);
                record.Write(typeDescriptor.Name);
            }

            uint typeIndex = _nextTypeIndex++;
            _userDefinedTypes.Add((typeDescriptor.Name, typeIndex));

            return typeIndex;
        }

        public uint GetClassTypeIndex(ClassTypeDescriptor classDescriptor)
        {
            using (LeafRecordWriter record = StartLeafRecord(classDescriptor.IsStruct == 1 ? LF_STRUCTURE : LF_CLASS))
            {
                record.Write((ushort)0); // Number of elements in class
                record.Write((ushort)CV_PROP_FORWARD_REFERENCE);
                record.Write((uint)0); // Field descriptor index
                record.Write((uint)0); // Derived-from descriptor index
                record.Write((uint)0); // Vtshape descriptor index
                record.Write((ushort)0); // Size
                record.Write(classDescriptor.Name);
            }

            return _nextTypeIndex++;
        }

        public uint GetCompleteClassTypeIndex(
            ClassTypeDescriptor classTypeDescriptor,
            ClassFieldsTypeDescriptor classFieldsTypeDescriptor,
            DataFieldDescriptor[] fields,
            StaticDataFieldDescriptor[] statics)
        {
            uint memberCount = 0;

            using (LeafRecordWriter fieldListRecord = StartLeafRecord(LF_FIELDLIST))
            {
                if (classTypeDescriptor.BaseClassId != 0)
                {
                    fieldListRecord.Write((ushort)LF_BCLASS);
                    fieldListRecord.Write((ushort)0); // TODO: Attributes
                    fieldListRecord.Write(classTypeDescriptor.BaseClassId);
                    fieldListRecord.WriteEncodedInteger(0); // Offset
                    fieldListRecord.WritePadding();
                    memberCount++;
                }
                else if (classTypeDescriptor.IsStruct == 0)
                {
                    fieldListRecord.Write((ushort)LF_VFUNCTAB);
                    fieldListRecord.Write((ushort)0); // Padding
                    fieldListRecord.Write(_vfuncTabTypeIndex);
                    fieldListRecord.WritePadding();
                    memberCount++;
                }

                foreach (DataFieldDescriptor desc in fields)
                {
                    if (desc.Offset == 0xFFFFFFFF)
                    {
                        fieldListRecord.Write((ushort)LF_STMEMBER);
                        fieldListRecord.Write((ushort)0); // TODO: Attributes
                        fieldListRecord.Write(desc.FieldTypeIndex);
                        fieldListRecord.Write(desc.Name);
                    }
                    else
                    {
                        fieldListRecord.Write((ushort)LF_MEMBER);
                        fieldListRecord.Write((ushort)0); // TODO: Attributes
                        fieldListRecord.Write(desc.FieldTypeIndex);
                        fieldListRecord.WriteEncodedInteger(desc.Offset);
                        fieldListRecord.Write(desc.Name);
                    }
                    fieldListRecord.WritePadding();
                    memberCount++;
                }
            }

            uint fieldListTypeIndex = _nextTypeIndex++;

            using (LeafRecordWriter record = StartLeafRecord(classTypeDescriptor.IsStruct == 1 ? LF_STRUCTURE : LF_CLASS))
            {
                record.Write((ushort)memberCount); // Number of elements in class
                record.Write((ushort)0); // TODO: Options
                record.Write(fieldListTypeIndex); // Field descriptor index
                record.Write((uint)0); // Derived-from descriptor index
                record.Write((uint)0); // Vtshape descriptor index
                record.Write((ushort)classFieldsTypeDescriptor.Size); // Size
                record.Write(classTypeDescriptor.Name);
            }

            uint typeIndex = _nextTypeIndex++;
            _userDefinedTypes.Add((classTypeDescriptor.Name, typeIndex));

            return typeIndex;
        }

        public uint GetMemberFunctionTypeIndex(MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes)
        {
            using (LeafRecordWriter fieldListRecord = StartLeafRecord(LF_ARGLIST))
            {
                fieldListRecord.Write((uint)argumentTypes.Length);
                foreach (uint argumentType in argumentTypes)
                {
                    fieldListRecord.Write(argumentType);
                }
            }

            uint argumentListTypeIndex = _nextTypeIndex++;

            using (LeafRecordWriter record = StartLeafRecord(LF_MFUNCTION))
            {
                record.Write(memberDescriptor.ReturnType);
                record.Write(memberDescriptor.ContainingClass);
                record.Write(memberDescriptor.TypeIndexOfThisPointer);
                record.Write((byte)memberDescriptor.CallingConvention);
                record.Write((byte)0); // TODO: Attributes
                record.Write((ushort)memberDescriptor.NumberOfArguments);
                record.Write(argumentListTypeIndex);
                record.Write((uint)memberDescriptor.ThisAdjust);
            }

            return _nextTypeIndex++;
        }

        public uint GetMemberFunctionId(MemberFunctionIdTypeDescriptor memberIdDescriptor)
        {
            using (LeafRecordWriter record = StartLeafRecord(LF_MFUNC_ID))
            {
                record.Write(memberIdDescriptor.ParentClass);
                record.Write(memberIdDescriptor.MemberFunction);
                record.Write(memberIdDescriptor.Name);
            }

            return _nextTypeIndex++;
        }

        public string GetMangledName(TypeDesc type)
        {
            return _nameMangler.GetMangledTypeName(type);
        }

        private LeafRecordWriter StartLeafRecord(LeafRecordType leafRecordType)
        {
            LeafRecordWriter writer = new LeafRecordWriter(this);
            writer.Write((ushort)leafRecordType);
            return writer;
        }

        private ref struct LeafRecordWriter
        {
            private CodeViewTypesBuilder _debugTypesBuilder;
            private ArrayBufferWriter<byte> _bufferWriter;

            public LeafRecordWriter(CodeViewTypesBuilder debugTypesBuilder)
            {
                _debugTypesBuilder = debugTypesBuilder;
                _bufferWriter = new();
            }

            public void Dispose()
            {
                int length = sizeof(ushort) + _bufferWriter.WrittenCount;
                int padding = ((length + 3) & ~3) - length;
                Span<byte> lengthBuffer = stackalloc byte[sizeof(ushort)];
                BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, (ushort)(length + padding - sizeof(ushort)));
                _debugTypesBuilder._outputStream.Write(lengthBuffer);
                _debugTypesBuilder._outputStream.Write(_bufferWriter.WrittenSpan);
                _debugTypesBuilder._outputStream.Write(stackalloc byte[padding]);
                _bufferWriter.Clear();

                // TODO: LeafRecordType.Index for long records
            }

            public void Write(byte value)
            {
                _bufferWriter.GetSpan(1)[0] = value;
                _bufferWriter.Advance(1);
            }

            public void Write(ushort value)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(_bufferWriter.GetSpan(sizeof(ushort)), value);
                _bufferWriter.Advance(sizeof(ushort));
            }

            public void Write(uint value)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(_bufferWriter.GetSpan(sizeof(uint)), value);
                _bufferWriter.Advance(sizeof(uint));
            }

            public void Write(ulong value)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(_bufferWriter.GetSpan(sizeof(ulong)), value);
                _bufferWriter.Advance(sizeof(ulong));
            }

            public void Write(string value)
            {
                int byteCount = Encoding.UTF8.GetByteCount(value) + 1;
                Encoding.UTF8.GetBytes(value, _bufferWriter.GetSpan(byteCount));
                _bufferWriter.Advance(byteCount);
            }

            public void WriteEncodedInteger(ulong value)
            {
                if (value < (ushort)LF_NUMERIC)
                {
                    Write((ushort)value);
                }
                else if (value <= ushort.MaxValue)
                {
                    Write((ushort)LF_USHORT);
                    Write((ushort)value);
                }
                else if (value <= uint.MaxValue)
                {
                    Write((ushort)LF_ULONG);
                    Write((uint)value);
                }
                else
                {
                    Write((ushort)LF_UQUADWORD);
                    Write(value);
                }
            }

            public void WritePadding()
            {
                int paddingLength = ((_bufferWriter.WrittenCount - 2 + 3) & ~3) - (_bufferWriter.WrittenCount - 2);
                Span<byte> padding = _bufferWriter.GetSpan(paddingLength);
                for (int i = 0; i < paddingLength; i++)
                {
                    padding[i] = (byte)(LF_PAD0 + paddingLength - i);
                }
                _bufferWriter.Advance(paddingLength);
            }

            public void Write(CodeViewType value) => Write((uint)value);
        }
    }
}
