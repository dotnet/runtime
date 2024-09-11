// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

internal interface IContractFactory<IProduct> where IProduct : Contracts.IContract
{
    static virtual IProduct CreateContract(ITarget target, int version) => throw new NotImplementedException();
}
