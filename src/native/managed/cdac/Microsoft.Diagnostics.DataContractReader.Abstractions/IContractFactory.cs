// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

public interface IContractFactory<out TContract> where TContract : Contracts.IContract
{
    TContract CreateContract(Target target, int version);
}
