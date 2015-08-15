// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security.AccessControl
{
    // Derive AuthorizationRuleCollection from ICollection since we removed the old ReadOnlyCollectionBase
    // base type and replaced it with this.
    public sealed partial class AuthorizationRuleCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public int Count { get { return default(int); } }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
}