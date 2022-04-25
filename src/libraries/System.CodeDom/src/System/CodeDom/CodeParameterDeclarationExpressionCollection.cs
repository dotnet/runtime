// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.CodeDom
{
    public class CodeParameterDeclarationExpressionCollection : CollectionBase
    {
        public CodeParameterDeclarationExpressionCollection() { }

        public CodeParameterDeclarationExpressionCollection(CodeParameterDeclarationExpressionCollection value)
        {
            AddRange(value);
        }

        public CodeParameterDeclarationExpressionCollection(CodeParameterDeclarationExpression[] value)
        {
            AddRange(value);
        }

        public CodeParameterDeclarationExpression this[int index]
        {
            get => (CodeParameterDeclarationExpression)List[index];
            set => List[index] = value;
        }

        public int Add(CodeParameterDeclarationExpression value) => List.Add(value);

        public void AddRange(CodeParameterDeclarationExpression[] value)
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

        public void AddRange(CodeParameterDeclarationExpressionCollection value)
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

        public bool Contains(CodeParameterDeclarationExpression value) => List.Contains(value);

        public void CopyTo(CodeParameterDeclarationExpression[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeParameterDeclarationExpression value) => List.IndexOf(value);

        public void Insert(int index, CodeParameterDeclarationExpression value) => List.Insert(index, value);

        public void Remove(CodeParameterDeclarationExpression value) => List.Remove(value);
    }
}
