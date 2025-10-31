// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedMethod
    {
        private AsyncMethodData _asyncMethodData;
        private MethodDesc _asyncOtherVariant;

        public override AsyncMethodData AsyncMethodData
        {
            get
            {
                if (!_asyncMethodData.Equals(default(AsyncMethodData)))
                    return _asyncMethodData;

                if (IsAsync)
                {
                    // If the method is already async, the template signature should already have been updated to reflect the AsyncCallConv
                    Debug.Assert(!Signature.ReturnsTaskOrValueTask() && Signature.IsAsyncCallConv);
                    _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.AsyncVariantImpl, Signature = Signature };
                }
                else if (Signature.ReturnsTaskOrValueTask())
                {
                    _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.TaskReturning, Signature = Signature };
                }
                else
                {
                    _asyncMethodData = new AsyncMethodData() { Kind = AsyncMethodKind.NotAsync, Signature = Signature };
                }

                return _asyncMethodData;
            }
        }

        public override MethodDesc GetAsyncOtherVariant()
        {
            if (_asyncOtherVariant is null)
            {
                MethodDesc otherVariant = IsAsync ?
                    new TaskReturningAsyncThunk(this, InstantiateSignature(_methodDef.GetAsyncOtherVariant().Signature))
                    : new AsyncMethodThunk(this);
                Interlocked.CompareExchange(ref _asyncOtherVariant, otherVariant, null);
            }
            return _asyncOtherVariant;
        }
    }
}
