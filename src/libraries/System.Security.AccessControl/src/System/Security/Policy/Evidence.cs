// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace System.Security.Policy
{
    public sealed partial class Evidence : ICollection, IEnumerable
    {
        public Evidence() { }
        [Obsolete("This constructor is obsolete. Use the constructor which accepts arrays of EvidenceBase instead.")]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
        public Evidence(Evidence evidence) { }
        public Evidence(EvidenceBase[] hostEvidence, EvidenceBase[] assemblyEvidence) { }
        [Obsolete("Evidence should not be treated as an ICollection. Use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
        public int Count { get { return 0; } }
        public bool IsReadOnly { get { return false; } }
        public bool IsSynchronized { get { return false; } }
        public bool Locked { get; set; }
        public object SyncRoot { get { return false; } }
        [Obsolete("Evidence.AddAssembly has been deprecated. Use AddAssemblyEvidence instead.")]
        public void AddAssembly(object id) { }
        public void AddAssemblyEvidence<T>(T evidence) where T : EvidenceBase { }
        public void AddHostEvidence<T>(T evidence) where T : EvidenceBase { }
        public T? GetAssemblyEvidence<T>() where T : EvidenceBase { return default(T); }
        public T? GetHostEvidence<T>() where T : EvidenceBase { return default(T); }
        [Obsolete("Evidence.AddHost has been deprecated. Use AddHostEvidence instead.")]
        public void AddHost(object id) { }
        public void Clear() { }
        public Evidence? Clone() { return default(Evidence); }
        [Obsolete("Evidence should not be treated as an ICollection. Use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
        public void CopyTo(Array array, int index) { }
        public IEnumerator GetAssemblyEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        [Obsolete("GetEnumerator is obsolete. Use GetAssemblyEnumerator and GetHostEnumerator instead.")]
        public IEnumerator GetEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        public IEnumerator GetHostEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        public void Merge(Evidence evidence) { }
        public void RemoveType(Type t) { }
    }
}
