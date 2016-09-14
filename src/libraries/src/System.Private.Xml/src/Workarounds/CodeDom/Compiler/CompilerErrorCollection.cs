// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System;
    using System.Collections;


    /// <devdoc>
    ///     <para>
    ///       A collection that stores <see cref='System.CodeDom.Compiler.CompilerError'/> objects.
    ///    </para>
    /// </devdoc>
    internal class CompilerErrorCollection : CollectionBase
    {
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/>.
        ///    </para>
        /// </devdoc>
        public CompilerErrorCollection()
        {
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> based on another <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/>.
        ///    </para>
        /// </devdoc>
        public CompilerErrorCollection(CompilerErrorCollection value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> containing any array of <see cref='System.CodeDom.Compiler.CompilerError'/> objects.
        ///    </para>
        /// </devdoc>
        public CompilerErrorCollection(CompilerError[] value)
        {
            this.AddRange(value);
        }

        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.CodeDom.Compiler.CompilerError'/>.</para>
        /// </devdoc>
        public CompilerError this[int index]
        {
            get
            {
                return ((CompilerError)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>Adds a <see cref='System.CodeDom.Compiler.CompilerError'/> with the specified value to the 
        ///    <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> .</para>
        /// </devdoc>
        public int Add(CompilerError value)
        {
            return List.Add(value);
        }

        /// <devdoc>
        /// <para>Copies the elements of an array to the end of the <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/>.</para>
        /// </devdoc>
        public void AddRange(CompilerError[] value)
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
        ///       Adds the contents of another <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> to the end of the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CompilerErrorCollection value)
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
        ///    <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> contains the specified <see cref='System.CodeDom.Compiler.CompilerError'/>.</para>
        /// </devdoc>
        public bool Contains(CompilerError value)
        {
            return List.Contains(value);
        }

        /// <devdoc>
        /// <para>Copies the <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(CompilerError[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       a value indicating whether the collection contains errors.
        ///    </para>
        /// </devdoc>
        public bool HasErrors
        {
            get
            {
                if (Count > 0)
                {
                    foreach (CompilerError e in this)
                    {
                        if (!e.IsWarning)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       a value indicating whether the collection contains warnings.
        ///    </para>
        /// </devdoc>
        public bool HasWarnings
        {
            get
            {
                if (Count > 0)
                {
                    foreach (CompilerError e in this)
                    {
                        if (e.IsWarning)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.CodeDom.Compiler.CompilerError'/> in 
        ///       the <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(CompilerError value)
        {
            return List.IndexOf(value);
        }

        /// <devdoc>
        /// <para>Inserts a <see cref='System.CodeDom.Compiler.CompilerError'/> into the <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, CompilerError value)
        {
            List.Insert(index, value);
        }

        /// <devdoc>
        ///    <para> Removes a specific <see cref='System.CodeDom.Compiler.CompilerError'/> from the 
        ///    <see cref='System.CodeDom.Compiler.CompilerErrorCollection'/> .</para>
        /// </devdoc>
        public void Remove(CompilerError value)
        {
            List.Remove(value);
        }
    }
}
