// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

public interface IData<TSelf> where TSelf : IData<TSelf>
{
    static abstract TSelf Create(Target target, TargetPointer address);
}

/// <summary>
/// Implemented by generated IData types whose fields are read lazily. Forces
/// every field to be materialized (read from the target) so that a caller can
/// eagerly validate that the entire structure is readable. A field that cannot
/// be read throws <see cref="VirtualReadException"/>.
/// </summary>
public interface IReadableData
{
    void EnsureAllFieldsRead();
}
