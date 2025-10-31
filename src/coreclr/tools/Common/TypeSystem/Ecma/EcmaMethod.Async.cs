// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaMethod
    {
        private AsyncMethodData _asyncMethodData;
        private MethodDesc _asyncOtherVariant;

        public override MethodSignature Signature
        {
            get
            {
                if (_metadataSignature == null)
                    InitializeSignature();
                if (AsyncMethodData.IsAsyncVariant)
                {
                    Debug.Assert(_asyncMethodData.Kind == AsyncMethodKind.AsyncVariantImpl && _asyncMethodData.Signature is not null);
                    return _asyncMethodData.Signature;
                }
                return _metadataSignature;
            }
        }

        public override AsyncMethodData AsyncMethodData
        {
            get
            {
                if (_asyncMethodData.Equals(default(AsyncMethodData)))
                {
                    InitializeSignature();
                    bool returnsTask = _metadataSignature.ReturnsTaskOrValueTask();
                    if (!returnsTask && !IsAsync)
                    {
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.NotAsync, Signature = _metadataSignature };
                    }
                    else if (returnsTask && IsAsync)
                    {
                        var asyncSignature = _metadataSignature.CreateAsyncSignature();
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.AsyncVariantImpl, Signature = asyncSignature };
                    }
                    else if (returnsTask && !IsAsync)
                    {
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.TaskReturning, Signature = _metadataSignature };
                    }
                    else
                    {
                        Debug.Assert(IsAsync && !returnsTask);
                        _asyncMethodData = new AsyncMethodData { Kind = AsyncMethodKind.AsyncExplicitImpl, Signature = _metadataSignature };
                    }
                }

                Debug.Assert(!_asyncMethodData.Equals(default(AsyncMethodData)));
                return _asyncMethodData;
            }
        }

        public override MethodDesc GetAsyncOtherVariant()
        {
            Debug.Assert(AsyncMethodData.Kind is AsyncMethodKind.TaskReturning or AsyncMethodKind.AsyncVariantImpl);
            if (_asyncOtherVariant is null)
            {
                MethodDesc otherVariant = IsAsync ?
                    new TaskReturningAsyncThunk(this, _metadataSignature) :
                    new AsyncMethodThunk(this);
                Interlocked.CompareExchange(ref _asyncOtherVariant, otherVariant, null);
            }
            return _asyncOtherVariant;
        }
    }
}
