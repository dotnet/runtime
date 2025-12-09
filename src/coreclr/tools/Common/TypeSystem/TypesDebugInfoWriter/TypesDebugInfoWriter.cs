// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Internal.Text;

namespace Internal.TypeSystem.TypesDebugInfo
{
    public interface ITypesDebugInfoWriter
    {
        uint GetEnumTypeIndex(EnumTypeDescriptor enumTypeDescriptor, EnumRecordTypeDescriptor[] typeRecords);

        uint GetClassTypeIndex(ClassTypeDescriptor classTypeDescriptor);

        uint GetCompleteClassTypeIndex(ClassTypeDescriptor classTypeDescriptor, ClassFieldsTypeDescriptor classFieldsTypeDescriptor,
                                       DataFieldDescriptor[] fields, StaticDataFieldDescriptor[] statics);

        uint GetArrayTypeIndex(ClassTypeDescriptor classDescriptor, ArrayTypeDescriptor arrayTypeDescriprtor);

        uint GetPointerTypeIndex(PointerTypeDescriptor pointerDescriptor);

        uint GetMemberFunctionTypeIndex(MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes);

        uint GetMemberFunctionId(MemberFunctionIdTypeDescriptor memberIdDescriptor);

        uint GetPrimitiveTypeIndex(TypeDesc type);

        Utf8String GetMangledName(TypeDesc type);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EnumRecordTypeDescriptor
    {
        public ulong Value;
        public Utf8String Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EnumTypeDescriptor
    {
        public uint ElementType;
        public ulong ElementCount;
        public Utf8String Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClassTypeDescriptor
    {
        public int IsStruct;
        public Utf8String Name;
        public uint BaseClassId;
        public ulong InstanceSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DataFieldDescriptor
    {
        public uint FieldTypeIndex;
        public ulong Offset;
        public Utf8String Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StaticDataFieldDescriptor
    {
        public Utf8String StaticDataName;
        public ulong StaticOffset;
        public int IsStaticDataInObject;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClassFieldsTypeDescriptor
    {
        public ulong Size;
        public int FieldsCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ArrayTypeDescriptor
    {
        public uint Rank;
        public uint ElementType;
        public uint Size;
        public int IsMultiDimensional;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointerTypeDescriptor
    {
        public uint ElementType;
        public int IsReference;
        public int IsConst;
        public int Is64Bit; // Otherwise, 32 bit
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemberFunctionTypeDescriptor
    {
        public uint ReturnType;
        public uint ContainingClass;
        public uint TypeIndexOfThisPointer;
        public uint ThisAdjust;
        public uint CallingConvention;
        public ushort NumberOfArguments;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemberFunctionIdTypeDescriptor
    {
        public uint MemberFunction;
        public uint ParentClass;
        public Utf8String Name;
    }
}
