// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Represents the policy associated with some piece of evidence
//
using System.Diagnostics.Contracts;
namespace System.Security.Policy {
    
    using System;
    using System.Security;
    using System.Security.Util;
    using Math = System.Math;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security.Permissions;
    using System.Text;
    using System.Globalization;
[Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    internal enum PolicyStatementAttribute
    {
        Nothing = 0x0,
        Exclusive = 0x01,
        LevelFinal = 0x02,
        All = 0x03,
    }
    
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed internal class PolicyStatement 
    {
        // The PermissionSet associated with this policy
        internal PermissionSet m_permSet;

        // The bitfield of inheritance properties associated with this policy
        internal PolicyStatementAttribute m_attributes;

        internal PolicyStatement()
        {
            m_permSet = null;
            m_attributes = PolicyStatementAttribute.Nothing;
        }
        
        public PolicyStatement( PermissionSet permSet )
            : this( permSet, PolicyStatementAttribute.Nothing )
        {
        }
        
        public PolicyStatement( PermissionSet permSet, PolicyStatementAttribute attributes )
        {
            if (permSet == null)
            {
                m_permSet = new PermissionSet( false );
            }
            else
            {
                m_permSet = permSet.Copy();
            }
            if (ValidProperties( attributes ))
            {
                m_attributes = attributes;
            }
        }

        public PermissionSet PermissionSet
        {
            get
            {
                lock (this)
                {
                    return m_permSet.Copy();
                }
            }
            
            set
            {
                lock (this)
                {
                    if (value == null)
                    {
                        m_permSet = new PermissionSet( false );
                    }
                    else
                    {
                        m_permSet = value.Copy();
                    }
                }
            }
        }

        private static bool ValidProperties( PolicyStatementAttribute attributes )
        {
            if ((attributes & ~(PolicyStatementAttribute.All)) == 0)
            {
                return true;
            }
            else
            {
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidFlag" ) );
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals( Object obj )
        {
            PolicyStatement other = obj as PolicyStatement;

            if (other == null)
                return false;

            if (this.m_attributes != other.m_attributes)
                return false;

            if (!Object.Equals( this.m_permSet, other.m_permSet ))
                return false;

            return true;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            int accumulator = (int)this.m_attributes;

            if (m_permSet != null)
                accumulator = accumulator ^ m_permSet.GetHashCode();

            return accumulator;
        }

    }
}

