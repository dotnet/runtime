// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

internal interface IContractFactory<ISelf, IProduct> where IProduct : Contracts.IContract where ISelf : IContractFactory<ISelf, IProduct>
{
    static virtual IProduct CreateContract(ITarget target, int version) => throw new NotImplementedException();
}

internal interface IContractFactory<ISelf> : Contracts.IContract, IContractFactory<ISelf, ISelf> where ISelf : IContractFactory<ISelf>
{
    static ISelf IContractFactory<ISelf, ISelf>.CreateContract(ITarget target, int version) => ISelf.Create(target, version);
    static virtual ISelf Create(ITarget target, int version) => throw new NotImplementedException();
}
