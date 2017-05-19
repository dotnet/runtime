// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Design
{
    using System.Collections;

    /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection"]/*' />
    /// <devdoc>
    ///     <para>
    ///       A collection that stores <see cref='System.String'/> objects.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name = "FullTrust")]
    public sealed class CategoryNameCollection : ReadOnlyCollectionBase
    {
        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.CategoryNameCollection"]/*' />
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.Drawing.Design.CategoryNameCollection'/> based on another <see cref='System.Drawing.Design.CategoryNameCollection'/>.
        ///    </para>
        /// </devdoc>
        public CategoryNameCollection(CategoryNameCollection value)
        {
            InnerList.AddRange(value);
        }

        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.CategoryNameCollection1"]/*' />
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.Drawing.Design.CategoryNameCollection'/> containing any array of <see cref='System.String'/> objects.
        ///    </para>
        /// </devdoc>
        public CategoryNameCollection(String[] value)
        {
            InnerList.AddRange(value);
        }

        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.this"]/*' />
        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.String'/>.</para>
        /// </devdoc>
        public string this[int index]
        {
            get
            {
                return ((string)(InnerList[index]));
            }
        }

        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.Contains"]/*' />
        /// <devdoc>
        /// <para>Gets a value indicating whether the 
        ///    <see cref='System.Drawing.Design.CategoryNameCollection'/> contains the specified <see cref='System.String'/>.</para>
        /// </devdoc>
        public bool Contains(string value)
        {
            return InnerList.Contains(value);
        }

        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.CopyTo"]/*' />
        /// <devdoc>
        /// <para>Copies the <see cref='System.Drawing.Design.CategoryNameCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(String[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        /// <include file='doc\CategoryNameCollection.uex' path='docs/doc[@for="CategoryNameCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.String'/> in 
        ///       the <see cref='System.Drawing.Design.CategoryNameCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(string value)
        {
            return InnerList.IndexOf(value);
        }
    }
}

