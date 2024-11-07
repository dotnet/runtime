// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class ExecutionManager_2 : ExecutionManagerBase<NibbleMap_2>
{
    public ExecutionManager_2(Target target, Data.RangeSectionMap topRangeSectionMap) : base(target, topRangeSectionMap)
    {
    }
}
