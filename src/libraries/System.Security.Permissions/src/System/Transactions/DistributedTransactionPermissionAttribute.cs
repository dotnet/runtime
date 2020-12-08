// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security;
using System.Security.Permissions;

namespace System.Transactions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class DistributedTransactionPermissionAttribute : CodeAccessSecurityAttribute
    {
        public DistributedTransactionPermissionAttribute(SecurityAction action) : base(action) { }
        public new bool Unrestricted { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}
