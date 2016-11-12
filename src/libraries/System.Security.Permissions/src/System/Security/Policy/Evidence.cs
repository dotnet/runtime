// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Policy
{
    [Serializable]
    public sealed partial class Evidence : ICollection, IEnumerable
    {
        public Evidence() { }
        [Obsolete("This constructor is obsolete. Please use the constructor which takes arrays of EvidenceBase instead.")]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
        public Evidence(Evidence evidence) { }
        [Obsolete("Evidence should not be treated as an ICollection. Please use GetHostEnumerator and GetAssemblyEnumerator to iterate over the evidence to collect a count.")]
        public int Count { get { return 0; } }
        public bool IsReadOnly { get { return false; } }
        public bool IsSynchronized { get { return false; } }
        public bool Locked { get; set; }
        public object SyncRoot { get { return false; } }
        [Obsolete("This method is obsolete. Please use AddAssemblyEvidence instead.")]
        public void AddAssembly(object id) { }
        [Obsolete("This method is obsolete. Please use AddHostEvidence instead.")]
        public void AddHost(object id) { }
        public void Clear() { }
        public Evidence Clone() { return default(Evidence); }
        [Obsolete("Evidence should not be treated as an ICollection. Please use the GetHostEnumerator and GetAssemblyEnumerator methods rather than using CopyTo.")]
        public void CopyTo(Array array, int index) { }
        public IEnumerator GetAssemblyEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        [Obsolete("GetEnumerator is obsolete. Please use GetAssemblyEnumerator and GetHostEnumerator instead.")]
        public IEnumerator GetEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        public IEnumerator GetHostEnumerator() { return Array.Empty<object>().GetEnumerator(); }
        public void Merge(Evidence evidence) { }
        public void RemoveType(Type t) { }
    }
}
