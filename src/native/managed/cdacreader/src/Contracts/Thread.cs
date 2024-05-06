// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// TODO: [cdac] Add other counts / threads
public record struct ThreadStoreData(
    int ThreadCount,
    TargetPointer FirstThread);

internal sealed class Thread
{
    private readonly Target _target;
    private readonly int _version;
    private readonly TargetPointer _threadStoreAddr;

    public static bool TryCreate(Target target, [NotNullWhen(true)] out Thread? thread)
    {
        thread = default;
        if (!target.TryGetContractVersion(nameof(Thread), out int version))
            return false;

        if (!target.TryReadGlobalPointer(Constants.Globals.ThreadStore, out TargetPointer threadStore))
            return false;

        thread = new Thread(target, version, threadStore);
        return true;
    }

    private Thread(Target target, int version, TargetPointer threadStore)
    {
        _target = target;
        _version = version;
        _threadStoreAddr = threadStore;
    }

    public ThreadStoreData GetThreadStoreData()
    {
        Data.ThreadStore? threadStore;
        if (!_target.ProcessedData.TryGet(_threadStoreAddr.Value, out threadStore))
        {
            threadStore = new Data.ThreadStore(_target, _threadStoreAddr);

            // Still okay if processed data is already registered by someone else
            _ = _target.ProcessedData.TryRegister(_threadStoreAddr.Value, threadStore);
        }

        return new ThreadStoreData(threadStore.ThreadCount, threadStore.FirstThread);
    }
}
