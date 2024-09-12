// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

internal interface IContractFactory<out TProduct> where TProduct : Contracts.IContract
{
    TProduct CreateContract(ITarget target, int version);
}
