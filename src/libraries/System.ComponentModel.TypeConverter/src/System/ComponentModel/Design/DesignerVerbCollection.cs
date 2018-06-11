// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    public class DesignerVerbCollection : CollectionBase
    {
        public DesignerVerbCollection()
        {
        }

        public DesignerVerbCollection(DesignerVerb[] value) => AddRange(value);

        public DesignerVerb this[int index]
        {
            get => (DesignerVerb)(List[index]);
            set => List[index] = value;
        }

        public int Add(DesignerVerb value) => List.Add(value);

        public void AddRange(DesignerVerb[] value)
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

        public void AddRange(DesignerVerbCollection value)
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

        public void Insert(int index, DesignerVerb value) => List.Insert(index, value);

        public int IndexOf(DesignerVerb value) => List.IndexOf(value);

        public bool Contains(DesignerVerb value) => List.Contains(value);

        public void Remove(DesignerVerb value) => List.Remove(value);

        public void CopyTo(DesignerVerb[] array, int index) => List.CopyTo(array, index);
    
        protected override void OnValidate(object value)
        {
            // Dont perform any argument validation.
        }
    }
}
