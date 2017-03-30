// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace System.Data
{
    /// <summary>
    /// Provides the base functionality for creating collections.
    /// </summary>
    public class InternalDataCollectionBase : ICollection
    {
        internal static readonly CollectionChangeEventArgs s_refreshEventArgs = new CollectionChangeEventArgs(CollectionChangeAction.Refresh, null);

        /// <summary>
        /// Gets the total number of elements in a collection.
        /// </summary>
        [Browsable(false)]
        public virtual int Count => List.Count;

        public virtual void CopyTo(Array ar, int index) => List.CopyTo(ar, index);

        public virtual IEnumerator GetEnumerator() => List.GetEnumerator();

        [Browsable(false)]
        public bool IsReadOnly => false;

        [Browsable(false)]
        public bool IsSynchronized => false; // so the user will know that it has to lock this object

        // Return value: 
        // > 0 (1)  : CaseSensitve equal      
        // < 0 (-1) : Case-Insensitive Equal
        // = 0      : Not Equal
        internal int NamesEqual(string s1, string s2, bool fCaseSensitive, CultureInfo locale)
        {
            if (fCaseSensitive)
            {
                return string.Compare(s1, s2, false, locale) == 0 ? 1 : 0;
            }

            // Case, kana and width -Insensitive compare
            if (locale.CompareInfo.Compare(s1, s2, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            {
                return string.Compare(s1, s2, false, locale) == 0 ? 1 : -1;
            }

            return 0;
        }


        [Browsable(false)]
        public object SyncRoot => this;

        protected virtual ArrayList List => null;
    }
}
