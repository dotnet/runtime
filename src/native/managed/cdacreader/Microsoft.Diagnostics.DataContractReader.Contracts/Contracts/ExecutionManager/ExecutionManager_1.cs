// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class ExecutionManager_1 : ExecutionManagerBase<NibbleMap_1>
{
    public ExecutionManager_1(Target target, Data.RangeSectionMap topRangeSectionMap) : base(target, topRangeSectionMap)
    {
    }
}
