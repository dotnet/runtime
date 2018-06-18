// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeStatementCollection : CollectionBase
    {
        public CodeStatementCollection() { }

        public CodeStatementCollection(CodeStatementCollection value)
        {
            AddRange(value);
        }

        public CodeStatementCollection(CodeStatement[] value)
        {
            AddRange(value);
        }

        public CodeStatement this[int index]
        {
            get => (CodeStatement)List[index];
            set => List[index] = value;
        }

        public int Add(CodeStatement value) => List.Add(value);

        public int Add(CodeExpression value) => Add(new CodeExpressionStatement(value));

        public void AddRange(CodeStatement[] value)
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

        public void AddRange(CodeStatementCollection value)
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

        public bool Contains(CodeStatement value) => List.Contains(value);

        public void CopyTo(CodeStatement[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeStatement value) => List.IndexOf(value);

        public void Insert(int index, CodeStatement value) => List.Insert(index, value);

        public void Remove(CodeStatement value) => List.Remove(value);
    }
}
