// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeCatchClauseCollection : CollectionBase
    {
        public CodeCatchClauseCollection() { }

        public CodeCatchClauseCollection(CodeCatchClauseCollection value)
        {
            AddRange(value);
        }

        public CodeCatchClauseCollection(CodeCatchClause[] value)
        {
            AddRange(value);
        }

        public CodeCatchClause this[int index]
        {
            get => ((CodeCatchClause)(List[index]));
            set => List[index] = value;
        }

        public int Add(CodeCatchClause value) => List.Add(value);

        public void AddRange(CodeCatchClause[] value)
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

        public void AddRange(CodeCatchClauseCollection value)
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

        public bool Contains(CodeCatchClause value) => List.Contains(value);

        public void CopyTo(CodeCatchClause[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeCatchClause value) => List.IndexOf(value);

        public void Insert(int index, CodeCatchClause value) => List.Insert(index, value);

        public void Remove(CodeCatchClause value) => List.Remove(value);
    }
}
