// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    public sealed class LocalBuilder : LocalVariableInfo
    {
        #region Private Data Members
        private int m_localIndex;
        private Type m_localType;
        private MethodInfo m_methodBuilder;
        private bool m_isPinned;
        #endregion

        #region Constructor
        internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder)
            : this(localIndex, localType, methodBuilder, false) { }
        internal LocalBuilder(int localIndex, Type localType, MethodInfo methodBuilder, bool isPinned)
        {
            m_isPinned = isPinned;
            m_localIndex = localIndex;
            m_localType = localType;
            m_methodBuilder = methodBuilder;
        }
        #endregion

        #region Internal Members
        internal int GetLocalIndex()
        {
            return m_localIndex;
        }
        internal MethodInfo GetMethodBuilder()
        {
            return m_methodBuilder;
        }
        #endregion

        #region LocalVariableInfo Override
        public override bool IsPinned { get { return m_isPinned; } }
        public override Type LocalType
        {
            get
            {
                return m_localType;
            }
        }
        public override int LocalIndex { get { return m_localIndex; } }
        #endregion

        #region Public Members
        public void SetLocalSymInfo(string name)
        {
            SetLocalSymInfo(name, 0, 0);
        }

        public void SetLocalSymInfo(string name, int startOffset, int endOffset)
        {
            ModuleBuilder dynMod;
            SignatureHelper sigHelp;
            int sigLength;
            byte[] signature;
            byte[] mungedSig;
            int index;

            MethodBuilder? methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                // it's a light code gen entity
                throw new NotSupportedException();
            dynMod = (ModuleBuilder)methodBuilder.Module;
            if (methodBuilder.IsTypeCreated())
            {
                // cannot change method after its containing type has been created
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
            }

            // set the name and range of offset for the local
            if (dynMod.GetSymWriter() == null)
            {
                // cannot set local name if not debug module
                throw new InvalidOperationException(SR.InvalidOperation_NotADebugModule);
            }

            sigHelp = SignatureHelper.GetFieldSigHelper(dynMod);
            sigHelp.AddArgument(m_localType);
            signature = sigHelp.InternalGetSignature(out sigLength);

            // The symbol store doesn't want the calling convention on the
            // front of the signature, but InternalGetSignature returns
            // the callinging convention. So we strip it off. This is a
            // bit unfortunate, since it means that we need to allocate
            // yet another array of bytes...  
            mungedSig = new byte[sigLength - 1];
            Buffer.BlockCopy(signature, 1, mungedSig, 0, sigLength - 1);

            index = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
            if (index == -1)
            {
                // top level scope information is kept with methodBuilder
                methodBuilder.m_localSymInfo!.AddLocalSymInfo(
                     name,
                     mungedSig,
                     m_localIndex,
                     startOffset,
                     endOffset);
            }
            else
            {
                methodBuilder.GetILGenerator().m_ScopeTree.AddLocalSymInfoToCurrentScope(
                     name,
                     mungedSig,
                     m_localIndex,
                     startOffset,
                     endOffset);
            }
        }
        #endregion
    }
}

