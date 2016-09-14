// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(109), AllowMultiple = true, Inherited = false)]
    public sealed partial class SecurityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public SecurityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base(default(System.Security.Permissions.SecurityAction)) { }
        public bool Assertion { get; set; }
        public bool BindingRedirects { get; set; }
        public bool ControlAppDomain { get; set; }
        public bool ControlDomainPolicy { get; set; }
        public bool ControlEvidence { get; set; }
        public bool ControlPolicy { get; set; }
        public bool ControlPrincipal { get; set; }
        public bool ControlThread { get; set; }
        public bool Execution { get; set; }
        public System.Security.Permissions.SecurityPermissionFlag Flags { get; set; }
        public bool Infrastructure { get; set; }
        public bool RemotingConfiguration { get; set; }
        public bool SerializationFormatter { get; set; }
        public bool SkipVerification { get; set; }
        public bool UnmanagedCode { get; set; }
        public override System.Security.IPermission CreatePermission() { return default(System.Security.IPermission); }
    }
}
