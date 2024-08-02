// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

public enum DataType
{
    Unknown = 0,

    int8,
    uint8,
    int16,
    uint16,
    int32,
    uint32,
    int64,
    uint64,
    nint,
    nuint,
    pointer,

    GCHandle,
    Thread,
    ThreadStore,
    GCAllocContext,
    Exception,
    ExceptionInfo,
    RuntimeThreadLocals,
    Module,
    MethodTable,
    EEClass,
    ArrayClass,
    MethodTableAuxiliaryData,
    GenericsDictInfo,
    TypeDesc,
    ParamTypeDesc,
    TypeVarTypeDesc,
    FnPtrTypeDesc,
    DynamicMetadata,
    Object,
    String,
    MethodDesc,
    MethodDescChunk,
    Array,
}
