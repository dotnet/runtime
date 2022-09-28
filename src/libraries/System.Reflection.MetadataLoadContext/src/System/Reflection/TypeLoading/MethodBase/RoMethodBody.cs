// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    internal abstract partial class RoMethodBody : MethodBody
    {
        protected RoMethodBody()
        {
        }

        public abstract override bool InitLocals { get; }
        public abstract override int MaxStackSize { get; }
        public abstract override int LocalSignatureMetadataToken { get; }

        // Unlike most apis, this one does not copy the byte array.
        public sealed override byte[]? GetILAsByteArray() => _lazyIL ??= ComputeIL();
        protected abstract byte[]? ComputeIL();
        private volatile byte[]? _lazyIL;

        public abstract override IList<LocalVariableInfo> LocalVariables { get; }
        public abstract override IList<ExceptionHandlingClause> ExceptionHandlingClauses { get; }
    }
}
