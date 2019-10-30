// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.Protocols
{
    public class PartialResultsCollection : ReadOnlyCollectionBase
    {
        internal PartialResultsCollection() { }

        public object this[int index] => InnerList[index];

        internal int Add(object value) => InnerList.Add(value);

        public bool Contains(object value) => InnerList.Contains(value);

        public int IndexOf(object value) => InnerList.IndexOf(value);

        public void CopyTo(object[] values, int index) => InnerList.CopyTo(values, index);
    }
}
