// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.CodeDom.Compiler
{
    public class CompilerErrorCollection : CollectionBase
    {
        public CompilerErrorCollection() { }

        public CompilerErrorCollection(CompilerErrorCollection value)
        {
            AddRange(value);
        }

        public CompilerErrorCollection(CompilerError[] value)
        {
            AddRange(value);
        }

        public CompilerError this[int index]
        {
            get => (CompilerError)List[index];
            set => List[index] = value;
        }

        public int Add(CompilerError value) => List.Add(value);

        public void AddRange(CompilerError[] value)
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

        public void AddRange(CompilerErrorCollection value)
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

        public bool Contains(CompilerError value) => List.Contains(value);

        public void CopyTo(CompilerError[] array, int index) => List.CopyTo(array, index);

        public bool HasErrors
        {
            get
            {
                if (Count > 0)
                {
                    foreach (CompilerError e in this)
                    {
                        if (!e.IsWarning)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool HasWarnings
        {
            get
            {
                if (Count > 0)
                {
                    foreach (CompilerError e in this)
                    {
                        if (e.IsWarning)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public int IndexOf(CompilerError value) => List.IndexOf(value);

        public void Insert(int index, CompilerError value) => List.Insert(index, value);

        public void Remove(CompilerError value) => List.Remove(value);
    }
}
