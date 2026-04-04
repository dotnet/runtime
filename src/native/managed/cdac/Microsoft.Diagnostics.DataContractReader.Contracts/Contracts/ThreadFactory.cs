// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ThreadFactory : IContractFactory<IThread>
{
    IThread IContractFactory<IThread>.CreateContract(Target target, int version)
    {
        TargetPointer threadStorePointer = target.ReadGlobalPointer(Constants.Globals.ThreadStore);
        TargetPointer threadStore = target.ReadPointer(threadStorePointer);
        return version switch
        {
            1 => new Thread_1(target, threadStore),
            _ => default(Thread),
        };
    }
}
