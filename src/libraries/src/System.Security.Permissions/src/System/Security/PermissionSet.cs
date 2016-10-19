// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;
using System.Collections;
using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    public partial class PermissionSet : ICollection, IEnumerable, IDeserializationCallback, ISecurityEncodable, IStackWalk
    {
        public PermissionSet(PermissionState state) { }
        public PermissionSet(PermissionSet permSet) { }
        public virtual int Count { get { return 0; } }
        public virtual bool IsReadOnly { get { return false; } }
        public virtual bool IsSynchronized { get { return false; } }
        public virtual object SyncRoot { get { return null; } }
        public IPermission AddPermission(IPermission perm) { return default(IPermission); }
        public void Assert() { }
        public bool ContainsNonCodeAccessPermissions() { return false; }
        [Obsolete]
        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { return null; }
        public virtual PermissionSet Copy() { return default(PermissionSet); }
        public virtual void CopyTo(Array array, int index) { }
        public void Demand() { }
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void Deny() { throw new NotSupportedException(); }
        public override bool Equals(object o) => base.Equals(o);
        public virtual void FromXml(SecurityElement et) { }
        public IEnumerator GetEnumerator() { return default(IEnumerator); }
        public override int GetHashCode() => base.GetHashCode();
        public IPermission GetPermission(Type permClass) { return default(IPermission); }
        public PermissionSet Intersect(PermissionSet other) { return default(PermissionSet); }
        public bool IsEmpty() { return false; }
        public bool IsSubsetOf(PermissionSet target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void PermitOnly() { throw new PlatformNotSupportedException(); }
        public IPermission RemovePermission(Type permClass) { return default(IPermission); }
        public static void RevertAssert() { }
        public IPermission SetPermission(IPermission perm) { return default(IPermission); }
        void IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() => base.ToString();
        public virtual SecurityElement ToXml() { return default(SecurityElement); }
        public PermissionSet Union(PermissionSet other) { return default(PermissionSet); }
    }
}
