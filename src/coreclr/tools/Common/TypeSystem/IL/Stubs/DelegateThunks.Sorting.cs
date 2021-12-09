// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    partial class DelegateThunk
    {
        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (DelegateThunk)other;
            return comparer.Compare(_delegateInfo.Type, otherMethod._delegateInfo.Type);
        }
    }

    partial class DelegateInvokeOpenStaticThunk
    {
        protected internal override int ClassCode => 386356101;
    }

    public sealed partial class DelegateInvokeOpenInstanceThunk
    {
        protected internal override int ClassCode => -1787190244;
    }

    partial class DelegateInvokeClosedStaticThunk
    {
        protected internal override int ClassCode => 28195375;
    }

    partial class DelegateInvokeMulticastThunk
    {
        protected internal override int ClassCode => 639863471;
    }

    partial class DelegateInvokeInstanceClosedOverGenericMethodThunk
    {
        protected internal override int ClassCode => -354480633;
    }

    partial class DelegateInvokeObjectArrayThunk
    {
        protected internal override int ClassCode => 1993292344;
    }

    partial class DelegateGetThunkMethodOverride
    {
        protected internal override int ClassCode => -321263379;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (DelegateGetThunkMethodOverride)other;
            return comparer.Compare(_delegateInfo.Type, otherMethod._delegateInfo.Type);
        }
    }
}
