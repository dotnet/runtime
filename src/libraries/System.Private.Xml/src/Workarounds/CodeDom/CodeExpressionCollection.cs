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
    ///       A collection that stores <see cref='System.CodeDom.CodeExpression'/> objects.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeExpressionCollection : CollectionBase
    {
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeExpressionCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection()
        {
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeExpressionCollection'/> based on another <see cref='System.CodeDom.CodeExpressionCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection(CodeExpressionCollection value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeExpressionCollection'/> containing any array of <see cref='System.CodeDom.CodeExpression'/> objects.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection(CodeExpression[] value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.CodeDom.CodeExpression'/>.</para>
        /// </devdoc>
        public CodeExpression this[int index]
        {
            get
            {
                return ((CodeExpression)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>Adds a <see cref='System.CodeDom.CodeExpression'/> with the specified value to the 
        ///    <see cref='System.CodeDom.CodeExpressionCollection'/> .</para>
        /// </devdoc>
        public int Add(CodeExpression value)
        {
            return List.Add(value);
        }

        /// <devdoc>
        /// <para>Copies the elements of an array to the end of the <see cref='System.CodeDom.CodeExpressionCollection'/>.</para>
        /// </devdoc>
        public void AddRange(CodeExpression[] value)
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
        ///       Adds the contents of another <see cref='System.CodeDom.CodeExpressionCollection'/> to the end of the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CodeExpressionCollection value)
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
        ///    <see cref='System.CodeDom.CodeExpressionCollection'/> contains the specified <see cref='System.CodeDom.CodeExpression'/>.</para>
        /// </devdoc>
        public bool Contains(CodeExpression value)
        {
            return List.Contains(value);
        }

        /// <devdoc>
        /// <para>Copies the <see cref='System.CodeDom.CodeExpressionCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(CodeExpression[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.CodeDom.CodeExpression'/> in 
        ///       the <see cref='System.CodeDom.CodeExpressionCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(CodeExpression value)
        {
            return List.IndexOf(value);
        }

        /// <devdoc>
        /// <para>Inserts a <see cref='System.CodeDom.CodeExpression'/> into the <see cref='System.CodeDom.CodeExpressionCollection'/> at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, CodeExpression value)
        {
            List.Insert(index, value);
        }

        /// <devdoc>
        ///    <para> Removes a specific <see cref='System.CodeDom.CodeExpression'/> from the 
        ///    <see cref='System.CodeDom.CodeExpressionCollection'/> .</para>
        /// </devdoc>
        public void Remove(CodeExpression value)
        {
            List.Remove(value);
        }
    }
}
