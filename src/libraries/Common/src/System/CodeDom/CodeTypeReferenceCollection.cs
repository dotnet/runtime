// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

#if !FEATURE_SERIALIZATION
namespace System.CodeDom
#else
namespace System.Runtime.Serialization
#endif
{
#if !FEATURE_SERIALIZATION
    public class CodeTypeReferenceCollection : CollectionBase
#else
    internal sealed class CodeTypeReferenceCollection : CollectionBase
#endif
    {
        public CodeTypeReferenceCollection() { }

        public CodeTypeReferenceCollection(CodeTypeReferenceCollection value)
        {
            AddRange(value);
        }

        public CodeTypeReferenceCollection(CodeTypeReference[] value)
        {
            AddRange(value);
        }

        public CodeTypeReference this[int index]
        {
            get { return ((CodeTypeReference)(List[index]!)); }
            set { List[index] = value; }
        }

        public int Add(CodeTypeReference value) => List.Add(value);

        public void Add(string value) => Add(new CodeTypeReference(value));

        public void Add(Type value) => Add(new CodeTypeReference(value));

        public void AddRange(CodeTypeReference[] value)
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

        public void AddRange(CodeTypeReferenceCollection value)
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

        public bool Contains(CodeTypeReference value) => List.Contains(value);

        public void CopyTo(CodeTypeReference[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeTypeReference value) => List.IndexOf(value);

        public void Insert(int index, CodeTypeReference value) => List.Insert(index, value);

        public void Remove(CodeTypeReference value) => List.Remove(value);
    }
}
