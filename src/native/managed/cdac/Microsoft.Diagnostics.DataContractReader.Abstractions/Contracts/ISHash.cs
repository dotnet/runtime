// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;
namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface ITraits<TKey, TEntry>
{
    abstract TKey GetKey(TEntry entry);
    abstract bool Equals(TKey left, TKey right);
    abstract uint Hash(TKey key);
    abstract bool IsNull(TEntry entry);
    abstract TEntry Null();
    abstract bool IsDeleted(TEntry entry);
}

public interface ISHash<TKey, TEntry> where TEntry : IData<TEntry>
{

}

public interface ISHash : IContract
{
    static string IContract.Name { get; } = nameof(SHash);
    public TEntry LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key) where TEntry : IData<TEntry> => throw new NotImplementedException();
    public ISHash<TKey, TEntry> CreateSHash<TKey, TEntry>(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits) where TEntry : IData<TEntry> => throw new NotImplementedException();
}

public readonly struct SHash : ISHash
{
}
