// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.AccessControl
{
    // Derive AuthorizationRuleCollection from ICollection since we removed the old ReadOnlyCollectionBase
    // base type and replaced it with this.
    public sealed partial class AuthorizationRuleCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public int Count { get { throw null; } }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
}
