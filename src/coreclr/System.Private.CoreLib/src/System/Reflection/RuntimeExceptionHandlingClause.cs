// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection
{
    internal sealed class RuntimeExceptionHandlingClause : ExceptionHandlingClause
    {
        // This class can only be created from inside the EE.
        private RuntimeExceptionHandlingClause() { }

        private RuntimeMethodBody _methodBody = null!;
        private ExceptionHandlingClauseOptions _flags;
        private int _tryOffset;
        private int _tryLength;
        private int _handlerOffset;
        private int _handlerLength;
        private int _catchMetadataToken;
        private int _filterOffset;

        public override ExceptionHandlingClauseOptions Flags => _flags;
        public override int TryOffset => _tryOffset;
        public override int TryLength => _tryLength;
        public override int HandlerOffset => _handlerOffset;
        public override int HandlerLength => _handlerLength;

        public override int FilterOffset
        {
            get
            {
                if (_flags != ExceptionHandlingClauseOptions.Filter)
                    throw new InvalidOperationException(SR.Arg_EHClauseNotFilter);

                return _filterOffset;
            }
        }

        public override Type? CatchType
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Module.ResolveType is marked as RequiresUnreferencedCode because it relies on tokens" +
                                "which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break." +
                                "The usage here is not like that as all these tokens come from existing metadata loaded from some IL" +
                                "and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
            get
            {
                if (_flags != ExceptionHandlingClauseOptions.Clause)
                    throw new InvalidOperationException(SR.Arg_EHClauseNotClause);

                Type? type = null;

                if (!MetadataToken.IsNullToken(_catchMetadataToken))
                {
                    Type? declaringType = _methodBody._methodBase.DeclaringType;
                    Module module = (declaringType == null) ? _methodBody._methodBase.Module : declaringType.Module;
                    type = module.ResolveType(_catchMetadataToken, declaringType?.GetGenericArguments(),
                        _methodBody._methodBase is MethodInfo ? _methodBody._methodBase.GetGenericArguments() : null);
                }

                return type;
            }
        }

        public override string ToString()
        {
            if (Flags == ExceptionHandlingClauseOptions.Clause)
            {
                return string.Format(CultureInfo.CurrentUICulture,
                    "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, CatchType={5}",
                    Flags, TryOffset, TryLength, HandlerOffset, HandlerLength, CatchType);
            }

            if (Flags == ExceptionHandlingClauseOptions.Filter)
            {
                return string.Format(CultureInfo.CurrentUICulture,
                    "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, FilterOffset={5}",
                    Flags, TryOffset, TryLength, HandlerOffset, HandlerLength, FilterOffset);
            }

            return string.Format(CultureInfo.CurrentUICulture,
                "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}",
                Flags, TryOffset, TryLength, HandlerOffset, HandlerLength);
        }
    }
}
