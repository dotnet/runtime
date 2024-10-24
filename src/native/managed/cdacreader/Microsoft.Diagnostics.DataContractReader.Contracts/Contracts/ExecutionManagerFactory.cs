// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ExecutionManagerFactory : IContractFactory<IExecutionManager>
{
    IExecutionManager IContractFactory<IExecutionManager>.CreateContract(Target target, int version)
    {
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);
        return version switch
        {
            1 => new ExecutionManager_1(target, rangeSectionMap),
            _ => default(ExecutionManager),
        };
    }
}
