// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
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
        public virtual Type LocalType { get { Debug.Assert(m_type != null, "type must be set!"); return m_type; } }
        public virtual bool IsPinned { get { return m_isPinned != 0; } }
        public virtual int LocalIndex { get { return m_localIndex; } }
        #endregion
    }
}

