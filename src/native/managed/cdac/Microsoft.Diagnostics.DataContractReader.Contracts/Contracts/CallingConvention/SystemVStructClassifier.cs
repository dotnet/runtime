// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal static class SystemVStructClassifier
{
    private const int MaxFields = SystemVStructDescriptor.MaxEightBytes * SystemVStructDescriptor.EightByteSizeInBytes;

    private struct Helper
    {
        public int StructSize;
        public int EightByteCount;
        public SystemVClassification[] EightByteClassifications;
        public int[] EightByteSizes;
        public int[] EightByteOffsets;

        public bool InEmbeddedStruct;
        public int CurrentUniqueOffsetField;
        public int LargestFieldOffset;
        public SystemVClassification[] FieldClassifications;
        public int[] FieldSizes;
        public int[] FieldOffsets;

        public static Helper Create(int totalStructSize) => new()
        {
            StructSize = totalStructSize,
            EightByteCount = 0,
            InEmbeddedStruct = false,
            CurrentUniqueOffsetField = 0,
            LargestFieldOffset = -1,
            EightByteClassifications = new SystemVClassification[SystemVStructDescriptor.MaxEightBytes],
            EightByteSizes = new int[SystemVStructDescriptor.MaxEightBytes],
            EightByteOffsets = new int[SystemVStructDescriptor.MaxEightBytes],
            FieldClassifications = new SystemVClassification[MaxFields],
            FieldSizes = new int[MaxFields],
            FieldOffsets = new int[MaxFields],
        };
    }

    public static SystemVStructDescriptor Classify(Target target, TypeHandle typeHandle, int structSize)
    {
        if (!typeHandle.IsMethodTable() || structSize == 0
            || structSize > SystemVStructDescriptor.MaxStructBytesToPassInRegisters)
        {
            return default;
        }

        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        if (!rts.IsValueType(typeHandle))
            return default;

        // Intrinsic SIMD/Int128 types bypass the SysV struct path (they're handled by
        // their own intrinsic register-passing rules at the JIT level).
        if (IsSimdOrInt128Intrinsic(rts, typeHandle))
            return default;

        Helper helper = Helper.Create(structSize);
        if (!ClassifyEightBytes(target, rts, typeHandle, ref helper, 0))
            return default;

        return new SystemVStructDescriptor
        {
            PassedInRegisters = true,
            EightByteCount = (byte)helper.EightByteCount,
            EightByteClassification0 = helper.EightByteClassifications[0],
            EightByteClassification1 = helper.EightByteClassifications[1],
            EightByteSize0 = (byte)helper.EightByteSizes[0],
            EightByteSize1 = (byte)helper.EightByteSizes[1],
            EightByteOffset0 = (byte)helper.EightByteOffsets[0],
            EightByteOffset1 = (byte)helper.EightByteOffsets[1],
        };
    }

    private static bool IsSimdOrInt128Intrinsic(IRuntimeTypeSystem rts, TypeHandle th)
    {
        // Vector64/128/256/512 of T and Int128/UInt128 carry a non-zero VectorSize on cDAC.
        // If the type-system contract reports a vector size, treat it as an intrinsic that
        // bypasses SysV struct classification.
        try
        {
            return rts.GetVectorSize(th) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static SystemVClassification CorElementTypeToClassification(CorElementType et) => et switch
    {
        CorElementType.Boolean or CorElementType.Char
            or CorElementType.I1 or CorElementType.U1
            or CorElementType.I2 or CorElementType.U2
            or CorElementType.I4 or CorElementType.U4
            or CorElementType.I8 or CorElementType.U8
            or CorElementType.I or CorElementType.U
            or CorElementType.Ptr or CorElementType.FnPtr
            => SystemVClassification.Integer,
        CorElementType.R4 or CorElementType.R8
            => SystemVClassification.SSE,
        CorElementType.ValueType
            => SystemVClassification.Struct, // recurse
        CorElementType.Class or CorElementType.Object or CorElementType.String
            or CorElementType.Array or CorElementType.SzArray
            or CorElementType.Var or CorElementType.MVar
            or CorElementType.GenericInst
            => SystemVClassification.IntegerReference,
        CorElementType.Byref
            => SystemVClassification.IntegerByRef,
        _ => SystemVClassification.Unknown,
    };

    private static SystemVClassification ReClassifyField(SystemVClassification original, SystemVClassification @new)
    {
        switch (@new)
        {
            case SystemVClassification.Integer:
                // Integer overrides everything; the resulting class is Integer.
                return SystemVClassification.Integer;
            case SystemVClassification.SSE:
                // If both old and new are SSE, the merge is SSE. Otherwise Integer wins.
                return original == SystemVClassification.SSE
                    ? SystemVClassification.SSE
                    : SystemVClassification.Integer;
            case SystemVClassification.IntegerReference:
                // IntegerReference can only merge with itself.
                return SystemVClassification.IntegerReference;
            case SystemVClassification.IntegerByRef:
                // IntegerByRef can only merge with itself.
                return SystemVClassification.IntegerByRef;
            default:
                return SystemVClassification.Unknown;
        }
    }

    private static bool ClassifyEightBytes(
        Target target,
        IRuntimeTypeSystem rts,
        TypeHandle typeHandle,
        ref Helper helper,
        int startOffsetOfStruct)
    {
        ushort numInstanceFields = rts.GetNumInstanceFields(typeHandle);
        TargetPointer firstFieldDesc = rts.GetFieldDescList(typeHandle);

        if (firstFieldDesc == TargetPointer.Null || numInstanceFields == 0)
        {
            // Empty struct: classify like padding.
            helper.LargestFieldOffset = startOffsetOfStruct;
            AssignClassifiedEightByteTypes(ref helper);
            return true;
        }

        uint fieldDescSize = (uint)target.GetTypeInfo(DataType.FieldDesc).Size!;

        // Walk the FieldDesc array. Each FieldDesc has a fixed size; statics are
        // intermixed with instance fields in this array, so we filter by IsStatic.
        ushort totalFields = (ushort)(numInstanceFields + rts.GetNumStaticFields(typeHandle)
            + rts.GetNumThreadStaticFields(typeHandle));

        for (ushort i = 0; i < totalFields; i++)
        {
            TargetPointer fdPtr = new(firstFieldDesc.Value + i * fieldDescSize);
            if (rts.IsFieldDescStatic(fdPtr))
                continue;

            CorElementType fieldType = rts.GetFieldDescType(fdPtr);
            uint memberDef = rts.GetFieldDescMemberDef(fdPtr);
            EntityHandle entity = MetadataTokens.EntityHandle((int)memberDef);
            if (entity.IsNil || entity.Kind != HandleKind.FieldDefinition)
                return false;

            // Resolve the field's FieldDefinition (used by GetFieldDescOffset for the
            // BigRVA path on static RVA fields — instance walks never hit it, but we
            // keep the lookup for safety/parity with the offset API contract).
            FieldDefinition fieldDef = default;
            try
            {
                TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fdPtr);
                TypeHandle ctx = rts.GetTypeHandle(enclosingMT);
                TargetPointer modulePtr = rts.GetModule(ctx);
                if (modulePtr != TargetPointer.Null)
                {
                    ModuleHandle moduleHandle = target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
                    MetadataReader? mdReader = target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
                    if (mdReader is not null)
                        fieldDef = mdReader.GetFieldDefinition((FieldDefinitionHandle)entity);
                }
            }
            catch
            {
                return false;
            }

            uint fieldOffset = rts.GetFieldDescOffset(fdPtr, fieldDef);
            int normalizedFieldOffset = (int)fieldOffset + startOffsetOfStruct;

            int fieldSize = ArgTypeInfo.GetElemSize(fieldType, default, target.PointerSize);
            if (fieldType == CorElementType.ValueType)
            {
                // For nested value-type fields, resolve the field's TypeHandle to get its size.
                TypeHandle fieldTH = rts.LookupApproxFieldTypeHandle(fdPtr);
                if (fieldTH.IsMethodTable())
                    fieldSize = rts.GetNumInstanceFieldBytes(fieldTH);
            }

            if (normalizedFieldOffset + fieldSize > helper.StructSize)
                return false;

            SystemVClassification fieldClass = CorElementTypeToClassification(fieldType);
            if (fieldClass == SystemVClassification.Struct)
            {
                // Recurse into the nested value type's fields.
                TypeHandle nested = rts.LookupApproxFieldTypeHandle(fdPtr);
                if (!nested.IsMethodTable())
                    return false;

                bool savedInEmbedded = helper.InEmbeddedStruct;
                helper.InEmbeddedStruct = true;
                bool nestedOk = ClassifyEightBytes(target, rts, nested, ref helper, normalizedFieldOffset);
                helper.InEmbeddedStruct = savedInEmbedded;
                if (!nestedOk)
                    return false;
                continue;
            }

            // Unaligned-field rule: a field that is not naturally aligned forces MEMORY.
            if (fieldSize > 0 && (normalizedFieldOffset % fieldSize) != 0)
                return false;

            // Overlapping-field (union) handling: if this offset has already been
            // recorded, merge the new classification with the existing one.
            if (normalizedFieldOffset <= helper.LargestFieldOffset)
            {
                int existing = -1;
                for (int j = helper.CurrentUniqueOffsetField - 1; j >= 0; j--)
                {
                    if (helper.FieldOffsets[j] == normalizedFieldOffset)
                    {
                        if (fieldSize > helper.FieldSizes[j])
                            helper.FieldSizes[j] = fieldSize;
                        helper.FieldClassifications[j] = ReClassifyField(helper.FieldClassifications[j], fieldClass);
                        existing = j;
                        break;
                    }
                }
                if (existing >= 0)
                    continue;
            }
            else
            {
                helper.LargestFieldOffset = normalizedFieldOffset;
            }

            if (helper.CurrentUniqueOffsetField >= MaxFields)
                return false;

            helper.FieldClassifications[helper.CurrentUniqueOffsetField] = fieldClass;
            helper.FieldSizes[helper.CurrentUniqueOffsetField] = fieldSize;
            helper.FieldOffsets[helper.CurrentUniqueOffsetField] = normalizedFieldOffset;
            helper.CurrentUniqueOffsetField++;
        }

        AssignClassifiedEightByteTypes(ref helper);
        return true;
    }

    private static void AssignClassifiedEightByteTypes(ref Helper helper)
    {
        const int MaxBytes = SystemVStructDescriptor.MaxEightBytes * SystemVStructDescriptor.EightByteSizeInBytes;
        if (helper.InEmbeddedStruct)
            return;

        int largestFieldOffset = helper.LargestFieldOffset;
        if (largestFieldOffset < 0)
            largestFieldOffset = 0;

        Span<int> sortedFieldOrder = stackalloc int[MaxBytes];
        for (int i = 0; i < MaxBytes; i++) sortedFieldOrder[i] = -1;

        int numFields = helper.CurrentUniqueOffsetField;
        for (int i = 0; i < numFields; i++)
        {
            int off = helper.FieldOffsets[i];
            if (off < 0 || off >= MaxBytes) continue;
            sortedFieldOrder[off] = i;
        }

        int lastFieldOrdinal = largestFieldOffset < MaxBytes ? sortedFieldOrder[largestFieldOffset] : -1;
        int lastFieldSize = lastFieldOrdinal >= 0 ? helper.FieldSizes[lastFieldOrdinal] : 0;
        int offsetAfterLastFieldByte = largestFieldOffset + lastFieldSize;
        SystemVClassification lastFieldClassification = lastFieldOrdinal >= 0
            ? helper.FieldClassifications[lastFieldOrdinal]
            : SystemVClassification.NoClass;

        int usedEightBytes = 0;
        int accumulatedSizeForEightBytes = 0;
        bool foundFieldInEightByte = false;

        for (int offset = 0; offset < helper.StructSize; offset++)
        {
            SystemVClassification fieldClassificationType;
            int fieldSize;

            int ordinal = offset < MaxBytes ? sortedFieldOrder[offset] : -1;
            if (ordinal == -1)
            {
                if (offset < accumulatedSizeForEightBytes)
                    continue; // inside a previously-processed field

                fieldSize = 1;
                // Padding before the last field's end -> NoClass; trailing padding
                // inherits the last field's classification (per spec).
                fieldClassificationType = offset < offsetAfterLastFieldByte
                    ? SystemVClassification.NoClass
                    : lastFieldClassification;
                if (offset % SystemVStructDescriptor.EightByteSizeInBytes == 0)
                    foundFieldInEightByte = false;
            }
            else
            {
                foundFieldInEightByte = true;
                fieldSize = helper.FieldSizes[ordinal];
                fieldClassificationType = helper.FieldClassifications[ordinal];
                accumulatedSizeForEightBytes = offset + fieldSize;
            }

            int fieldStartEightByte = offset / SystemVStructDescriptor.EightByteSizeInBytes;
            int fieldEndEightByte = (offset + fieldSize - 1) / SystemVStructDescriptor.EightByteSizeInBytes;
            if (fieldEndEightByte >= SystemVStructDescriptor.MaxEightBytes)
                return; // shouldn't happen for size <= 16, but guard anyway

            usedEightBytes = System.Math.Max(usedEightBytes, fieldEndEightByte + 1);

            for (int eb = fieldStartEightByte; eb <= fieldEndEightByte; eb++)
            {
                SystemVClassification existing = helper.EightByteClassifications[eb];
                if (existing == fieldClassificationType)
                {
                    // Already this class
                }
                else if (existing == SystemVClassification.NoClass)
                {
                    helper.EightByteClassifications[eb] = fieldClassificationType;
                }
                else if (existing == SystemVClassification.Memory
                    || fieldClassificationType == SystemVClassification.Memory
                    || existing == SystemVClassification.IntegerReference
                    || fieldClassificationType == SystemVClassification.IntegerReference
                    || existing == SystemVClassification.IntegerByRef
                    || fieldClassificationType == SystemVClassification.IntegerByRef)
                {
                    helper.EightByteClassifications[eb] = ReClassifyField(existing, fieldClassificationType);
                }
                else if (existing == SystemVClassification.Integer
                    || fieldClassificationType == SystemVClassification.Integer)
                {
                    helper.EightByteClassifications[eb] = SystemVClassification.Integer;
                }
                else
                {
                    helper.EightByteClassifications[eb] = SystemVClassification.SSE;
                }
            }

            if ((offset + 1) % SystemVStructDescriptor.EightByteSizeInBytes == 0)
            {
                // Promote NoClass eightbyte to Integer when there's no field in it
                // (matches the workaround in methodtable.cpp:2660).
                int eb = offset / SystemVStructDescriptor.EightByteSizeInBytes;
                if (!foundFieldInEightByte
                    && helper.EightByteClassifications[eb] == SystemVClassification.NoClass)
                {
                    helper.EightByteClassifications[eb] = SystemVClassification.Integer;
                }
                foundFieldInEightByte = false;
            }
        }

        helper.EightByteCount = usedEightBytes;
        for (int i = 0; i < usedEightBytes; i++)
        {
            helper.EightByteOffsets[i] = i * SystemVStructDescriptor.EightByteSizeInBytes;
            int remaining = helper.StructSize - helper.EightByteOffsets[i];
            helper.EightByteSizes[i] = System.Math.Min(remaining, SystemVStructDescriptor.EightByteSizeInBytes);
        }
    }
}
