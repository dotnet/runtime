// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.ExtensionsManager
{
    internal class DefaultExtensionsManager : IExtensionsManager
    {
        private readonly IInprocessExtensionsRegistration _inprocessExtensionsRegistration;

        public DefaultExtensionsManager(IInprocessExtensionsRegistration inprocessExtensionsRegistration)
        {
            _inprocessExtensionsRegistration = inprocessExtensionsRegistration;
        }

        public async Task<IExtension[]> GetInProcessExtensionsAsync()
        {
            List<IExtension> validInProcessExtension = new();
            foreach (IInProcessExtension inProcExtension in _inprocessExtensionsRegistration.RegisterExtensions())
            {
                if (await inProcExtension.ConnectAsync())
                {
                    validInProcessExtension.Add(inProcExtension);
                }
                else
                {
                    inProcExtension.Dispose();
                }
            }

            return validInProcessExtension.ToArray();
        }
    }
}
