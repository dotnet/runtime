// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#nullable enable

namespace System.Security.Policy
{
    public sealed partial class Evidence : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public Evidence() { }
        [System.ObsoleteAttribute("This constructor is obsolete. Please use the constructor which takes arrays of EvidenceBase instead.")]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
        public Evidence(System.Security.Policy.Evidence evidence) { }
        public Evidence(System.Security.Policy.EvidenceBase[] hostEvidence, System.Security.Policy.EvidenceBase[] assemblyEvidence) { }
        [System.ObsoleteAttribute("Evidence should not be treated as an ICollection. Please use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public bool Locked { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        [System.ObsoleteAttribute("This method is obsolete. Please use AddAssemblyEvidence instead.")]
        public void AddAssembly(object id) { }
        public void AddAssemblyEvidence<T>(T evidence) where T : System.Security.Policy.EvidenceBase { }
        [System.ObsoleteAttribute("This method is obsolete. Please use AddHostEvidence instead.")]
        public void AddHost(object id) { }
        public void AddHostEvidence<T>(T evidence) where T : System.Security.Policy.EvidenceBase { }
        public void Clear() { }
        public System.Security.Policy.Evidence? Clone() { throw null; }
        [System.ObsoleteAttribute("Evidence should not be treated as an ICollection. Please use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetAssemblyEnumerator() { throw null; }
        public T? GetAssemblyEvidence<T>() where T : System.Security.Policy.EvidenceBase { throw null; }
        [System.ObsoleteAttribute("GetEnumerator is obsolete. Please use GetAssemblyEnumerator and GetHostEnumerator instead.")]
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public System.Collections.IEnumerator GetHostEnumerator() { throw null; }
        public T? GetHostEvidence<T>() where T : System.Security.Policy.EvidenceBase { throw null; }
        public void Merge(System.Security.Policy.Evidence evidence) { }
        public void RemoveType(System.Type t) { }
    }
    public abstract partial class EvidenceBase
    {
        protected EvidenceBase() { }
        public virtual System.Security.Policy.EvidenceBase? Clone() { throw null; }
    }
}