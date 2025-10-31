// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed partial class TaskReturningAsyncThunk : MethodDelegator
    {
        protected internal override int ClassCode => 0x6d7dcbcb;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            if (other is TaskReturningAsyncThunk otherAsync)
            {
                return comparer.Compare(_wrappedMethod, otherAsync._wrappedMethod);
            }
            return -1;
        }
    }
}
