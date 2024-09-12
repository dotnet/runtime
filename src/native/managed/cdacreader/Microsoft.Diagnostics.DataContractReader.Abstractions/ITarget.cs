// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Representation of the target under inspection
/// </summary>
/// <remarks>
/// This class provides APIs used by contracts for reading from the target and getting type and globals
/// information based on the target's contract descriptor. Like the contracts themselves in cdacreader,
/// these are throwing APIs. Any callers at the boundaries (for example, unmanaged entry points, COM)
/// should handle any exceptions.
/// </remarks>
internal interface ITarget
{
    int PointerSize { get; }

    TargetPointer ReadGlobalPointer(string global);

    /// <summary>
    /// Read a pointer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Pointer read from the target</returns>}
    TargetPointer ReadPointer(ulong address);
    public TargetCodePointer ReadCodePointer(ulong address);

    void ReadBuffer(ulong address, Span<byte> buffer);

    /// <summary>
    /// Read a null-terminated UTF-8 string from the target
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>}
    public string ReadUtf8String(ulong address);

    /// <summary>
    /// Read a null-terminated UTF-16 string from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>String read from the target</returns>}
    public string ReadUtf16String(ulong address);

    /// <summary>
    /// Read a native unsigned integer from the target in target endianness
    /// </summary>
    /// <param name="address">Address to start reading from</param>
    /// <returns>Value read from the target</returns>
    public TargetNUInt ReadNUInt(ulong address);

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

    public readonly record struct TypeInfo
    {
        public uint? Size { get; init; }
        public readonly IReadOnlyDictionary<string, FieldInfo> Fields
        {
            get;
            init;
        }

        public TypeInfo()
        {
            Fields = new Dictionary<string, FieldInfo>();
        }
    }

    public readonly record struct FieldInfo
    {
        public int Offset {get; init; }
        public readonly DataType Type {get; init;}
        public readonly string? TypeName {get; init; }
    }

    Contracts.IRegistry Contracts { get; }
}
