// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IContract
{
    static virtual string Name => throw new NotImplementedException();
    static virtual IContract Create(Target target, int version) => throw new NotImplementedException();
}
