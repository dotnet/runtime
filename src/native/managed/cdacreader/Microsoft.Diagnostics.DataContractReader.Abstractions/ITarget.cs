// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

internal interface ITarget
{
    int PointerSize { get; }

    TargetPointer ReadGlobalPointer(string global);

    TargetPointer ReadPointer(ulong address);
    public TargetCodePointer ReadCodePointer(ulong address);

    void ReadBuffer(ulong address, Span<byte> buffer);

    /// <summary>
    /// Read a null-terminated UTF-16 string from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>}
    public string ReadUtf16String(ulong address);

    T ReadGlobal<T>(string name) where T : struct, INumber<T>;

    /// <summary>
    /// Read a value from the target in target endianness
    /// </summary>
    /// <typeparam name="T">Type of value to read</typeparam>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    public T Read<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

    public bool IsAlignedToPointerSize(TargetPointer pointer);

    TypeInfo GetTypeInfo(DataType type);

    IDataCache ProcessedData { get; }

    public interface IDataCache
    {
        T GetOrAdd<T>(TargetPointer address) where T : Data.IData<T>;
        bool TryGet<T>(ulong address, [NotNullWhen(true)] out T? data);
    }

    public record struct TypeInfo
    {
        public uint? Size;
        public Dictionary<string, FieldInfo> Fields = [];

        public TypeInfo() { }
    }

    public record struct FieldInfo
    {
        public int Offset;
        public DataType Type;
        public string? TypeName;
    }

    Contracts.IRegistry Contracts { get; }
}
