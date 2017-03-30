// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    public sealed class IsolatedStorageFilePermission : IsolatedStoragePermission
    {
        public IsolatedStorageFilePermission(PermissionState state) : base(state) { }
        public override IPermission Union(IPermission target) { return null; }
        public override bool IsSubsetOf(IPermission target) { return false; }
        public override IPermission Intersect(IPermission target) { return null; }
        public override IPermission Copy() { return null; }
        public override SecurityElement ToXml() { return null; }
    }
}
