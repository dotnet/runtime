// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader;

public interface ITarget
{
    TargetPointer ReadGlobalPointer(string global);

    TargetPointer ReadPointer(ulong address);

    T ReadGlobal<T>(string name) where T : struct, INumber<T>;

    TypeInfo GetTypeInfo(DataType type);

    IDataCache ProcessedData { get; }

    public interface IDataCache
    {
        T GetOrAdd<T>(TargetPointer address) where T : Data.IData<T>;
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


}
