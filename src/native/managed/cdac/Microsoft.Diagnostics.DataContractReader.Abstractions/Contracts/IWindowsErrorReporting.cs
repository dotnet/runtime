// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IWindowsErrorReporting : IContract
{
    static string IContract.Name => nameof(WindowsErrorReporting);

    byte[] GetWatsonBuckets(TargetPointer threadPointer) => throw new NotImplementedException();
}

public readonly struct WindowsErrorReporting : IWindowsErrorReporting
{
    // Everything throws NotImplementedException
}
