// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

// Additional implementation to keep public API in Mono (it has a reference to this file)
// https://github.com/dotnet/runtime/issues/20232

namespace System.Data.Common
{
    partial class DbProviderFactory
    {
        public virtual CodeAccessPermission CreatePermission(PermissionState state) => null;
    }
}
