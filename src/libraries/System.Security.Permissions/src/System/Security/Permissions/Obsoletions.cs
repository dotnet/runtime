// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class Obsoletions
    {
        internal const string SharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

        internal const string PrincipalPermissionAttributeMessage = "PrincipalPermissionAttribute is not honored by the runtime and must not be used.";
        internal const string PrincipalPermissionAttributeDiagId = "SYSLIB0002";

        internal const string CodeAccessSecurityMessage = "Code Access Security is not supported or honored by the runtime.";
        internal const string CodeAccessSecurityDiagId = "SYSLIB0003";
    }
}
