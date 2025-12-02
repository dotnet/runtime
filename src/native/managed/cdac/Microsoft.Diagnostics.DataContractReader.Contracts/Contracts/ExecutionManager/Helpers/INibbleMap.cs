// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Diagnostics;
using System;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal interface INibbleMap
{
    public static abstract INibbleMap Create(Target target);

    public TargetPointer FindMethodCode(Data.CodeHeapListNode heapListNode, TargetCodePointer jittedCodeAddress);
}
