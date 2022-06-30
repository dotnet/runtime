// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.ExtensionsManager
{
    internal class DefaultInprocessExtensionsRegistration : IInprocessExtensionsRegistration
    {
        public IInProcessExtension[] RegisterExtensions()
        {
            List<IInProcessExtension> extensions = new List<IInProcessExtension>();
            foreach (Func<string, Task<string>> runnerToExtensionCallback in TestExtensions.RunnerToExtensionCallbacks)
            {
                extensions.Add(new DefaultInProcessExtension(runnerToExtensionCallback));
            }

            return extensions.ToArray();
        }
    }
}
