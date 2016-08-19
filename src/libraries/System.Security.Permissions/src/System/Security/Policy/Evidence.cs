// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class Evidence : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public Evidence() { }
        [System.ObsoleteAttribute]
        public Evidence(object[] hostEvidence, object[] assemblyEvidence) { }
        public Evidence(System.Security.Policy.Evidence evidence) { }
        [System.ObsoleteAttribute]
        public int Count { get { return default(int); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsSynchronized { get { return default(bool); } }
        public bool Locked { get; set; }
        public object SyncRoot { get { return default(object); } }
        [System.ObsoleteAttribute]
        public void AddAssembly(object id) { }
        [System.ObsoleteAttribute]
        public void AddHost(object id) { }
        public void Clear() { }
        public System.Security.Policy.Evidence Clone() { return default(System.Security.Policy.Evidence); }
        [System.ObsoleteAttribute]
        public void CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetAssemblyEnumerator() { return default(System.Collections.IEnumerator); }
        [System.ObsoleteAttribute]
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public System.Collections.IEnumerator GetHostEnumerator() { return default(System.Collections.IEnumerator); }
        public void Merge(System.Security.Policy.Evidence evidence) { }
        public void RemoveType(System.Type t) { }
    }
}
