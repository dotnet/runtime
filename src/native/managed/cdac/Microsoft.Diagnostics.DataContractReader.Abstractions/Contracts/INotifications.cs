// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface INotifications : IContract
{
    static string IContract.Name { get; } = nameof(Notifications);
    void SetGcNotification(int condemnedGeneration) => throw new NotImplementedException();
}

public readonly struct Notifications : INotifications
{
    // Everything throws NotImplementedException
}
