// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    public interface IInternalConfigConfigurationFactory
    {
        Configuration Create(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type typeConfigHost,
            params object[] hostInitConfigurationParams);
        string NormalizeLocationSubPath(string subPath, IConfigErrorInfo errorInfo);
    }
}
