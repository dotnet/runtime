// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class FileIOPermissionAttribute : CodeAccessSecurityAttribute
    {
        public FileIOPermissionAttribute(SecurityAction action) : base(action) { }
        public string Read { get; set; }
        public string Write { get; set; }
        public string Append { get; set; }
        public string PathDiscovery { get; set; }
        public string ViewAccessControl { get; set; }
        public string ChangeAccessControl { get; set; }
        [Obsolete]
        public string All { get; set; }
        public string ViewAndModify { get; set; }
        public FileIOPermissionAccess AllFiles { get; set; }
        public FileIOPermissionAccess AllLocalFiles { get; set; }
        public override IPermission CreatePermission() { return null; }
    }
}