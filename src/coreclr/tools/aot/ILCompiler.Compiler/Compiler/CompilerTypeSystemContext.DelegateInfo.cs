// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        private class DelegateInfoHashtable : LockFreeReaderHashtable<TypeDesc, DelegateInfo>
        {
            private readonly DelegateFeature _delegateFeatures;

            public DelegateInfoHashtable(DelegateFeature features)
                => _delegateFeatures = features;

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
                return new DelegateInfo(key, _delegateFeatures);
            }
        }

        private readonly DelegateInfoHashtable _delegateInfoHashtable;

        public DelegateInfo GetDelegateInfo(TypeDesc delegateType)
        {
            return _delegateInfoHashtable.GetOrCreateValue(delegateType);
        }
    }
}
