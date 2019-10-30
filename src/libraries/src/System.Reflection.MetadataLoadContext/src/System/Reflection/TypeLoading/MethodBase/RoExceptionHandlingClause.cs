// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    internal sealed partial class RoExceptionHandlingClause : ExceptionHandlingClause
    {
        private readonly Type _catchType;
        private readonly ExceptionHandlingClauseOptions _flags;
        private readonly int _filterOffset;
        private readonly int _tryOffset;
        private readonly int _tryLength;
        private readonly int _handlerOffset;
        private readonly int _handlerLength;

        internal RoExceptionHandlingClause(Type catchType, ExceptionHandlingClauseOptions flags, int filterOffset, int tryOffset, int tryLength, int handlerOffset, int handlerLength)
        {
            _catchType = catchType;
            _flags = flags;
            _filterOffset = filterOffset;
            _tryOffset = tryOffset;
            _tryLength = tryLength;
            _handlerOffset = handlerOffset;
            _handlerLength = handlerLength;
        }

        public sealed override Type CatchType => _flags == ExceptionHandlingClauseOptions.Clause ? _catchType : throw new InvalidOperationException(SR.NotAClause);
        public sealed override ExceptionHandlingClauseOptions Flags => _flags;
        public sealed override int FilterOffset => _flags == ExceptionHandlingClauseOptions.Filter ? _filterOffset : throw new InvalidOperationException(SR.NotAFilter);
        public sealed override int HandlerOffset => _handlerOffset;
        public sealed override int HandlerLength => _handlerLength;
        public sealed override int TryOffset => _tryOffset;
        public sealed override int TryLength => _tryLength;
    }
}
