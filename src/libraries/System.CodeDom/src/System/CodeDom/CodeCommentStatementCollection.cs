// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom
{
    public class CodeCommentStatementCollection : CollectionBase
    {
        public CodeCommentStatementCollection() { }

        public CodeCommentStatementCollection(CodeCommentStatementCollection value)
        {
            AddRange(value);
        }

        public CodeCommentStatementCollection(CodeCommentStatement[] value)
        {
            AddRange(value);
        }

        public CodeCommentStatement this[int index]
        {
            get { return (CodeCommentStatement)List[index]; }
            set { List[index] = value; }
        }

        public int Add(CodeCommentStatement value) => List.Add(value);

        public void AddRange(CodeCommentStatement[] value)
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

        public void AddRange(CodeCommentStatementCollection value)
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

        public bool Contains(CodeCommentStatement value) => List.Contains(value);

        public void CopyTo(CodeCommentStatement[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeCommentStatement value) => List.IndexOf(value);

        public void Insert(int index, CodeCommentStatement value) => List.Insert(index, value);

        public void Remove(CodeCommentStatement value) => List.Remove(value);
    }
}
