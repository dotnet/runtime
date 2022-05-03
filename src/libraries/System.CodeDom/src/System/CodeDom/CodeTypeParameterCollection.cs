// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.CodeDom
{
    public class CodeTypeParameterCollection : CollectionBase
    {
        public CodeTypeParameterCollection() { }

        public CodeTypeParameterCollection(CodeTypeParameterCollection value)
        {
            AddRange(value);
        }

        public CodeTypeParameterCollection(CodeTypeParameter[] value)
        {
            AddRange(value);
        }

        public CodeTypeParameter this[int index]
        {
            get => (CodeTypeParameter)List[index];
            set => List[index] = value;
        }

        public int Add(CodeTypeParameter value) => List.Add(value);

        public void Add(string value) => Add(new CodeTypeParameter(value));

        public void AddRange(CodeTypeParameter[] value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(CodeTypeParameterCollection value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(CodeTypeParameter value) => List.Contains(value);

        public void CopyTo(CodeTypeParameter[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeTypeParameter value) => List.IndexOf(value);

        public void Insert(int index, CodeTypeParameter value) => List.Insert(index, value);

        public void Remove(CodeTypeParameter value) => List.Remove(value);
    }
}
