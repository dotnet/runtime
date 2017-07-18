// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Permissions;

namespace System.Drawing.Design
{
    /// <summary>
    /// A collection that stores <see cref='string'/> objects.
    /// </summary>
    public sealed class CategoryNameCollection : ReadOnlyCollectionBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref='CategoryNameCollection'/> based on another
        /// <see cref='CategoryNameCollection'/>.
        /// </summary>
        public CategoryNameCollection(CategoryNameCollection value) => InnerList.AddRange(value);

        /// <summary>
        /// Initializes a new instance of <see cref='CategoryNameCollection'/> containing any array of
        /// <see cref='string'/> objects.
        /// </summary>
        public CategoryNameCollection(string[] value) => InnerList.AddRange(value);

        /// <summary>
        /// Represents the entry at the specified index of the <see cref='string'/>.
        /// </summary>
        public string this[int index] => ((string)(InnerList[index]));

        /// <summary>
        /// Gets a value indicating whether the  <see cref='CategoryNameCollection'/> contains the specified
        /// <see cref='string'/>.
        /// </summary>
        public bool Contains(string value) => InnerList.Contains(value);

        /// <summary>
        /// Copies the <see cref='CategoryNameCollection'/> values to a one-dimensional <see cref='Array'/> instance
        /// at the specified index.
        /// </summary>
        public void CopyTo(string[] array, int index) => InnerList.CopyTo(array, index);

        /// <summary>
        /// Returns the index of a <see cref='string'/> in  the <see cref='CategoryNameCollection'/> .
        /// </summary>
        public int IndexOf(string value) => InnerList.IndexOf(value);
    }
}
