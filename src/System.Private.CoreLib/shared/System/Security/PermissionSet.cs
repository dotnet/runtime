// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Security.Permissions;
using System.Collections;
using System.Runtime.Serialization;

namespace System.Security
{
#if PROJECTN
    [Internal.Runtime.CompilerServices.RelocatedType("System.Runtime.Extensions")]
#endif
    public partial class PermissionSet : ICollection, IEnumerable, IDeserializationCallback, ISecurityEncodable, IStackWalk
    {
        public PermissionSet(PermissionState state) { }
        public PermissionSet(PermissionSet? permSet) { }
        public virtual int Count { get { return 0; } }
        public virtual bool IsReadOnly { get { return false; } }
        public virtual bool IsSynchronized { get { return false; } }
        public virtual object SyncRoot { get { return this; } }
        public IPermission? AddPermission(IPermission? perm) { return AddPermissionImpl(perm); }
        protected virtual IPermission? AddPermissionImpl(IPermission? perm) { return default(IPermission); }
        public void Assert() { }
        public bool ContainsNonCodeAccessPermissions() { return false; }
        [Obsolete]
        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { throw new NotImplementedException(); }
        public virtual PermissionSet Copy() { return new PermissionSet(this); }
        public virtual void CopyTo(Array array, int index) { }
        public void Demand() { }
        [Obsolete]
        public void Deny() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public override bool Equals(object? o) => base.Equals(o);
        public virtual void FromXml(SecurityElement et) { }
        public IEnumerator GetEnumerator() { return GetEnumeratorImpl(); }
        protected virtual IEnumerator GetEnumeratorImpl() { return Array.Empty<object>().GetEnumerator(); }
        public override int GetHashCode() => base.GetHashCode();
        public IPermission? GetPermission(Type? permClass) { return GetPermissionImpl(permClass); }
        protected virtual IPermission? GetPermissionImpl(Type? permClass) { return default(IPermission); }
        public PermissionSet? Intersect(PermissionSet? other) { return default(PermissionSet); }
        public bool IsEmpty() { return false; }
        public bool IsSubsetOf(PermissionSet? target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void PermitOnly() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public IPermission? RemovePermission(Type? permClass) { return RemovePermissionImpl(permClass); }
        protected virtual IPermission? RemovePermissionImpl(Type? permClass) { return default(IPermission); }
        public static void RevertAssert() { }
        public IPermission? SetPermission(IPermission? perm) { return SetPermissionImpl(perm); }
        protected virtual IPermission? SetPermissionImpl(IPermission? perm) { return default(IPermission); }
        void IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() => base.ToString()!;
        public virtual SecurityElement? ToXml() { return default(SecurityElement); }
        public PermissionSet? Union(PermissionSet? other) { return default(PermissionSet); }
    }
}
