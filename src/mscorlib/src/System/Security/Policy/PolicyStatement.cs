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

#if FEATURE_CAS_POLICY
        // Evidence which was not verified but which was required to generate this policy statement.
        // This is not serialized, since once we serialize we lose the ability to verify the evidence,
        // meaning that restoring this state is meaningless.
        [NonSerialized]
        private List<IDelayEvaluatedEvidence> m_dependentEvidence;
#endif

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
            PolicyStatement copy = new PolicyStatement(m_permSet, Attributes, true); // The PolicyStatement .ctor will copy the permission set
#if FEATURE_CAS_POLICY
            if (HasDependentEvidence)
            {
                copy.m_dependentEvidence = new List<IDelayEvaluatedEvidence>(m_dependentEvidence);
            }
#endif

            return copy;
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

#if FEATURE_CAS_POLICY
        /// <summary>
        ///     Gets all of the delay evaluated evidence which needs to be verified before this policy can
        ///     be used.
        /// </summary>
        internal IEnumerable<IDelayEvaluatedEvidence> DependentEvidence
        {
            get
            {
                BCLDebug.Assert(HasDependentEvidence, "HasDependentEvidence");
                return m_dependentEvidence.AsReadOnly();
            }
        }

        /// <summary>
        ///     Determine if this policy dependent upon the evaluation of any delay evaluated evidence
        /// </summary>
        internal bool HasDependentEvidence
        {
            get { return m_dependentEvidence != null && m_dependentEvidence.Count > 0; }
        }

        /// <summary>
        ///     Add evidence which this policy statement is depending upon being verified to be valid.
        /// </summary>
        internal void AddDependentEvidence(IDelayEvaluatedEvidence dependentEvidence)
        {
            BCLDebug.Assert(dependentEvidence != null, "dependentEvidence != null");

            if (m_dependentEvidence == null)
            {
                m_dependentEvidence = new List<IDelayEvaluatedEvidence>();
            }

            m_dependentEvidence.Add(dependentEvidence);
        }
#endif

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

#if FEATURE_CAS_POLICY
            // If our code group generated a grant set based upon unverified evidence, or it generated a grant
            // set strictly less than that of a child group based upon unverified evidence, we need to keep
            // track of any unverified evidence our child group has.
            if (childPolicy.HasDependentEvidence)
            {
                bool childEvidenceNeedsVerification = m_permSet.IsSubsetOf(childPolicy.GetPermissionSetNoCopy()) &&
                                                      !childPolicy.GetPermissionSetNoCopy().IsSubsetOf(m_permSet);

                if (HasDependentEvidence || childEvidenceNeedsVerification)
                {
                    if (m_dependentEvidence == null)
                    {
                        m_dependentEvidence = new List<IDelayEvaluatedEvidence>();
                    }

                    m_dependentEvidence.AddRange(childPolicy.DependentEvidence);
                }
            }
#endif

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

#if FEATURE_CAS_POLICY

        public SecurityElement ToXml()
        {
            return ToXml( null );
        }

        public void FromXml( SecurityElement et )
        {
            FromXml( et, null );
        }

        public SecurityElement ToXml( PolicyLevel level )
        {
            return ToXml( level, false );
        }

        internal SecurityElement ToXml( PolicyLevel level, bool useInternal )
        {
            SecurityElement e = new SecurityElement( "PolicyStatement" );
            e.AddAttribute( "version", "1" );
            if (m_attributes != PolicyStatementAttribute.Nothing)
                e.AddAttribute( "Attributes", XMLUtil.BitFieldEnumToString( typeof( PolicyStatementAttribute ), m_attributes ) );            
            
            lock (this)
            {
                if (m_permSet != null)
                {
                    if (m_permSet is NamedPermissionSet)
                    {
                        // If the named permission set exists in the parent level of this
                        // policy struct, then just save the name of the permission set.
                        // Otherwise, serialize it like normal.
                
                        NamedPermissionSet namedPermSet = (NamedPermissionSet)m_permSet;
                        if (level != null && level.GetNamedPermissionSet( namedPermSet.Name ) != null)
                        {
                            e.AddAttribute( "PermissionSetName", namedPermSet.Name );
                        }
                        else
                        {
                            if (useInternal)
                                e.AddChild( namedPermSet.InternalToXml() );
                            else
                                e.AddChild( namedPermSet.ToXml() );
                        }
                    }
                    else
                    {
                        if (useInternal)
                            e.AddChild( m_permSet.InternalToXml() );
                        else
                            e.AddChild( m_permSet.ToXml() );
                    }
                }
            }
            
            return e;
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void FromXml( SecurityElement et, PolicyLevel level )
        {
            FromXml( et, level, false );
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void FromXml( SecurityElement et, PolicyLevel level, bool allowInternalOnly )
        {
            if (et == null)
                throw new ArgumentNullException( "et" );

            if (!et.Tag.Equals( "PolicyStatement" ))
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Argument_InvalidXMLElement" ),  "PolicyStatement", this.GetType().FullName ) );
            Contract.EndContractBlock();
        
            m_attributes = (PolicyStatementAttribute) 0;

            String strAttributes = et.Attribute( "Attributes" );

            if (strAttributes != null)
                m_attributes = (PolicyStatementAttribute)Enum.Parse( typeof( PolicyStatementAttribute ), strAttributes );

            lock (this)
            {
                m_permSet = null;

                if (level != null)
                {
                    String permSetName = et.Attribute( "PermissionSetName" );
    
                    if (permSetName != null)
                    {
                        m_permSet = level.GetNamedPermissionSetInternal( permSetName );

                        if (m_permSet == null)
                            m_permSet = new PermissionSet( PermissionState.None );
                    }
                }


                if (m_permSet == null)
                {
                    // There is no provided level, it is not a named permission set, or
                    // the named permission set doesn't exist in the provided level,
                    // so just create the class through reflection and decode normally.
        
                    SecurityElement e = et.SearchForChildByTag( "PermissionSet" );

                    if (e != null)
                    {
                        String className = e.Attribute( "class" );

                        if (className != null && (className.Equals( "NamedPermissionSet" ) ||
                                                  className.Equals( "System.Security.NamedPermissionSet" )))
                            m_permSet = new NamedPermissionSet( "DefaultName", PermissionState.None );
                        else
                            m_permSet = new PermissionSet( PermissionState.None );
                
                        try
                        {
                            m_permSet.FromXml( e, allowInternalOnly, true );
                        }
                        catch
                        {
                            // ignore any exceptions from the decode process.
                            // Note: we go ahead and use the permission set anyway.  This should be safe since
                            // the decode process should never give permission beyond what a proper decode would have
                            // given.
                        }
                    }
                    else
                    {
                        throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidXML" ) );
                    }
                }

                if (m_permSet == null) 
                    m_permSet = new PermissionSet( PermissionState.None );
            }    
        }


        [System.Security.SecurityCritical]  // auto-generated
        internal void FromXml( SecurityDocument doc, int position, PolicyLevel level, bool allowInternalOnly )
        {
            if (doc == null)
                throw new ArgumentNullException( "doc" );
            Contract.EndContractBlock();

            if (!doc.GetTagForElement( position ).Equals( "PolicyStatement" ))
                throw new ArgumentException( String.Format( CultureInfo.CurrentCulture, Environment.GetResourceString( "Argument_InvalidXMLElement" ),  "PolicyStatement", this.GetType().FullName ) );
        
            m_attributes = (PolicyStatementAttribute) 0;

            String strAttributes = doc.GetAttributeForElement( position, "Attributes" );

            if (strAttributes != null)
                m_attributes = (PolicyStatementAttribute)Enum.Parse( typeof( PolicyStatementAttribute ), strAttributes );

            lock (this)
            {
                m_permSet = null;

                if (level != null)
                {
                    String permSetName = doc.GetAttributeForElement( position, "PermissionSetName" );
    
                    if (permSetName != null)
                    {
                        m_permSet = level.GetNamedPermissionSetInternal( permSetName );

                        if (m_permSet == null)
                            m_permSet = new PermissionSet( PermissionState.None );
                    }
                }


                if (m_permSet == null)
                {
                    // There is no provided level, it is not a named permission set, or
                    // the named permission set doesn't exist in the provided level,
                    // so just create the class through reflection and decode normally.
        
                    ArrayList childPositions = doc.GetChildrenPositionForElement( position );
                    int positionPermissionSet = -1;

                    for (int i = 0; i < childPositions.Count; ++i)
                    {
                        if (doc.GetTagForElement( (int)childPositions[i] ).Equals( "PermissionSet" ))
                        {
                            positionPermissionSet = (int)childPositions[i];
                        }
                    }

                    if (positionPermissionSet != -1)
                    {
                        String className = doc.GetAttributeForElement( positionPermissionSet, "class" );

                        if (className != null && (className.Equals( "NamedPermissionSet" ) ||
                                                  className.Equals( "System.Security.NamedPermissionSet" )))
                            m_permSet = new NamedPermissionSet( "DefaultName", PermissionState.None );
                        else
                            m_permSet = new PermissionSet( PermissionState.None );
                
                        m_permSet.FromXml( doc, positionPermissionSet, allowInternalOnly );
                    }
                    else
                    {
                        throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidXML" ) );
                    }
                }

                if (m_permSet == null) 
                    m_permSet = new PermissionSet( PermissionState.None );
            }    
        }
#endif // FEATURE_CAS_POLICY


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

