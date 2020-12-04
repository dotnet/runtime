// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class KeyContainerPermissionAttribute : CodeAccessSecurityAttribute
    {
        public KeyContainerPermissionAttribute(SecurityAction action) : base(action) { }
        public string KeyStore { get; set; }
        public string ProviderName { get; set; }
        public int ProviderType { get; set; }
        public string KeyContainerName { get; set; }
        public int KeySpec { get; set; }
        public KeyContainerPermissionFlags Flags { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}
