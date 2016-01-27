// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.TCEAdapterGen {

    using System;
    using System.Reflection;
    using System.Collections;

    internal class EventItfInfo
    {
        public EventItfInfo(String strEventItfName,
                            String strSrcItfName,
                            String strEventProviderName,
                            RuntimeAssembly asmImport,
                            RuntimeAssembly asmSrcItf)
        {
            m_strEventItfName = strEventItfName;
            m_strSrcItfName = strSrcItfName;
            m_strEventProviderName = strEventProviderName;
            m_asmImport = asmImport;
            m_asmSrcItf = asmSrcItf;
        }
        
        public Type GetEventItfType()
        {
            Type t = m_asmImport.GetType(m_strEventItfName, true, false);
            if (t != null && !t.IsVisible) 
                t = null;
            return t;
        }

        public Type GetSrcItfType()
        {
            Type t = m_asmSrcItf.GetType(m_strSrcItfName, true, false);
            if (t != null && !t.IsVisible) 
                t = null;
            return t;
        }

        public String GetEventProviderName()
        {
            return m_strEventProviderName;
        }
        
        private String m_strEventItfName;
        private String m_strSrcItfName;
        private String m_strEventProviderName;
        private RuntimeAssembly m_asmImport;
        private RuntimeAssembly m_asmSrcItf;
    }
}
