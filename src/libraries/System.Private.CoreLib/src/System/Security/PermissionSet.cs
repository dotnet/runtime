// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Security.Permissions;
using System.Collections;
using System.Runtime.Serialization;

namespace System.Security
{
#if SYSTEM_PRIVATE_CORELIB
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public partial class PermissionSet : ICollection, IEnumerable, IDeserializationCallback, ISecurityEncodable, IStackWalk
    {
        public PermissionSet(PermissionState state) { }
        public PermissionSet(PermissionSet? permSet) { }
        public virtual int Count => 0;
        public virtual bool IsReadOnly => false;
        public virtual bool IsSynchronized => false;
        public virtual object SyncRoot => this;
        public IPermission? AddPermission(IPermission? perm) { return AddPermissionImpl(perm); }
        protected virtual IPermission? AddPermissionImpl(IPermission? perm) { return default; }
        public void Assert() { }
        public bool ContainsNonCodeAccessPermissions() { return false; }
        [Obsolete]
        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
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
        protected virtual IPermission? GetPermissionImpl(Type? permClass) { return default; }
        public PermissionSet? Intersect(PermissionSet? other) { return default; }
        public bool IsEmpty() { return false; }
        public bool IsSubsetOf(PermissionSet? target) { return false; }
        public bool IsUnrestricted() { return false; }
        public void PermitOnly() { throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); }
        public IPermission? RemovePermission(Type? permClass) { return RemovePermissionImpl(permClass); }
        protected virtual IPermission? RemovePermissionImpl(Type? permClass) { return default; }
        public static void RevertAssert() { }
        public IPermission? SetPermission(IPermission? perm) { return SetPermissionImpl(perm); }
        protected virtual IPermission? SetPermissionImpl(IPermission? perm) { return default; }
        void IDeserializationCallback.OnDeserialization(object? sender) { }
        public override string ToString() => base.ToString()!;
        public virtual SecurityElement? ToXml() { return default; }
        public PermissionSet? Union(PermissionSet? other) { return default; }
    }
}
