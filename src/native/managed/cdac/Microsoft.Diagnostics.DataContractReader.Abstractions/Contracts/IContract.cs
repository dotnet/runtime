// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IContract
{
    static virtual string Name => throw new NotImplementedException();

    /// <summary>
    /// Clear any cached data held by this contract.
    /// Called when the target process state may have changed (e.g. on resume).
    /// </summary>
    void Flush() { }
}
