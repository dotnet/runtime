// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal enum ArgLocationKind
{
    GpRegister,
    FpRegister,
    Stack,
}

internal readonly struct ArgLocation
{
    public required ArgLocationKind Kind { get; init; }
    public required int TransitionBlockOffset { get; init; }
    public required int Size { get; init; }
    public required CorElementType ElementType { get; init; }
}

internal readonly struct ArgLocDesc
{
    public required CorElementType ArgType { get; init; }
    public required int ArgSize { get; init; }
    public required ArgTypeInfo ArgTypeInfo { get; init; }
    public required bool IsByRef { get; init; }
    public required IReadOnlyList<ArgLocation> Locations { get; init; }
}

internal sealed class ArgIteratorData
{
    private readonly bool _hasThis;
    private readonly bool _isVarArg;
    private readonly ArgTypeInfo[] _parameterTypes;
    private readonly ArgTypeInfo _returnType;

    public ArgIteratorData(bool hasThis, bool isVarArg, ArgTypeInfo[] parameterTypes, ArgTypeInfo returnType)
    {
        _hasThis = hasThis;
        _isVarArg = isVarArg;
        _parameterTypes = parameterTypes;
        _returnType = returnType;
    }

    public bool HasThis() => _hasThis;
    public bool IsVarArg() => _isVarArg;
    public int NumFixedArgs() => _parameterTypes.Length;

    public CorElementType GetArgumentType(int argNum, out ArgTypeInfo thArgType)
    {
        thArgType = _parameterTypes[argNum];
        return thArgType.CorElementType;
    }

    public ArgTypeInfo GetByRefArgumentType(int argNum)
    {
        if (argNum < _parameterTypes.Length && _parameterTypes[argNum].CorElementType == CorElementType.Byref)
            return _parameterTypes[argNum];
        return default;
    }

    public CorElementType GetReturnType(out ArgTypeInfo thRetType)
    {
        thRetType = _returnType;
        return thRetType.CorElementType;
    }
}

internal enum SystemVClassification : byte
{
    Unknown = 0,
    Struct = 1,
    NoClass = 2,
    Memory = 3,
    Integer = 4,
    IntegerReference = 5,
    IntegerByRef = 6,
    SSE = 7,
}

internal struct SystemVStructDescriptor
{
    public const int MaxEightBytes = 2;
    public const int MaxStructBytesToPassInRegisters = 16;
    public const int EightByteSizeInBytes = 8;

    public bool PassedInRegisters;
    public byte EightByteCount;
    public SystemVClassification EightByteClassification0;
    public SystemVClassification EightByteClassification1;
    public byte EightByteSize0;
    public byte EightByteSize1;
    public byte EightByteOffset0;
    public byte EightByteOffset1;

    public SystemVClassification Classification(int index) => index switch
    {
        0 => EightByteClassification0,
        1 => EightByteClassification1,
        _ => throw new System.ArgumentOutOfRangeException(nameof(index)),
    };
}
