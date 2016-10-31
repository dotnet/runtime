// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Policy
{
    public sealed partial class ApplicationTrustCollection : ICollection, IEnumerable
    {
        internal ApplicationTrustCollection() { }
        public int Count { get { return 0; } }
        public bool IsSynchronized { get { return false; } }
        public ApplicationTrust this[int index] { get { return default(ApplicationTrust); } }
        public ApplicationTrust this[string appFullName] { get { return default(ApplicationTrust); } }
        public object SyncRoot { get { return null; } }
        public int Add(ApplicationTrust trust) { return 0; }
        public void AddRange(ApplicationTrust[] trusts) { }
        public void AddRange(ApplicationTrustCollection trusts) { }
        public void Clear() { }
        public void CopyTo(ApplicationTrust[] array, int index) { }
        public ApplicationTrustEnumerator GetEnumerator() { return new ApplicationTrustEnumerator(); }
        public void Remove(ApplicationTrust trust) { }
        public void RemoveRange(ApplicationTrust[] trusts) { }
        public void RemoveRange(ApplicationTrustCollection trusts) { }
        void ICollection.CopyTo(Array array, int index) { }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
