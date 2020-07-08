// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration.Internal
{
    public interface IConfigSystem
    {
        IInternalConfigHost Host { get; }
        IInternalConfigRoot Root { get; }
        void Init(Type typeConfigHost, params object[] hostInitParams);
    }
}
