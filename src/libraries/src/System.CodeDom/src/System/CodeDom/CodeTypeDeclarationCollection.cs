// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeTypeDeclarationCollection : CollectionBase
    {
        public CodeTypeDeclarationCollection() { }

        public CodeTypeDeclarationCollection(CodeTypeDeclarationCollection value)
        {
            AddRange(value);
        }

        public CodeTypeDeclarationCollection(CodeTypeDeclaration[] value)
        {
            AddRange(value);
        }

        public CodeTypeDeclaration this[int index]
        {
            get => (CodeTypeDeclaration)List[index];
            set => List[index] = value;
        }

        public int Add(CodeTypeDeclaration value) => List.Add(value);

        public void AddRange(CodeTypeDeclaration[] value)
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

        public void AddRange(CodeTypeDeclarationCollection value)
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

        public bool Contains(CodeTypeDeclaration value) => List.Contains(value);

        public void CopyTo(CodeTypeDeclaration[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeTypeDeclaration value) => List.IndexOf(value);

        public void Insert(int index, CodeTypeDeclaration value) => List.Insert(index, value);

        public void Remove(CodeTypeDeclaration value) => List.Remove(value);
    }
}
