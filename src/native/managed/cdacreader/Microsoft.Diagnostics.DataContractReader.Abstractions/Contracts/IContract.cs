// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IContract
{
    static virtual string Name => throw new NotImplementedException();
}

internal interface IContractFactory<ISelf, IProduct> : IContract where IProduct : IContract where ISelf : IProduct, IContractFactory<ISelf, IProduct>
{
    static virtual IProduct CreateContract(ITarget target, int version) => throw new NotImplementedException();
}

internal interface IContractFactory<ISelf> : IContractFactory<ISelf, ISelf> where ISelf : IContractFactory<ISelf>
{
    static ISelf IContractFactory<ISelf, ISelf>.CreateContract(ITarget target, int version) => ISelf.Create(target, version);
    static virtual ISelf Create(ITarget target, int version) => throw new NotImplementedException();
}
