// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.ExtensionsManager
{
    internal class DefaultInProcessExtension : IInProcessExtension
    {
        private readonly Func<string, Task<string>> _runnerToExtensionCallback;

        public DefaultInProcessExtension(Func<string, Task<string>> runnerToExtensionCallback) => _runnerToExtensionCallback = runnerToExtensionCallback;
        public Task<string> SendMessageAsync(string message) => _runnerToExtensionCallback(message);
        public void Dispose() { }
        public Task<bool> ConnectAsync() => Task.FromResult(true);
    }
}
