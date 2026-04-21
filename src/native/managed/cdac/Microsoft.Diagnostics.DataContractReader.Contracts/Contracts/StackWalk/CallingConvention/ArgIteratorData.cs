// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from crossgen2's ArgIterator.cs — data holder types.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.CallingConvention;

/// <summary>
/// Describes how a single argument is laid out in registers and/or stack locations.
/// Ported from crossgen2's <c>ArgLocDesc</c>.
/// </summary>
internal struct ArgLocDesc
{
    public int m_idxFloatReg;
    public int m_cFloatReg;

    public int m_idxGenReg;
    public short m_cGenReg;

    public bool m_fRequires64BitAlignment;

    public int m_byteStackIndex;
    public int m_byteStackSize;

    public void Init()
    {
        m_idxFloatReg = -1;
        m_cFloatReg = 0;
        m_idxGenReg = -1;
        m_cGenReg = 0;
        m_byteStackIndex = -1;
        m_byteStackSize = 0;
        m_fRequires64BitAlignment = false;
    }
}

/// <summary>
/// Holds parsed method signature data for <see cref="ArgIterator"/>.
/// Ported from crossgen2's <c>ArgIteratorData</c>.
/// </summary>
internal sealed class ArgIteratorData
{
    private readonly bool _hasThis;
    private readonly bool _isVarArg;
    private readonly ArgTypeInfo[] _parameterTypes;
    private readonly ArgTypeInfo _returnType;

    public ArgIteratorData(
        bool hasThis,
        bool isVarArg,
        ArgTypeInfo[] parameterTypes,
        ArgTypeInfo returnType)
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
