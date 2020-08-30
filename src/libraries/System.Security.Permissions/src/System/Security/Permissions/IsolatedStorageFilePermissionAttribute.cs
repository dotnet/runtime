// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor
     | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly,
    AllowMultiple = true, Inherited = false)]
    public sealed class IsolatedStorageFilePermissionAttribute : IsolatedStoragePermissionAttribute
    {
        public IsolatedStorageFilePermissionAttribute(SecurityAction action) : base(action) { }
        public override IPermission CreatePermission() { return null; }
    }

}
