// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner
{
    internal interface IExtension : IDisposable
    {
        Task<string> SendMessageAsync(string message);
        Task<bool> ConnectAsync();
    }
}
