// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext
    {
        private AsyncMethodVariantFactory _asyncMethods = new();

        public MethodDesc GetAsyncVariant(MethodDesc method)
        {
            if (!method.Signature.ReturnsTaskOrValueTask())
                throw new InvalidOperationException();

            return _asyncMethods.GetOrCreateAsyncMethodImplVariant(method);
        }
    }
}
