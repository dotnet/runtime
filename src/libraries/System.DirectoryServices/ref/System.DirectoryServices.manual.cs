// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.DirectoryServices.Design;
using System.Runtime.CompilerServices;

#if NETSTANDARD2_0
// These types (DirectoryServicesPermission, etc) were originally implemented in System.DirectoryServices.dll but have been
// moved and type forwarded to System.Security.Permissions in NetCore but not NetFx since they are implemented in NetFx's version
// of System.DirectoryServices.dll.
namespace System.DirectoryServices
{
    [System.ObsoleteAttribute("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class DirectoryServicesPermission : System.Security.Permissions.ResourcePermissionBase
    {
        public DirectoryServicesPermission() { }
        public DirectoryServicesPermission(System.DirectoryServices.DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public DirectoryServicesPermission(System.DirectoryServices.DirectoryServicesPermissionEntry[] permissionAccessEntries) { }
        public DirectoryServicesPermission(System.Security.Permissions.PermissionState state) { }
        public System.DirectoryServices.DirectoryServicesPermissionEntryCollection PermissionEntries { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum DirectoryServicesPermissionAccess
    {
        None = 0,
        Browse = 2,
        Write = 6,
    }
    [System.ObsoleteAttribute("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Event | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public partial class DirectoryServicesPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public DirectoryServicesPermissionAttribute(System.Security.Permissions.SecurityAction action) : base(default(System.Security.Permissions.SecurityAction)) { }
        public string Path { get { throw null; } set { } }
        public System.DirectoryServices.DirectoryServicesPermissionAccess PermissionAccess { get { throw null; } set { } }
        public override System.Security.IPermission CreatePermission() { throw null; }
    }
    public partial class DirectoryServicesPermissionEntry
    {
        public DirectoryServicesPermissionEntry(System.DirectoryServices.DirectoryServicesPermissionAccess permissionAccess, string path) { }
        public string Path { get { throw null; } }
        public System.DirectoryServices.DirectoryServicesPermissionAccess PermissionAccess { get { throw null; } }
    }
    public partial class DirectoryServicesPermissionEntryCollection : System.Collections.CollectionBase
    {
        internal DirectoryServicesPermissionEntryCollection() { }
        public System.DirectoryServices.DirectoryServicesPermissionEntry this[int index] { get { throw null; } set { } }
        public int Add(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void AddRange(System.DirectoryServices.DirectoryServicesPermissionEntryCollection value) { }
        public void AddRange(System.DirectoryServices.DirectoryServicesPermissionEntry[] value) { }
        public bool Contains(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void CopyTo(System.DirectoryServices.DirectoryServicesPermissionEntry[] array, int index) { }
        public int IndexOf(System.DirectoryServices.DirectoryServicesPermissionEntry value) { throw null; }
        public void Insert(int index, System.DirectoryServices.DirectoryServicesPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(System.DirectoryServices.DirectoryServicesPermissionEntry value) { }
    }
}
#else
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermission))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionAccess))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionAttribute))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionEntry))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionEntryCollection))]
#endif

namespace System.DirectoryServices
{
    [System.ComponentModel.TypeConverter(typeof(DirectoryEntryConverter))]
    public partial class DirectoryEntry { }
}

namespace System.DirectoryServices.Design
{
    internal sealed class DirectoryEntryConverter { }
}
