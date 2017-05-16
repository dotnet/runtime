// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeExpressionCollection : CollectionBase
    {
        public CodeExpressionCollection() { }

        public CodeExpressionCollection(CodeExpressionCollection value)
        {
            AddRange(value);
        }

        public CodeExpressionCollection(CodeExpression[] value)
        {
            AddRange(value);
        }

        public CodeExpression this[int index]
        {
            get { return (CodeExpression)List[index]; }
            set { List[index] = value; }
        }

        public int Add(CodeExpression value) => List.Add(value);

        public void AddRange(CodeExpression[] value)
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

        public void AddRange(CodeExpressionCollection value)
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

        public bool Contains(CodeExpression value) => List.Contains(value);

        public void CopyTo(CodeExpression[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeExpression value) => List.IndexOf(value);

        public void Insert(int index, CodeExpression value) => List.Insert(index, value);

        public void Remove(CodeExpression value) => List.Remove(value);
    }
}
