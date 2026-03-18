// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExecutionManagerFactory : IContractFactory<IExecutionManager>
{
    IExecutionManager IContractFactory<IExecutionManager>.CreateContract(Target target, int version)
    {
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);
        return version switch
        {
            1 => new ExecutionManager_1(target, rangeSectionMap),

            // The nibblemap algorithm was changed in version 2
            2 => new ExecutionManager_2(target, rangeSectionMap),
            _ => default(ExecutionManager),
        };
    }
}
