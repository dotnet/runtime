// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Security.Permissions;

namespace System.Data.SqlClient
{
    public sealed class SqlClientPermission : DBDataPermission
    {
        public SqlClientPermission() : base(default(PermissionState)) { }
        public SqlClientPermission(PermissionState state) : base(default(PermissionState)) { }
        public SqlClientPermission(PermissionState state, bool allowBlankPassword) : base(default(PermissionState)) { }
        public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior) { }
        public override System.Security.IPermission Copy() { return null; }
    }
}