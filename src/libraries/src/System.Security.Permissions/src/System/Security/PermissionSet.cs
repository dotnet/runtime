// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial class PermissionSet : System.Collections.ICollection, System.Collections.IEnumerable, /* System.Runtime.Serialization.IDeserializationCallback, */ System.Security.ISecurityEncodable, System.Security.IStackWalk
    {
        public PermissionSet(System.Security.Permissions.PermissionState state) { }
        public PermissionSet(System.Security.PermissionSet permSet) { }
        public virtual int Count { get { return default(int); } }
        public virtual bool IsReadOnly { get { return default(bool); } }
        public virtual bool IsSynchronized { get { return default(bool); } }
        public virtual object SyncRoot { get { return default(object); } }
        public System.Security.IPermission AddPermission(System.Security.IPermission perm) { return default(System.Security.IPermission); }
        public void Assert() { }
        public bool ContainsNonCodeAccessPermissions() { return default(bool); }
        [System.ObsoleteAttribute]
        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { return default(byte[]); }
        public virtual System.Security.PermissionSet Copy() { return default(System.Security.PermissionSet); }
        public virtual void CopyTo(System.Array array, int index) { }
        public void Demand() { }
        [System.ObsoleteAttribute]
        public void Deny() { throw new System.NotSupportedException(); }
        public override bool Equals(object o) => base.Equals(o);
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override int GetHashCode() => base.GetHashCode();
        public System.Security.IPermission GetPermission(System.Type permClass) { return default(System.Security.IPermission); }
        public System.Security.PermissionSet Intersect(System.Security.PermissionSet other) { return default(System.Security.PermissionSet); }
        public bool IsEmpty() { return default(bool); }
        public bool IsSubsetOf(System.Security.PermissionSet target) { return default(bool); }
        public bool IsUnrestricted() { return default(bool); }
        public void PermitOnly() { throw new System.PlatformNotSupportedException(); }
        public System.Security.IPermission RemovePermission(System.Type permClass) { return default(System.Security.IPermission); }
        public static void RevertAssert() { }
        public System.Security.IPermission SetPermission(System.Security.IPermission perm) { return default(System.Security.IPermission); }
        public override string ToString() => base.ToString();
        public System.Security.PermissionSet Union(System.Security.PermissionSet other) { return default(System.Security.PermissionSet); }
    }
}
