// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Permissions;

namespace System.Data.OracleClient
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class OraclePermissionAttribute : CodeAccessSecurityAttribute
    {
        public OraclePermissionAttribute(SecurityAction action) : base(action) { }
        public bool AllowBlankPassword { get; set; }
        public string ConnectionString { get { return null; } set { } }
        public KeyRestrictionBehavior KeyRestrictionBehavior { get; set; }
        public string KeyRestrictions { get { return null; } set { } }
        public override Security.IPermission CreatePermission() { return null; }
        [System.ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeConnectionString() => false;
        [System.ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
        public bool ShouldSerializeKeyRestrictions() => false;
    }
}
