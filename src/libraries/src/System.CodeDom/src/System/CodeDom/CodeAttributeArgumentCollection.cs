// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeAttributeArgumentCollection : CollectionBase
    {
        public CodeAttributeArgumentCollection() { }

        public CodeAttributeArgumentCollection(CodeAttributeArgumentCollection value)
        {
            AddRange(value);
        }

        public CodeAttributeArgumentCollection(CodeAttributeArgument[] value)
        {
            AddRange(value);
        }

        public CodeAttributeArgument this[int index]
        {
            get => (CodeAttributeArgument)List[index];
            set => List[index] = value;
        }

        public int Add(CodeAttributeArgument value) => List.Add(value);

        public void AddRange(CodeAttributeArgument[] value)
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

        public void AddRange(CodeAttributeArgumentCollection value)
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

        public bool Contains(CodeAttributeArgument value) => List.Contains(value);

        public void CopyTo(CodeAttributeArgument[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeAttributeArgument value) => List.IndexOf(value);

        public void Insert(int index, CodeAttributeArgument value) => List.Insert(index, value);

        public void Remove(CodeAttributeArgument value) => List.Remove(value);
    }
}
