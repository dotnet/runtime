// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    internal sealed class RuntimeLocalBuilder : LocalBuilder
    {
        #region Private Data Members
        private readonly int m_localIndex;
        private readonly Type m_localType;
        private readonly MethodInfo m_methodBuilder;
        private readonly bool m_isPinned;
        #endregion

        #region Constructor
        internal RuntimeLocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder)
            : this(localIndex, localType, methodBuilder, false) { }
        internal RuntimeLocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
        {
            m_isPinned = isPinned;
            m_localIndex = localIndex;
            m_localType = localType;
            m_methodBuilder = methodBuilder;
        }
        #endregion

        #region Internal Members
        internal MethodInfo GetMethodBuilder() => m_methodBuilder;
        #endregion

        #region LocalVariableInfo Override
        public override bool IsPinned => m_isPinned;
        public override Type LocalType => m_localType;
        public override int LocalIndex => m_localIndex;
        #endregion
    }
}
