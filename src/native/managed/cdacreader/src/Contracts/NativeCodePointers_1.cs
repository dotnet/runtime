// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct NativeCodePointers_1 : INativeCodePointers
{
    private readonly Target _target;

    public NativeCodePointers_1(Target target)
    {
        _target = target;
    }
}
