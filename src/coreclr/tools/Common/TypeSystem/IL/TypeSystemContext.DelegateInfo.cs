// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext
    {
        private class DelegateInfoHashtable : LockFreeReaderHashtable<TypeDesc, DelegateInfo>
        {
            protected override int GetKeyHashCode(TypeDesc key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(DelegateInfo value)
            {
                return value.Type.GetHashCode();
            }
            protected override bool CompareKeyToValue(TypeDesc key, DelegateInfo value)
            {
                return Object.ReferenceEquals(key, value.Type);
            }
            protected override bool CompareValueToValue(DelegateInfo value1, DelegateInfo value2)
            {
                return Object.ReferenceEquals(value1.Type, value2.Type);
            }
            protected override DelegateInfo CreateValueFromKey(TypeDesc key)
            {
                return key.Context.CreateDelegateInfo(key);
            }
        }

        private DelegateInfoHashtable _delegateInfoHashtable = new DelegateInfoHashtable();

        public DelegateInfo GetDelegateInfo(TypeDesc delegateType)
        {
            return _delegateInfoHashtable.GetOrCreateValue(delegateType);
        }

        /// <summary>
        /// Creates a <see cref="DelegateInfo"/> for a given delegate type.
        /// </summary>
        protected virtual DelegateInfo CreateDelegateInfo(TypeDesc key)
        {
            // Type system contexts that support creating delegate infos need to override.
            throw new NotSupportedException();
        }
    }
}
