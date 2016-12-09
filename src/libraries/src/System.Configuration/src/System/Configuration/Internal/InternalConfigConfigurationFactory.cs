// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ClassConfiguration = System.Configuration.Configuration;

namespace System.Configuration.Internal
{
    internal sealed class InternalConfigConfigurationFactory : IInternalConfigConfigurationFactory
    {
        private InternalConfigConfigurationFactory() { }

        ClassConfiguration IInternalConfigConfigurationFactory.Create(Type typeConfigHost,
            params object[] hostInitConfigurationParams)
        {
            return new ClassConfiguration(null, typeConfigHost, hostInitConfigurationParams);
        }

        // Normalize a locationSubpath argument
        string IInternalConfigConfigurationFactory.NormalizeLocationSubPath(string subPath, IConfigErrorInfo errorInfo)
        {
            return BaseConfigurationRecord.NormalizeLocationSubPath(subPath, errorInfo);
        }
    }
}