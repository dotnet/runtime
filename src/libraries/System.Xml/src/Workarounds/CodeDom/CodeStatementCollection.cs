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
    ///       A collection that stores <see cref='System.CodeDom.CodeStatement'/> objects.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeStatementCollection : CollectionBase
    {
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeStatementCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection()
        {
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeStatementCollection'/> based on another <see cref='System.CodeDom.CodeStatementCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection(CodeStatementCollection value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeStatementCollection'/> containing any array of <see cref='System.CodeDom.CodeStatement'/> objects.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection(CodeStatement[] value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.CodeDom.CodeStatement'/>.</para>
        /// </devdoc>
        public CodeStatement this[int index]
        {
            get
            {
                return ((CodeStatement)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>Adds a <see cref='System.CodeDom.CodeStatement'/> with the specified value to the 
        ///    <see cref='System.CodeDom.CodeStatementCollection'/> .</para>
        /// </devdoc>
        public int Add(CodeStatement value)
        {
            return List.Add(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(CodeExpression value)
        {
            return Add(new CodeExpressionStatement(value));
        }

        /// <devdoc>
        /// <para>Copies the elements of an array to the end of the <see cref='System.CodeDom.CodeStatementCollection'/>.</para>
        /// </devdoc>
        public void AddRange(CodeStatement[] value)
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
        ///       Adds the contents of another <see cref='System.CodeDom.CodeStatementCollection'/> to the end of the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CodeStatementCollection value)
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
        ///    <see cref='System.CodeDom.CodeStatementCollection'/> contains the specified <see cref='System.CodeDom.CodeStatement'/>.</para>
        /// </devdoc>
        public bool Contains(CodeStatement value)
        {
            return List.Contains(value);
        }

        /// <devdoc>
        /// <para>Copies the <see cref='System.CodeDom.CodeStatementCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(CodeStatement[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.CodeDom.CodeStatement'/> in 
        ///       the <see cref='System.CodeDom.CodeStatementCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(CodeStatement value)
        {
            return List.IndexOf(value);
        }

        /// <devdoc>
        /// <para>Inserts a <see cref='System.CodeDom.CodeStatement'/> into the <see cref='System.CodeDom.CodeStatementCollection'/> at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, CodeStatement value)
        {
            List.Insert(index, value);
        }

        /// <devdoc>
        ///    <para> Removes a specific <see cref='System.CodeDom.CodeStatement'/> from the 
        ///    <see cref='System.CodeDom.CodeStatementCollection'/> .</para>
        /// </devdoc>
        public void Remove(CodeStatement value)
        {
            List.Remove(value);
        }
    }
}
