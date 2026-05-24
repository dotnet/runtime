// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    private readonly RuntimeInfoArchitecture _arch;
    private readonly RuntimeInfoOperatingSystem _os;

    public CdacTypeHandle(TypeHandle typeHandle, Target target)
    {
        _typeHandle = typeHandle;
        _target = target;
        _arch = _target.Contracts.RuntimeInfo.GetTargetArchitecture();
        _os = _target.Contracts.RuntimeInfo.GetTargetOperatingSystem();
    }

    private IRuntimeTypeSystem Rts => _target.Contracts.RuntimeTypeSystem;

    public int PointerSize => _target.PointerSize;

    public bool IsNull() => _typeHandle.IsNull;

    public bool IsValueType() => !_typeHandle.IsNull && Rts.IsValueType(_typeHandle);

    public bool IsPointerType() => !_typeHandle.IsNull && Rts.IsPointer(_typeHandle);

    public bool HasIndeterminateSize() => false;

    public int GetSize()
    {
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
        if (_typeHandle.IsNull)
            return (SharedCorElementType)0;

        CdacCorElementType cdacType = Rts.GetSignatureCorElementType(_typeHandle);
        return MapCorElementType(cdacType);
    }

    public bool RequiresAlign8()
    {
        return !_typeHandle.IsNull && Rts.RequiresAlign8(_typeHandle);
    }

    public bool IsHomogeneousAggregate()
    {
        if (_arch is not RuntimeInfoArchitecture.Arm and not RuntimeInfoArchitecture.Arm64)
            return false;

        // TODO(hfa): Implement HFA detection for ARM/ARM64.
        // See crossgen2 TypeHandle.IsHomogeneousAggregate().
        throw new NotImplementedException("HFA detection for ARM/ARM64 is not yet implemented.");
    }

    public int GetHomogeneousAggregateElementSize()
    {
        if (_arch is not RuntimeInfoArchitecture.Arm and not RuntimeInfoArchitecture.Arm64)
            return 0;

        // TODO(hfa): Return 4 for float HFA, 8 for double HFA, 16 for Vector128 HFA.
        throw new NotImplementedException("HFA element size for ARM/ARM64 is not yet implemented.");
    }

    public void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor)
    {
        throw new NotImplementedException("SystemV AMD64 struct-in-registers is not yet supported by the cDAC.");
    }

    public FpStructInRegistersInfo GetFpStructInRegistersInfo(Internal.TypeSystem.TargetArchitecture architecture)
    {
        // TODO(riscv-loongarch): Implement RISC-V/LoongArch64 FP struct classification.
        // Structs with 1-2 floating-point fields can be passed in FP registers.
        throw new NotImplementedException("RISC-V/LoongArch64 FP struct classification is not yet implemented.");
    }

    public bool IsTrivialPointerSizedStruct()
    {
        // TODO(x86): Implement for x86 register passing.
        // A trivial pointer-sized struct (exactly pointer-size, one field, no GC refs)
        // can be passed in a register on x86. See crossgen2 TypeHandle.IsTrivialPointerSizedStruct.
        throw new NotImplementedException("Trivial pointer-sized struct detection for x86 is not yet implemented.");
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
