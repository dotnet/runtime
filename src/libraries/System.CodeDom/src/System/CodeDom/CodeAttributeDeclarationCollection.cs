// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.CodeDom
{
    public class CodeAttributeDeclarationCollection : CollectionBase
    {
        public CodeAttributeDeclarationCollection()
        {
        }

        public CodeAttributeDeclarationCollection(CodeAttributeDeclarationCollection value)
        {
            AddRange(value);
        }

        public CodeAttributeDeclarationCollection(CodeAttributeDeclaration[] value)
        {
            AddRange(value);
        }

        public CodeAttributeDeclaration this[int index]
        {
            get => ((CodeAttributeDeclaration)(List[index]));
            set => List[index] = value;
        }

        public int Add(CodeAttributeDeclaration value) => List.Add(value);

        public void AddRange(CodeAttributeDeclaration[] value)
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

        public void AddRange(CodeAttributeDeclarationCollection value)
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

        public bool Contains(CodeAttributeDeclaration value) => List.Contains(value);

        public void CopyTo(CodeAttributeDeclaration[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeAttributeDeclaration value) => List.IndexOf(value);

        public void Insert(int index, CodeAttributeDeclaration value) => List.Insert(index, value);

        public void Remove(CodeAttributeDeclaration value) => List.Remove(value);
    }
}
