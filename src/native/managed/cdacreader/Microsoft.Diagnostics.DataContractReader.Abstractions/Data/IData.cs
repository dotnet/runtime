// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal interface IData<TSelf> where TSelf : IData<TSelf>
{
    static abstract TSelf Create(Target target, TargetPointer address);
}
