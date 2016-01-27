// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security 
{
    //PermissionSetEnumerator.cs
    
    using System;
    using System.Collections;
    using TokenBasedSetEnumerator = System.Security.Util.TokenBasedSetEnumerator;
    using TokenBasedSet = System.Security.Util.TokenBasedSet;
    
    internal class PermissionSetEnumerator : IEnumerator 
    {
        PermissionSetEnumeratorInternal enm;
        
        public Object Current
        {
            get
            {
                return enm.Current;
            }
        }

        public bool MoveNext()
        {
            return enm.MoveNext();
        }
        
        public void Reset()
        {
            enm.Reset();
        }
        
        internal PermissionSetEnumerator(PermissionSet permSet)
        {
            enm = new PermissionSetEnumeratorInternal(permSet);
        }
    }
    
    internal struct PermissionSetEnumeratorInternal 
    {
        private PermissionSet m_permSet;
        private TokenBasedSetEnumerator enm;
        
        public Object Current
        {
            get
            {
                return enm.Current;
            }
        }

        internal PermissionSetEnumeratorInternal(PermissionSet permSet)
        {
            m_permSet = permSet;
            enm = new TokenBasedSetEnumerator(permSet.m_permSet);
        }

        public int GetCurrentIndex()
        {
            return enm.Index;
        }
        
        public void Reset()
        {
            enm.Reset();
        }
        
        public bool MoveNext()
        {
            while (enm.MoveNext())
            {
                Object obj = enm.Current;
                IPermission perm = obj as IPermission;
                if (perm != null)
                {
                    enm.Current = perm;
                    return true;
                }

#if FEATURE_CAS_POLICY
                SecurityElement elem = obj as SecurityElement;

                if (elem != null)
                {
                    perm = m_permSet.CreatePermission(elem, enm.Index);
                    if (perm != null)
                    {
                        enm.Current = perm;
                        return true;
                    }
                }
#endif // FEATURE_CAS_POLICY
            }
            return false;
        }
    }
}

