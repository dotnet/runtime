// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ExecutionManager_2 : ExecutionManagerBase<NibbleMapConstantLookup>
{
    public ExecutionManager_2(Target target, Data.RangeSectionMap topRangeSectionMap) : base(target, topRangeSectionMap)
    {
    }
}
