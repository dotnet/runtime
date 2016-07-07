// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;


    /// <devdoc>
    ///     <para>
    ///       A collection that stores <see cref='System.CodeDom.CodeCommentStatement'/> objects.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeCommentStatementCollection : CollectionBase
    {
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCommentStatementCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatementCollection()
        {
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCommentStatementCollection'/> based on another <see cref='System.CodeDom.CodeCommentStatementCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatementCollection(CodeCommentStatementCollection value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCommentStatementCollection'/> containing any array of <see cref='System.CodeDom.CodeCommentStatement'/> objects.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatementCollection(CodeCommentStatement[] value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.CodeDom.CodeCommentStatement'/>.</para>
        /// </devdoc>
        public CodeCommentStatement this[int index]
        {
            get
            {
                return ((CodeCommentStatement)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>Adds a <see cref='System.CodeDom.CodeCommentStatement'/> with the specified value to the 
        ///    <see cref='System.CodeDom.CodeCommentStatementCollection'/> .</para>
        /// </devdoc>
        public int Add(CodeCommentStatement value)
        {
            return List.Add(value);
        }

        /// <devdoc>
        /// <para>Copies the elements of an array to the end of the <see cref='System.CodeDom.CodeCommentStatementCollection'/>.</para>
        /// </devdoc>
        public void AddRange(CodeCommentStatement[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1)))
            {
                this.Add(value[i]);
            }
        }

        /// <devdoc>
        ///     <para>
        ///       Adds the contents of another <see cref='System.CodeDom.CodeCommentStatementCollection'/> to the end of the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CodeCommentStatementCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                this.Add(value[i]);
            }
        }

        /// <devdoc>
        /// <para>Gets a value indicating whether the 
        ///    <see cref='System.CodeDom.CodeCommentStatementCollection'/> contains the specified <see cref='System.CodeDom.CodeCommentStatement'/>.</para>
        /// </devdoc>
        public bool Contains(CodeCommentStatement value)
        {
            return List.Contains(value);
        }

        /// <devdoc>
        /// <para>Copies the <see cref='System.CodeDom.CodeCommentStatementCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(CodeCommentStatement[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.CodeDom.CodeCommentStatement'/> in 
        ///       the <see cref='System.CodeDom.CodeCommentStatementCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(CodeCommentStatement value)
        {
            return List.IndexOf(value);
        }

        /// <devdoc>
        /// <para>Inserts a <see cref='System.CodeDom.CodeCommentStatement'/> into the <see cref='System.CodeDom.CodeCommentStatementCollection'/> at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, CodeCommentStatement value)
        {
            List.Insert(index, value);
        }

        /// <devdoc>
        ///    <para> Removes a specific <see cref='System.CodeDom.CodeCommentStatement'/> from the 
        ///    <see cref='System.CodeDom.CodeCommentStatementCollection'/> .</para>
        /// </devdoc>
        public void Remove(CodeCommentStatement value)
        {
            List.Remove(value);
        }
    }
}
