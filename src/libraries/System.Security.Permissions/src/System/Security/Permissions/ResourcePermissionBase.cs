// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Security.Permissions
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public abstract class ResourcePermissionBase : CodeAccessPermission, IUnrestrictedPermission
    {
        public const string Any = "*";
        public const string Local = ".";
        protected ResourcePermissionBase() { }
        protected ResourcePermissionBase(PermissionState state) { }
        private static Hashtable CreateHashtable() { return null; }
        private string ComputerName { get; set; }
        private bool IsEmpty { get; }
        protected Type PermissionAccessType { get; set; }
        protected string[] TagNames { get; set; }
        protected void AddPermissionAccess(ResourcePermissionBaseEntry entry) { }
        protected void Clear() { }
        public override IPermission Copy() { return null; }
        protected ResourcePermissionBaseEntry[] GetPermissionEntries() { return null; }
        public override void FromXml(SecurityElement securityElement) { }
        public override IPermission Intersect(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public bool IsUnrestricted() { return false; }
        protected void RemovePermissionAccess(ResourcePermissionBaseEntry entry) { }
        public override SecurityElement ToXml() { return null; }
        public override IPermission Union(IPermission target) { return null; }
    }
}
