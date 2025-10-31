// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed partial class AsyncMethodThunk : MethodDelegator
    {
        protected internal override int ClassCode
        {
            get
            {
                return 0x554d08b9;
            }
        }

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            if (other is AsyncMethodThunk otherAsync)
            {
                return comparer.Compare(_wrappedMethod, otherAsync._wrappedMethod);
            }
            return -1;
        }
    }
}
