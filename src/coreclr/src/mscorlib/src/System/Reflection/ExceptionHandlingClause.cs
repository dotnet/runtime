// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Reflection
{
    public class ExceptionHandlingClause
    {
        #region costructor
        // This class can only be created from inside the EE.
        protected ExceptionHandlingClause() { }
        #endregion

        #region Private Data Members
        private MethodBody m_methodBody;
        [ContractPublicPropertyName("Flags")]
        private ExceptionHandlingClauseOptions m_flags;
        private int m_tryOffset;
        private int m_tryLength;
        private int m_handlerOffset;
        private int m_handlerLength;
        private int m_catchMetadataToken;
        private int m_filterOffset;
        #endregion

        #region Public Members
        public virtual ExceptionHandlingClauseOptions Flags { get { return m_flags; } }
        public virtual int TryOffset { get { return m_tryOffset; } }
        public virtual int TryLength { get { return m_tryLength; } }
        public virtual int HandlerOffset { get { return m_handlerOffset; } }
        public virtual int HandlerLength { get { return m_handlerLength; } }

        public virtual int FilterOffset
        {
            get
            {
                if (m_flags != ExceptionHandlingClauseOptions.Filter)
                    throw new InvalidOperationException(SR.Arg_EHClauseNotFilter);

                return m_filterOffset;
            }
        }

        public virtual Type CatchType
        {
            get
            {
                if (m_flags != ExceptionHandlingClauseOptions.Clause)
                    throw new InvalidOperationException(SR.Arg_EHClauseNotClause);

                Type type = null;

                if (!MetadataToken.IsNullToken(m_catchMetadataToken))
                {
                    Type declaringType = m_methodBody.m_methodBase.DeclaringType;
                    Module module = (declaringType == null) ? m_methodBody.m_methodBase.Module : declaringType.Module;
                    type = module.ResolveType(m_catchMetadataToken, (declaringType == null) ? null : declaringType.GetGenericArguments(),
                        m_methodBody.m_methodBase is MethodInfo ? m_methodBody.m_methodBase.GetGenericArguments() : null);
                }

                return type;
            }
        }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            if (Flags == ExceptionHandlingClauseOptions.Clause)
            {
                return String.Format(CultureInfo.CurrentUICulture,
                    "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, CatchType={5}",
                    Flags, TryOffset, TryLength, HandlerOffset, HandlerLength, CatchType);
            }

            if (Flags == ExceptionHandlingClauseOptions.Filter)
            {
                return String.Format(CultureInfo.CurrentUICulture,
                    "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, FilterOffset={5}",
                    Flags, TryOffset, TryLength, HandlerOffset, HandlerLength, FilterOffset);
            }

            return String.Format(CultureInfo.CurrentUICulture,
                "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}",
                Flags, TryOffset, TryLength, HandlerOffset, HandlerLength);
        }
        #endregion
    }
}

