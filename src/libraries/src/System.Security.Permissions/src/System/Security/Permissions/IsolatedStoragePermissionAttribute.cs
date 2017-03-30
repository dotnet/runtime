// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public abstract class IsolatedStoragePermissionAttribute : CodeAccessSecurityAttribute
    {
        protected IsolatedStoragePermissionAttribute(SecurityAction action) : base(action) { }
        public long UserQuota { get; set; }
        public IsolatedStorageContainment UsageAllowed { get; set; }
    }
}
