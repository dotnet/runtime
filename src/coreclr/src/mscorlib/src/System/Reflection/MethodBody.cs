// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Reflection
{   
    [Flags()]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum ExceptionHandlingClauseOptions: int
    {
        Clause = 0x0,
        Filter = 0x1,
        Finally = 0x2,
        Fault = 0x4,
    }
    
    [System.Runtime.InteropServices.ComVisible(true)]
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
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_EHClauseNotFilter"));

                return m_filterOffset;
            }
        }

        public virtual Type CatchType
        {
            get
            {
                if (m_flags != ExceptionHandlingClauseOptions.Clause)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_EHClauseNotClause"));

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
    
    [System.Runtime.InteropServices.ComVisible(true)]
    public class MethodBody
    {
        #region costructor
        // This class can only be created from inside the EE.
        protected MethodBody() { }
        #endregion
        
        #region Private Data Members
        private byte[] m_IL;
        private ExceptionHandlingClause[] m_exceptionHandlingClauses;
        private LocalVariableInfo[] m_localVariables;
        internal MethodBase m_methodBase;
        private int m_localSignatureMetadataToken;
        private int m_maxStackSize;
        private bool m_initLocals;   
        #endregion

        #region Public Members
        public virtual int LocalSignatureMetadataToken { get { return m_localSignatureMetadataToken; } }
        public virtual IList<LocalVariableInfo> LocalVariables { get { return Array.AsReadOnly(m_localVariables); } }
        public virtual int MaxStackSize { get { return m_maxStackSize; } }
        public virtual bool InitLocals { get { return m_initLocals; } }
        public virtual byte[] GetILAsByteArray() { return m_IL; }
        public virtual IList<ExceptionHandlingClause> ExceptionHandlingClauses { get { return Array.AsReadOnly(m_exceptionHandlingClauses); } }
        #endregion
    }   

    [System.Runtime.InteropServices.ComVisible(true)]
    public class LocalVariableInfo
    {
        #region Private Data Members
        private RuntimeType m_type;
        private int m_isPinned;
        private int m_localIndex;
        #endregion

        #region Constructor
        protected LocalVariableInfo() { }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            string toString = LocalType.ToString() + " (" + LocalIndex + ")";
            
            if (IsPinned)
                toString += " (pinned)";

            return toString;
        }
        #endregion

        #region Public Members
        public virtual Type LocalType { get { Contract.Assert(m_type != null, "type must be set!"); return m_type; } }
        public virtual bool IsPinned { get { return m_isPinned != 0; } }
        public virtual int LocalIndex { get { return m_localIndex; } }
        #endregion
    }
}

