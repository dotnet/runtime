// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration.Internal
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    internal sealed class InternalConfigConfigurationFactory : IInternalConfigConfigurationFactory
    {
        private InternalConfigConfigurationFactory() { }

        Configuration IInternalConfigConfigurationFactory.Create(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type typeConfigHost,
            params object[] hostInitConfigurationParams)
        {
            return new Configuration(null, typeConfigHost, hostInitConfigurationParams);
        }

        string IInternalConfigConfigurationFactory.NormalizeLocationSubPath(string subPath, IConfigErrorInfo errorInfo)
        {
            return BaseConfigurationRecord.NormalizeLocationSubPath(subPath, errorInfo);
        }
    }
}
