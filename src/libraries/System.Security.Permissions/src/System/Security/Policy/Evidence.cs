// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class Evidence : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public Evidence() { }
        [System.ObsoleteAttribute("This constructor is obsolete. Please use the constructor which takes arrays of EvidenceBase instead.")]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
        public Evidence(System.Security.Policy.Evidence evidence) { }
        [System.ObsoleteAttribute("Evidence should not be treated as an ICollection. Please use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
        public int Count { get { return 0; } }
        public bool IsReadOnly { get { return false; } }
        public bool IsSynchronized { get { return false; } }
        public bool Locked { get; set; }
        public object SyncRoot { get { return false; } }
        [System.ObsoleteAttribute("This method is obsolete. Please use AddAssemblyEvidence instead.")]
        public void AddAssembly(object id) { }
        [System.ObsoleteAttribute("This method is obsolete. Please use AddHostEvidence instead.")]
        public void AddHost(object id) { }
        public void Clear() { }
        public System.Security.Policy.Evidence Clone() { return default(System.Security.Policy.Evidence); }
        [System.ObsoleteAttribute("Evidence should not be treated as an ICollection. Please use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetAssemblyEnumerator() { return default(System.Collections.IEnumerator); }
        [System.ObsoleteAttribute("GetEnumerator is obsolete. Please use GetAssemblyEnumerator and GetHostEnumerator instead.")]
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public System.Collections.IEnumerator GetHostEnumerator() { return default(System.Collections.IEnumerator); }
        public void Merge(System.Security.Policy.Evidence evidence) { }
        public void RemoveType(System.Type t) { }
    }
}
