// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection
{
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
}

