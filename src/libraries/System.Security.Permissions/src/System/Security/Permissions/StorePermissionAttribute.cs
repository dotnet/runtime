// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class StorePermissionAttribute : CodeAccessSecurityAttribute
    {
        public StorePermissionAttribute(SecurityAction action) : base(action) { }
        public StorePermissionFlags Flags { get; set; }
        public bool CreateStore { get; set; }
        public bool DeleteStore { get; set; }
        public bool EnumerateStores { get; set; }
        public bool OpenStore { get; set; }
        public bool AddToStore { get; set; }
        public bool RemoveFromStore { get; set; }
        public bool EnumerateCertificates { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}
