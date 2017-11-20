// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    public interface IInternalConfigHostPaths
    {
        void RefreshConfigPaths();
        bool HasLocalConfig { get; }
        bool HasRoamingConfig { get; }
        bool IsAppConfigHttp { get; }
    }
}
