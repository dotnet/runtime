// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Security;
using System.Security.Permissions;

namespace System.Data.Odbc
{
#if NET5_0_OR_GREATER
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class OdbcPermission : DBDataPermission
    {
        public OdbcPermission() : base(default(PermissionState)) { }
        public OdbcPermission(PermissionState state) : base(default(PermissionState)) { }
        public OdbcPermission(PermissionState state, bool allowBlankPassword) : base(default(PermissionState)) { }
        public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior) { }
        public override IPermission Copy() { return null; }
    }
}
