// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;

    [
        ComVisible(true)
    ]
    internal class CodeTypeParameterCollection : CollectionBase
    {
        public CodeTypeParameterCollection()
        {
        }

        public CodeTypeParameterCollection(CodeTypeParameterCollection value)
        {
            this.AddRange(value);
        }

        public CodeTypeParameterCollection(CodeTypeParameter[] value)
        {
            this.AddRange(value);
        }

        public CodeTypeParameter this[int index]
        {
            get
            {
                return ((CodeTypeParameter)(List[index]));
            }
            set
            {
                List[index] = value;
            }
        }

        public int Add(CodeTypeParameter value)
        {
            return List.Add(value);
        }

        public void Add(string value)
        {
            Add(new CodeTypeParameter(value));
        }

        public void AddRange(CodeTypeParameter[] value)
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

        public void AddRange(CodeTypeParameterCollection value)
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

        public bool Contains(CodeTypeParameter value)
        {
            return List.Contains(value);
        }

        public void CopyTo(CodeTypeParameter[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(CodeTypeParameter value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, CodeTypeParameter value)
        {
            List.Insert(index, value);
        }

        public void Remove(CodeTypeParameter value)
        {
            List.Remove(value);
        }
    }
}

