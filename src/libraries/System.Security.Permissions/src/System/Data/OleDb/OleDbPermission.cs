// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Security;
using System.Security.Permissions;

namespace System.Data.OleDb
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class OleDbPermission : DBDataPermission
    {
        public OleDbPermission() : base(default(PermissionState)) { }
        public OleDbPermission(PermissionState state) : base(default(PermissionState)) { }
        public OleDbPermission(PermissionState state, bool allowBlankPassword) : base(default(PermissionState)) { }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
        public string Provider { get { return null; } set { } }
        public override IPermission Copy() { return null; }
    }
}
