// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.CodeDom
{
    public class CodeTypeMemberCollection : CollectionBase
    {
        private bool _trackMethodIdentity;
        internal void TrackMethodIdentity() => _trackMethodIdentity = true;

        private void InvalidateMethodIdentity()
        {
            if (_trackMethodIdentity)
                CodeMemberMethod.InvalidateMethodIdentity();
        }

        protected override void OnClearComplete()
        {
            base.OnClearComplete();
            InvalidateMethodIdentity();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            base.OnInsertComplete(index, value);
            InvalidateMethodIdentity();
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            base.OnRemoveComplete(index, value);
            InvalidateMethodIdentity();
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            base.OnSetComplete(index, oldValue, newValue);
            InvalidateMethodIdentity();
        }

        public CodeTypeMemberCollection() { }

        public CodeTypeMemberCollection(CodeTypeMemberCollection value)
        {
            AddRange(value);
        }

        public CodeTypeMemberCollection(CodeTypeMember[] value)
        {
            AddRange(value);
        }

        public CodeTypeMember this[int index]
        {
            get => (CodeTypeMember)List[index];
            set => List[index] = value;
        }

        public int Add(CodeTypeMember value) => List.Add(value);

        public void AddRange(CodeTypeMember[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(CodeTypeMemberCollection value)
        {
            ArgumentNullException.ThrowIfNull(value);

            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(CodeTypeMember value) => List.Contains(value);

        public void CopyTo(CodeTypeMember[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeTypeMember value) => List.IndexOf(value);

        public void Insert(int index, CodeTypeMember value) => List.Insert(index, value);

        public void Remove(CodeTypeMember value) => List.Remove(value);
    }
}
