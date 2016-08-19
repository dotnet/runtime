// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(4205), AllowMultiple = true, Inherited = false)]
    public sealed partial class HostProtectionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public HostProtectionAttribute() : base(default(System.Security.Permissions.SecurityAction)) { }
        public HostProtectionAttribute(System.Security.Permissions.SecurityAction action) : base(default(System.Security.Permissions.SecurityAction)) { }
        public bool ExternalProcessMgmt { get; set; }
        public bool ExternalThreading { get; set; }
        public bool MayLeakOnAbort { get; set; }
        public System.Security.Permissions.HostProtectionResource Resources { get; set; }
        public bool SecurityInfrastructure { get; set; }
        public bool SelfAffectingProcessMgmt { get; set; }
        public bool SelfAffectingThreading { get; set; }
        public bool SharedState { get; set; }
        public bool Synchronization { get; set; }
        public bool UI { get; set; }
        public override System.Security.IPermission CreatePermission() { return default(System.Security.IPermission); }
    }
}
