// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.CallingConvention;
using Internal.JitInterface;
using Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

using CdacITypeHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ITypeHandle;
using CdacCorElementType = Microsoft.Diagnostics.DataContractReader.Contracts.CorElementType;
using SharedCorElementType = Internal.CorConstants.CorElementType;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Adapts cDAC signature type information to the shared <see cref="ITypeHandle"/>
/// interface used by ArgIterator for calling-convention computation.
/// </summary>
internal readonly struct CdacTypeHandle : Internal.CallingConvention.ITypeHandle
{
    private readonly SignatureTypeInfo _typeInfo;
    private readonly Target _target;
    private readonly TypeInformation _typeInformation;

    public CdacTypeHandle(CdacITypeHandle? typeHandle, Target target, TypeInformation typeInformation)
    {
        _target = target;
        _typeInformation = typeInformation;
        _typeInfo = typeHandle is null
            ? default
            : new SignatureTypeInfo(
                target.Contracts.RuntimeTypeSystem.GetSignatureCorElementType(typeHandle),
                typeHandle);
    }

    public CdacTypeHandle(SignatureTypeInfo typeInfo, Target target, TypeInformation typeInformation)
    {
        _typeInfo = typeInfo;
        _target = target;
        _typeInformation = typeInformation;
    }

    private IRuntimeTypeSystem Rts => _target.Contracts.RuntimeTypeSystem;
    private CdacITypeHandle? ExactTypeHandle => _typeInfo.ExactTypeHandle;

    public int PointerSize => _target.PointerSize;
    public RuntimeInfoArchitecture Arch => _target.Contracts.RuntimeInfo.GetTargetArchitecture();

    public bool IsNull() => ExactTypeHandle is null && _typeInfo.ElementType == default;

    public bool IsValueType()
        => _typeInfo.ElementType == CdacCorElementType.ValueType
            || (ExactTypeHandle is not null && Rts.IsValueType(ExactTypeHandle));

    public bool IsPointerType()
        => _typeInfo.ElementType == CdacCorElementType.Ptr
           || (ExactTypeHandle is not null && Rts.IsPointer(ExactTypeHandle));

    public bool HasIndeterminateSize()
        => _typeInfo.ElementType == CdacCorElementType.ValueType
            && ExactTypeHandle is null;

    public int GetSize()
    {
        if (_typeInfo.ElementType is CdacCorElementType.Ptr
                                  or CdacCorElementType.Byref
                                  or CdacCorElementType.SzArray
                                  or CdacCorElementType.Array
                                  or CdacCorElementType.Class
                                  or CdacCorElementType.Object
                                  or CdacCorElementType.String
                                  or CdacCorElementType.FnPtr)
        {
            return PointerSize;
        }

        if (ExactTypeHandle is null)
        {
            throw new NotImplementedException(
                $"Exact runtime layout is unavailable for {_typeInfo.ElementType}.");
        }

        // GetBaseSize returns the full object size including object header and padding.
        // For value types used in calling convention, we need the unboxed size.
        // BaseSize = ObjHeader + MethodTable* + unboxed fields, aligned to pointer size.
        // Unboxed size = BaseSize - 2 * PointerSize (subtract ObjHeader + MT pointer).
        uint baseSize = Rts.GetBaseSize(ExactTypeHandle);
        return (int)(baseSize - (uint)(2 * PointerSize));
    }

    public SharedCorElementType GetCorElementType()
    {
        if (_typeInfo.ElementType == default)
        {
            if (ExactTypeHandle is null)
            {
                return (SharedCorElementType)0;
            }

            return MapCorElementType(Rts.GetInternalCorElementType(ExactTypeHandle));
        }

        if (_typeInfo.ElementType != CdacCorElementType.ValueType || ExactTypeHandle is null)
        {
            return MapCorElementType(_typeInfo.ElementType);
        }

        // Mirror the runtime's MetaSig::PeekArgNormalized -- for value types
        // it resolves the closed ITypeHandle and returns
        // MethodTable::GetInternalCorElementType, which collapses enums to
        // their underlying primitive (byte enum -> U1, int enum -> I4, ...).
        // The shared ArgIterator's x86 IsArgumentInRegister relies on this
        // normalization to recognise sub-pointer-size enums as register-
        // passable; returning ELEMENT_TYPE_VALUETYPE for a byte enum makes
        // it fall into the IsTrivialPointerSizedStruct path which then
        // (correctly) rejects it because GetSize() != PointerSize, and the
        // arg gets mis-accounted as stack-passed.
        CdacCorElementType cdacType = Rts.GetInternalCorElementType(ExactTypeHandle);
        return MapCorElementType(cdacType);
    }

    public bool RequiresAlign8()
    {
        return ExactTypeHandle is not null && Rts.RequiresAlign8(ExactTypeHandle);
    }

    public bool IsHomogeneousAggregate()
        => ExactTypeHandle is not null && Rts.TryGetHFAElementSize(ExactTypeHandle, out _);

    public int GetHomogeneousAggregateElementSize()
    {
        Debug.Assert(IsHomogeneousAggregate());
        return Rts.TryGetHFAElementSize(ExactTypeHandle!, out int size) ? size : 0;
    }

    public void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor)
    {
        descriptor = default;
        descriptor.passedInRegisters = false;

        if (ExactTypeHandle is null)
            return;

        // Read the runtime-cached classification from the type system; mirrors
        // SystemVRegDescriptorFromSystemVEightByteRegistersInfo in jitinterface.cpp.
        // Only populated on UNIX_AMD64_ABI builds.
        if (!Rts.TryGetSystemVAmd64EightByteClassification(ExactTypeHandle, out SystemVAmd64EightByteClassification info))
            return;

        descriptor.passedInRegisters = true;
        descriptor.eightByteCount = 1;
        descriptor.eightByteClassifications0 = ToSystemVClassificationType(info.First.Classification);
        descriptor.eightByteSizes0 = info.First.Size;
        descriptor.eightByteOffsets0 = 0;

        if (info.Second is SystemVAmd64EightByte second)
        {
            descriptor.eightByteCount = 2;
            descriptor.eightByteClassifications1 = ToSystemVClassificationType(second.Classification);
            descriptor.eightByteSizes1 = second.Size;
            descriptor.eightByteOffsets1 = SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR.SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES;
        }
    }

    private static SystemVClassificationType ToSystemVClassificationType(SystemVAmd64Classification classification)
        => classification switch
        {
            SystemVAmd64Classification.Unknown => SystemVClassificationType.SystemVClassificationTypeUnknown,
            SystemVAmd64Classification.Struct => SystemVClassificationType.SystemVClassificationTypeStruct,
            SystemVAmd64Classification.NoClass => SystemVClassificationType.SystemVClassificationTypeNoClass,
            SystemVAmd64Classification.Memory => SystemVClassificationType.SystemVClassificationTypeMemory,
            SystemVAmd64Classification.Integer => SystemVClassificationType.SystemVClassificationTypeInteger,
            SystemVAmd64Classification.IntegerReference => SystemVClassificationType.SystemVClassificationTypeIntegerReference,
            SystemVAmd64Classification.IntegerByRef => SystemVClassificationType.SystemVClassificationTypeIntegerByRef,
            SystemVAmd64Classification.SSE => SystemVClassificationType.SystemVClassificationTypeSSE,
            _ => SystemVClassificationType.SystemVClassificationTypeUnknown,
        };

    public FpStructInRegistersInfo GetFpStructInRegistersInfo(Internal.TypeSystem.TargetArchitecture architecture)
    {
        // TODO(riscv-loongarch): Implement RISC-V/LoongArch64 FP struct classification.
        // Structs with 1-2 floating-point fields can be passed in FP registers.
        throw new NotImplementedException("RISC-V/LoongArch64 FP struct classification is not yet implemented.");
    }

    public bool IsTrivialPointerSizedStruct()
    {
        // Only meaningful on x86 -- this controls whether a value-type arg
        // can be passed in a register. Outside x86 (where structs always go
        // through other paths) we return false so callers ignore us.
        if (Arch != RuntimeInfoArchitecture.X86
            || ExactTypeHandle is null
            || !Rts.IsValueType(ExactTypeHandle))
            return false;

        // Must be exactly pointer-size (4 bytes on x86).
        if (GetSize() != PointerSize)
            return false;

        // Walk instance fields: exactly one, and that field must itself be a
        // pointer-sized primitive (IntPtr/UIntPtr/I/U/Ptr/FnPtr) or another
        // trivial pointer-sized struct. Mirrors crossgen2's
        // ITypeHandle.IsTrivialPointerSizedStruct (ILCompiler.ReadyToRun).
        TargetPointer? singleFieldType = null;
        foreach (TargetPointer fieldDesc in Rts.GetFieldDescList(ExactTypeHandle))
        {
            if (Rts.IsFieldDescStatic(fieldDesc))
                continue;

            if (singleFieldType.HasValue)
                return false;   // more than one instance field

            singleFieldType = fieldDesc;
        }

        if (!singleFieldType.HasValue)
            return false;

        CdacCorElementType fieldType = Rts.GetFieldDescType(singleFieldType.Value);
        switch (fieldType)
        {
            case CdacCorElementType.I:
            case CdacCorElementType.U:
            case CdacCorElementType.I4:
            case CdacCorElementType.U4:
            case CdacCorElementType.Ptr:
            case CdacCorElementType.FnPtr:
                // On x86 pointer-size == 4 bytes, so I4/U4 fit too. Covers
                // enums whose underlying type is Int32/UInt32.
                return true;

            case CdacCorElementType.ValueType:
                SignatureTypeInfo nested;
                try
                {
                    nested = _typeInformation.GetFieldTypeInfo(
                        singleFieldType.Value,
                        _typeInfo);
                }
                catch
                {
                    return false;
                }
                return new CdacTypeHandle(nested, _target, _typeInformation).IsTrivialPointerSizedStruct();

            default:
                return false;
        }
    }

    // Only used by ArgIterator on WASM32 for stack alignment of value types.
    public int GetFieldAlignment()
    {
        throw new NotImplementedException("Field alignment is not yet implemented.");
    }

    /// <summary>
    /// Maps cDAC CorElementType (short names like I4) to the shared CorElementType
    /// (ELEMENT_TYPE_* names). The numeric values are identical, so we cast directly.
    /// </summary>
    private static SharedCorElementType MapCorElementType(CdacCorElementType cdacType)
    {
        return (SharedCorElementType)(int)cdacType;
    }
}
