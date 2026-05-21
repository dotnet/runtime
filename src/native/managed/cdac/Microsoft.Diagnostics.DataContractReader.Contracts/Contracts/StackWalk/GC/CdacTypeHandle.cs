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

    public CdacTypeHandle(TypeHandle typeHandle, Target target)
    {
        _typeHandle = typeHandle;
        _target = target;
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
        // TODO: Implement HFA detection when needed for ARM/ARM64
        return false;
    }

    public int GetHomogeneousAggregateElementSize()
    {
        return 0;
    }

    public void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor)
    {
        // TODO: Implement SystemV AMD64 struct classification when needed
        descriptor = default;
        descriptor.passedInRegisters = false;
    }

    public FpStructInRegistersInfo GetFpStructInRegistersInfo(Internal.TypeSystem.TargetArchitecture architecture)
    {
        // TODO: Implement RISC-V/LoongArch64 FP struct classification when needed
        return default;
    }

    public bool IsTrivialPointerSizedStruct()
    {
        // TODO: Implement for x86 register passing when needed
        return false;
    }

    public int GetFieldAlignment()
    {
        // Default to pointer size alignment
        return PointerSize;
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
