// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration.Internal
{
    public interface IConfigSystem
    {
        IInternalConfigHost Host { get; }
        IInternalConfigRoot Root { get; }
        void Init(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type typeConfigHost,
            params object[] hostInitParams);
    }
}
