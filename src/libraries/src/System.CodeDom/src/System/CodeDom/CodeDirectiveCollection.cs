// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeDirectiveCollection : CollectionBase
    {
        public CodeDirectiveCollection() { }

        public CodeDirectiveCollection(CodeDirectiveCollection value)
        {
            AddRange(value);
        }

        public CodeDirectiveCollection(CodeDirective[] value)
        {
            AddRange(value);
        }

        public CodeDirective this[int index]
        {
            get => (CodeDirective)List[index];
            set => List[index] = value;
        }

        public int Add(CodeDirective value) => List.Add(value);

        public void AddRange(CodeDirective[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(CodeDirectiveCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(CodeDirective value) => List.Contains(value);

        public void CopyTo(CodeDirective[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeDirective value) => List.IndexOf(value);

        public void Insert(int index, CodeDirective value) => List.Insert(index, value);

        public void Remove(CodeDirective value) => List.Remove(value);
    }
}
