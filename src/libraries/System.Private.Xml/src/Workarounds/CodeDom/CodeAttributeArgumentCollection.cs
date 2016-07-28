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
    ///       A collection that stores <see cref='System.CodeDom.CodeAttributeArgument'/> objects.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeAttributeArgumentCollection : CollectionBase
    {
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeArgumentCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeAttributeArgumentCollection()
        {
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> based on another <see cref='System.CodeDom.CodeAttributeArgumentCollection'/>.
        ///    </para>
        /// </devdoc>
        public CodeAttributeArgumentCollection(CodeAttributeArgumentCollection value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> containing any array of <see cref='System.CodeDom.CodeAttributeArgument'/> objects.
        ///    </para>
        /// </devdoc>
        public CodeAttributeArgumentCollection(CodeAttributeArgument[] value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.CodeDom.CodeAttributeArgument'/>.</para>
        /// </devdoc>
        public CodeAttributeArgument this[int index]
        {
            get
            {
                return ((CodeAttributeArgument)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>Adds a <see cref='System.CodeDom.CodeAttributeArgument'/> with the specified value to the 
        ///    <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> .</para>
        /// </devdoc>
        public int Add(CodeAttributeArgument value)
        {
            return List.Add(value);
        }

        /// <devdoc>
        /// <para>Copies the elements of an array to the end of the <see cref='System.CodeDom.CodeAttributeArgumentCollection'/>.</para>
        /// </devdoc>
        public void AddRange(CodeAttributeArgument[] value)
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
        ///       Adds the contents of another <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> to the end of the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CodeAttributeArgumentCollection value)
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
        ///    <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> contains the specified <see cref='System.CodeDom.CodeAttributeArgument'/>.</para>
        /// </devdoc>
        public bool Contains(CodeAttributeArgument value)
        {
            return List.Contains(value);
        }

        /// <devdoc>
        /// <para>Copies the <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(CodeAttributeArgument[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.CodeDom.CodeAttributeArgument'/> in 
        ///       the <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(CodeAttributeArgument value)
        {
            return List.IndexOf(value);
        }

        /// <devdoc>
        /// <para>Inserts a <see cref='System.CodeDom.CodeAttributeArgument'/> into the <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, CodeAttributeArgument value)
        {
            List.Insert(index, value);
        }

        /// <devdoc>
        ///    <para> Removes a specific <see cref='System.CodeDom.CodeAttributeArgument'/> from the 
        ///    <see cref='System.CodeDom.CodeAttributeArgumentCollection'/> .</para>
        /// </devdoc>
        public void Remove(CodeAttributeArgument value)
        {
            List.Remove(value);
        }
    }
}
