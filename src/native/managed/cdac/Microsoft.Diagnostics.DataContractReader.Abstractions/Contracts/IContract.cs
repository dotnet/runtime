// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IContract
{
    static virtual string Name => throw new NotImplementedException();

    /// <summary>
    /// Clear cached data held by this contract for the given <paramref name="scope"/>.
    /// Called when the target process state may have changed (e.g. on resume) or as
    /// part of a stress-harness re-read of live target state.
    /// <para>
    /// Default implementation is a no-op. Contracts that maintain caches or capture
    /// target-memory snapshots at construction must override this method and handle
    /// each <see cref="FlushScope"/> value appropriately.
    /// </para>
    /// </summary>
    void Flush(FlushScope scope) { }
}
