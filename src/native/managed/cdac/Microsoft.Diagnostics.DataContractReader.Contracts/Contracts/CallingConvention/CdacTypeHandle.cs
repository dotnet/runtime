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

    // Outermost ELEMENT_TYPE_* wrapper (PTR / BYREF / SZARRAY / ARRAY / etc.)
    // recorded out-of-band by the signature wrapper provider in
    // CallingConvention_1.ParamMetadataProvider. Used when the underlying
    // TypeHandle would be null (the runtime hasn't cached the constructed
    // form), in which case Rts.GetSignatureCorElementType would return 0 and
    // ArgIterator would fail to classify the arg for stack-size accounting.
    // CdacCorElementType.End (== default) means "no override; ask Rts".
    private readonly CdacCorElementType _kindOverride;

    public CdacTypeHandle(TypeHandle typeHandle, Target target)
        : this(typeHandle, target, kindOverride: default)
    {
    }

    public CdacTypeHandle(TypeHandle typeHandle, Target target, CdacCorElementType kindOverride)
    {
        _typeHandle = typeHandle;
        _target = target;
        _arch = _target.Contracts.RuntimeInfo.GetTargetArchitecture();
        _os = _target.Contracts.RuntimeInfo.GetTargetOperatingSystem();
        _kindOverride = kindOverride;
    }

    private IRuntimeTypeSystem Rts => _target.Contracts.RuntimeTypeSystem;

    public int PointerSize => _target.PointerSize;

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
        // Only meaningful on x86 -- this controls whether a value-type arg
        // can be passed in a register. Outside x86 (where structs always go
        // through other paths) we return false so callers ignore us.
        if (_arch != RuntimeInfoArchitecture.X86 || _typeHandle.IsNull || !Rts.IsValueType(_typeHandle))
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
                // pointer-sized struct, we are too. cDAC's GetFieldDescType
                // doesn't directly hand us the nested TypeHandle, so we
                // can't follow this chain without more API. Conservative
                // fallback: report false. The relevant runtime cases
                // (e.g. IntPtr inside a single-field struct) collapse to
                // the primitive checks above for most reachable types.
                return false;

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
