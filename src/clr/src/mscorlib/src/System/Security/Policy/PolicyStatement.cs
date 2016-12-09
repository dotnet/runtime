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
    public enum PolicyStatementAttribute
    {
        Nothing = 0x0,
        Exclusive = 0x01,
        LevelFinal = 0x02,
        All = 0x03,
    }
    
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class PolicyStatement : ISecurityPolicyEncodable, ISecurityEncodable
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
        
        private PolicyStatement( PermissionSet permSet, PolicyStatementAttribute attributes, bool copy )
        {
            if (permSet != null)
            {
                if (copy)
                    m_permSet = permSet.Copy();
                else
                    m_permSet = permSet;
            }
            else
            {
                m_permSet = new PermissionSet( false );
            }
                
            m_attributes = attributes;
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
        
        internal void SetPermissionSetNoCopy( PermissionSet permSet )
        {
            m_permSet = permSet;
        }
        
        internal PermissionSet GetPermissionSetNoCopy()
        {
            lock (this)
            {
                return m_permSet;
            }
        }
        
        public PolicyStatementAttribute Attributes
        {
            get
            {
                return m_attributes;
            }
            
            set
            {
                if (ValidProperties( value ))
                {
                    m_attributes = value;
                }
            }
        }

        public PolicyStatement Copy()
        {
            // The PolicyStatement .ctor will copy the permission set
            return new PolicyStatement(m_permSet, Attributes, true);
        }

        public String AttributeString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
            
                bool first = true;
            
                if (GetFlag((int) PolicyStatementAttribute.Exclusive ))
                {
                    sb.Append( "Exclusive" );
                    first = false;
                }
                if (GetFlag((int) PolicyStatementAttribute.LevelFinal ))
                {
                    if (!first)
                        sb.Append( " " );
                    sb.Append( "LevelFinal" );
                }
            
                return sb.ToString();
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
        
        private bool GetFlag( int flag )
        {
            return (flag & (int)m_attributes) != 0;
        }

        /// <summary>
        ///     Union a child policy statement into this policy statement
        /// </summary>
        internal void InplaceUnion(PolicyStatement childPolicy)
        {
            BCLDebug.Assert(childPolicy != null, "childPolicy != null");

            if (((Attributes & childPolicy.Attributes) & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
            {
                throw new PolicyException(Environment.GetResourceString( "Policy_MultipleExclusive" ));
            }

            // We need to merge together our grant set and attributes.  The result of this merge is
            // dependent upon if we're merging a child marked exclusive or not.  If the child is not
            // exclusive, we need to union in its grant set and or in its attributes. However, if the child
            // is exclusive then it is the only code group which should have an effect on the resulting
            // grant set and therefore our grant should be ignored.
            if ((childPolicy.Attributes & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
            {
                m_permSet = childPolicy.GetPermissionSetNoCopy();
                Attributes = childPolicy.Attributes;
            }
            else
            {
                m_permSet.InplaceUnion(childPolicy.GetPermissionSetNoCopy());
                Attributes = Attributes | childPolicy.Attributes;
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

