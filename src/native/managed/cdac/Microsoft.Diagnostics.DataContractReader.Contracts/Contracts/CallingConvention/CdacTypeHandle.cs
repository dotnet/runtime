// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.CallingConvention;
using Internal.JitInterface;

using CdacCorElementType = Microsoft.Diagnostics.DataContractReader.Contracts.CorElementType;
using SharedCorElementType = Internal.CorConstants.CorElementType;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Adapts cDAC's IRuntimeTypeSystem + TypeHandle to the shared <see cref="ITypeHandle"/>
/// interface used by ArgIterator for calling-convention computation.
/// </summary>
internal readonly struct CdacTypeHandle : ITypeHandle
{
    private readonly TypeHandle _typeHandle;
    private readonly Target _target;

    // Outermost ELEMENT_TYPE_* wrapper (PTR / BYREF / SZARRAY / ARRAY / etc.)
    // recorded out-of-band by the signature wrapper provider in
    // CallingConvention_1.ParamMetadataProvider. Used when the underlying
    // TypeHandle would be null (the runtime hasn't cached the constructed
    // form), in which case Rts.GetSignatureCorElementType would return 0 and
    // ArgIterator would fail to classify the arg for stack-size accounting.
    // `default` (the enum's 0 value, which CorElementType doesn't name) means
    // "no override; ask Rts".
    private readonly CdacCorElementType _kindOverride;

    public CdacTypeHandle(TypeHandle typeHandle, Target target)
        : this(typeHandle, target, kindOverride: default)
    {
    }

    public CdacTypeHandle(TypeHandle typeHandle, Target target, CdacCorElementType kindOverride)
    {
        _typeHandle = typeHandle;
        _target = target;
        _kindOverride = kindOverride;
    }

    private IRuntimeTypeSystem Rts => _target.Contracts.RuntimeTypeSystem;

    public int PointerSize => _target.PointerSize;
    public RuntimeInfoArchitecture Arch => _target.Contracts.RuntimeInfo.GetTargetArchitecture();

    public bool IsNull() => _typeHandle.IsNull && _kindOverride == default;

    public bool IsValueType() => !_typeHandle.IsNull && Rts.IsValueType(_typeHandle);

    public bool IsPointerType()
        => _kindOverride == CdacCorElementType.Ptr
           || (!_typeHandle.IsNull && Rts.IsPointer(_typeHandle));

    public bool HasIndeterminateSize() => false;

    public int GetSize()
    {
        // Constructed pointer/array/byref args always occupy one TADDR slot
        // in the transition block (the actual pointee is reached via the
        // pointer value, not stored inline). When _kindOverride is set, the
        // underlying TypeHandle may be null (uncached PTR), so GetBaseSize
        // would fault.
        if (_kindOverride is CdacCorElementType.Ptr
                          or CdacCorElementType.Byref
                          or CdacCorElementType.SzArray
                          or CdacCorElementType.Array)
        {
            return PointerSize;
        }

        if (_typeHandle.IsNull)
            return 0;

        // GetBaseSize returns the full object size including object header and padding.
        // For value types used in calling convention, we need the unboxed size.
        // BaseSize = ObjHeader + MethodTable* + unboxed fields, aligned to pointer size.
        // Unboxed size = BaseSize - 2 * PointerSize (subtract ObjHeader + MT pointer).
        uint baseSize = Rts.GetBaseSize(_typeHandle);
        return (int)(baseSize - (uint)(2 * PointerSize));
    }

    public SharedCorElementType GetCorElementType()
    {
        if (_kindOverride != default)
            return MapCorElementType(_kindOverride);

        if (_typeHandle.IsNull)
            return (SharedCorElementType)0;

        // Mirror the runtime's MetaSig::PeekArgNormalized -- for value types
        // it resolves the closed TypeHandle and returns
        // MethodTable::GetInternalCorElementType, which collapses enums to
        // their underlying primitive (byte enum -> U1, int enum -> I4, ...).
        // The shared ArgIterator's x86 IsArgumentInRegister relies on this
        // normalization to recognise sub-pointer-size enums as register-
        // passable; returning ELEMENT_TYPE_VALUETYPE for a byte enum makes
        // it fall into the IsTrivialPointerSizedStruct path which then
        // (correctly) rejects it because GetSize() != PointerSize, and the
        // arg gets mis-accounted as stack-passed.
        CdacCorElementType cdacType = Rts.GetInternalCorElementType(_typeHandle);
        return MapCorElementType(cdacType);
    }

    public bool RequiresAlign8()
    {
        return !_typeHandle.IsNull && Rts.RequiresAlign8(_typeHandle);
    }

    public bool IsHomogeneousAggregate()
        => !_typeHandle.IsNull && Rts.TryGetHFAElementSize(_typeHandle, out _);

    public int GetHomogeneousAggregateElementSize()
    {
        Debug.Assert(IsHomogeneousAggregate());
        return Rts.TryGetHFAElementSize(_typeHandle, out int size) ? size : 0;
    }

    public void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor)
    {
        descriptor = default;
        descriptor.passedInRegisters = false;

        if (_typeHandle.IsNull)
            return;

        // Read the runtime-cached classification from the type system; mirrors
        // SystemVRegDescriptorFromSystemVEightByteRegistersInfo in jitinterface.cpp.
        // Only populated on UNIX_AMD64_ABI builds.
        if (!Rts.TryGetSystemVAmd64EightByteClassification(_typeHandle, out SystemVAmd64EightByteClassification info))
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
        if (Arch != RuntimeInfoArchitecture.X86 || _typeHandle.IsNull || !Rts.IsValueType(_typeHandle))
            return false;

        // Must be exactly pointer-size (4 bytes on x86).
        if (GetSize() != PointerSize)
            return false;

        // Walk instance fields: exactly one, and that field must itself be a
        // pointer-sized primitive (IntPtr/UIntPtr/I/U/Ptr/FnPtr) or another
        // trivial pointer-sized struct. Mirrors crossgen2's
        // TypeHandle.IsTrivialPointerSizedStruct (ILCompiler.ReadyToRun).
        TargetPointer? singleFieldType = null;
        foreach (TargetPointer fieldDesc in Rts.GetFieldDescList(_typeHandle))
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
                // Recurse: if the wrapped struct is itself a trivial
                // pointer-sized struct, we are too. Resolve the field's
                // TypeHandle via the field's metadata signature and
                // re-run IsTrivialPointerSizedStruct on it.
                TypeHandle nested = Rts.GetFieldDescApproxTypeHandle(singleFieldType.Value);
                if (nested.IsNull)
                    return false;
                return new CdacTypeHandle(nested, _target).IsTrivialPointerSizedStruct();

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
