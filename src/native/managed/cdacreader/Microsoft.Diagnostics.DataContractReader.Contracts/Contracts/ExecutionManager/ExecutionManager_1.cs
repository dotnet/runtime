// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ExecutionManager_1 : ExecutionManagerBase<NibbleMapLinearLookup>
{
    public ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap) : base(target, topRangeSectionMap)
    {
    }
}
