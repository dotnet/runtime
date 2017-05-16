// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeNamespaceCollection : CollectionBase
    {
        public CodeNamespaceCollection() { }

        public CodeNamespaceCollection(CodeNamespaceCollection value)
        {
            AddRange(value);
        }

        public CodeNamespaceCollection(CodeNamespace[] value)
        {
            AddRange(value);
        }

        public CodeNamespace this[int index]
        {
            get { return (CodeNamespace)List[index]; }
            set { List[index] = value; }
        }

        public int Add(CodeNamespace value) => List.Add(value);

        public void AddRange(CodeNamespace[] value)
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

        public void AddRange(CodeNamespaceCollection value)
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

        public bool Contains(CodeNamespace value) => List.Contains(value);

        public void CopyTo(CodeNamespace[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeNamespace value) => List.IndexOf(value);

        public void Insert(int index, CodeNamespace value) => List.Insert(index, value);

        public void Remove(CodeNamespace value) => List.Remove(value);
    }
}
