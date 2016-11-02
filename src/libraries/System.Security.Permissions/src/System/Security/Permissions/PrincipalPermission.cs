// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//  PrincipalPermission.cs
// 
// <OWNER>Microsoft</OWNER>
//
 
namespace System.Security.Permissions
{
    using System;
    using SecurityElement = System.Security.SecurityElement;
    using System.Security.Util;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;    
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics.Contracts;
    
    [Serializable]
    internal class IDRole
    {
        internal bool m_authenticated;
        internal String m_id;
        internal String m_role;
 
#if !FEATURE_PAL
        // cache the translation from name to Sid for the case of WindowsPrincipal.
        [NonSerialized]
        private SecurityIdentifier m_sid = null;
        internal SecurityIdentifier Sid {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                if (String.IsNullOrEmpty(m_role))
                    return null;
 
                if (m_sid == null) {
                    NTAccount ntAccount = new NTAccount(m_role);
                    IdentityReferenceCollection source = new IdentityReferenceCollection(1);
                    source.Add(ntAccount);
                    IdentityReferenceCollection target = NTAccount.Translate(source, typeof(SecurityIdentifier), false);
                    m_sid = target[0] as SecurityIdentifier;
                }
 
                return m_sid;
            }
        }
#endif // !FEATURE_PAL
 
#if FEATURE_CAS_POLICY
        internal SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement( "Identity" );
            
            if (m_authenticated)
                root.AddAttribute( "Authenticated", "true" );
                
            if (m_id != null)
            {
                root.AddAttribute( "ID", SecurityElement.Escape( m_id ) );
            }
               
            if (m_role != null)
            {
                root.AddAttribute( "Role", SecurityElement.Escape( m_role ) );
            }
                            
            return root;
        }
        
        internal void FromXml( SecurityElement e )
        {
            String elAuth = e.Attribute( "Authenticated" );
            if (elAuth != null)
            {
                m_authenticated = String.Compare( elAuth, "true", StringComparison.OrdinalIgnoreCase) == 0;
            }
            else
            {
                m_authenticated = false;
            }
           
            String elID = e.Attribute( "ID" );
            if (elID != null)
            {
                m_id = elID;
            }
            else
            {
                m_id = null;
            }
            
            String elRole = e.Attribute( "Role" );
            if (elRole != null)
            {
                m_role = elRole;
            }
            else
            {
                m_role = null;
            }
        }
#endif // FEATURE_CAS_POLICY
 
        public override int GetHashCode()
        {
            return ((m_authenticated ? 0 : 101) +
                        (m_id == null ? 0 : m_id.GetHashCode()) +
                        (m_role == null? 0 : m_role.GetHashCode()));
        }    
 
    }
    
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class PrincipalPermission : IPermission, IUnrestrictedPermission, ISecurityEncodable, IBuiltInPermission
    {
        private IDRole[] m_array;
        
        public PrincipalPermission( PermissionState state )
        {
            if (state == PermissionState.Unrestricted)
            {
                m_array = new IDRole[1];
                m_array[0] = new IDRole();
                m_array[0].m_authenticated = true;
                m_array[0].m_id = null;
                m_array[0].m_role = null;
            }
            else if (state == PermissionState.None)
            {
                m_array = new IDRole[1];
                m_array[0] = new IDRole();
                m_array[0].m_authenticated = false;
                m_array[0].m_id = "";
                m_array[0].m_role = "";
            }
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
        }
        
        public PrincipalPermission( String name, String role )
        {
            m_array = new IDRole[1];
            m_array[0] = new IDRole();
            m_array[0].m_authenticated = true;
            m_array[0].m_id = name;
            m_array[0].m_role = role;
        }
    
        public PrincipalPermission( String name, String role, bool isAuthenticated )
        {
            m_array = new IDRole[1];
            m_array[0] = new IDRole();
            m_array[0].m_authenticated = isAuthenticated;
            m_array[0].m_id = name;
            m_array[0].m_role = role;
        }        
    
        private PrincipalPermission( IDRole[] array )
        {
            m_array = array;
        }
    
        private bool IsEmpty()
        {
            for (int i = 0; i < m_array.Length; ++i)
            {
                if ((m_array[i].m_id == null || !m_array[i].m_id.Equals( "" )) ||
                    (m_array[i].m_role == null || !m_array[i].m_role.Equals( "" )) ||
                    m_array[i].m_authenticated)
                {
                    return false;
                }
            }
            return true;
        }
        
        private bool VerifyType(IPermission perm)
        {
            // if perm is null, then obviously not of the same type
            if ((perm == null) || (perm.GetType() != this.GetType())) {
                return(false);
            } else {
                return(true);
            }
        }
         
        
        public bool IsUnrestricted()
        {
            for (int i = 0; i < m_array.Length; ++i)
            {
                if (m_array[i].m_id != null || m_array[i].m_role != null || !m_array[i].m_authenticated)
                {
                    return false;
                }
            }
            return true;
        }
 
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return this.IsEmpty();
            }
        
            try
            {
                PrincipalPermission operand = (PrincipalPermission)target;
            
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                {
                    for (int i = 0; i < this.m_array.Length; ++i)
                    {
                        bool foundMatch = false;
                
                        for (int j = 0; j < operand.m_array.Length; ++j)
                        {
                            if (operand.m_array[j].m_authenticated == this.m_array[i].m_authenticated &&
                                (operand.m_array[j].m_id == null ||
                                 (this.m_array[i].m_id != null && this.m_array[i].m_id.Equals( operand.m_array[j].m_id ))) &&
                                (operand.m_array[j].m_role == null ||
                                 (this.m_array[i].m_role != null && this.m_array[i].m_role.Equals( operand.m_array[j].m_role ))))
                            {
                                foundMatch = true;
                                break;
                            }
                        }
                    
                        if (!foundMatch)
                            return false;
                    }
                                            
                    return true;
                }
            }
            catch (InvalidCastException)
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }                
 
            
        }
        
        public IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            else if (this.IsUnrestricted())
            {
                return target.Copy();
            }
    
            PrincipalPermission operand = (PrincipalPermission)target;
    
            if (operand.IsUnrestricted())
            {
                return this.Copy();
            }
            
            List<IDRole> idroles = null;
            
            for (int i = 0; i < this.m_array.Length; ++i)
            {
                for (int j = 0; j < operand.m_array.Length; ++j)
                {
                    if (operand.m_array[j].m_authenticated == this.m_array[i].m_authenticated)
                    {
                        if (operand.m_array[j].m_id == null ||
                            this.m_array[i].m_id == null ||
                            this.m_array[i].m_id.Equals( operand.m_array[j].m_id ))
                        {
                            if (idroles == null)
                            {
                                idroles = new List<IDRole>();
                            }
                    
                            IDRole idrole = new IDRole();
                            
                            idrole.m_id = operand.m_array[j].m_id == null ? this.m_array[i].m_id : operand.m_array[j].m_id;
                            
                            if (operand.m_array[j].m_role == null ||
                                this.m_array[i].m_role == null ||
                                this.m_array[i].m_role.Equals( operand.m_array[j].m_role))
                            {
                                idrole.m_role = operand.m_array[j].m_role == null ? this.m_array[i].m_role : operand.m_array[j].m_role;
                            }
                            else
                            {
                                idrole.m_role = "";
                            }
                            
                            idrole.m_authenticated = operand.m_array[j].m_authenticated;
                            
                            idroles.Add( idrole );
                        }
                        else if (operand.m_array[j].m_role == null ||
                                 this.m_array[i].m_role == null ||
                                 this.m_array[i].m_role.Equals( operand.m_array[j].m_role))
                        {
                            if (idroles == null)
                            {
                                idroles = new List<IDRole>();
                            }
 
                            IDRole idrole = new IDRole();
                            
                            idrole.m_id = "";
                            idrole.m_role = operand.m_array[j].m_role == null ? this.m_array[i].m_role : operand.m_array[j].m_role;
                            idrole.m_authenticated = operand.m_array[j].m_authenticated;
                            
                            idroles.Add( idrole );
                        }
                    }
                }
            }
            
            if (idroles == null)
            {
                return null;
            }
            else
            {
                IDRole[] idrolesArray = new IDRole[idroles.Count];
                
                IEnumerator idrolesEnumerator = idroles.GetEnumerator();
                int index = 0;
                
                while (idrolesEnumerator.MoveNext())
                {
                    idrolesArray[index++] = (IDRole)idrolesEnumerator.Current;
                }
                                                                
                return new PrincipalPermission( idrolesArray );
            }
        }                                                    
        
        public IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(other))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
    
            PrincipalPermission operand = (PrincipalPermission)other;
           
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new PrincipalPermission( PermissionState.Unrestricted );
            }
    
            // Now we have to do a real union
            
            int combinedLength = this.m_array.Length + operand.m_array.Length;
            IDRole[] idrolesArray = new IDRole[combinedLength];
            
            int i, j;
            for (i = 0; i < this.m_array.Length; ++i)
            {
                idrolesArray[i] = this.m_array[i];
            }
            
            for (j = 0; j < operand.m_array.Length; ++j)
            {
                idrolesArray[i+j] = operand.m_array[j];
            }
            
            return new PrincipalPermission( idrolesArray );
 
        }    
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals(Object obj)
        {
            IPermission perm = obj as IPermission;
            if(obj != null && perm == null)
                return false;
            if(!this.IsSubsetOf(perm))
                return false;
            if(perm != null && !perm.IsSubsetOf(this))
                return false;
            return true;
        }
 
        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            int hash = 0;
            int i;
            for(i = 0; i < m_array.Length; i++)
                hash += m_array[i].GetHashCode();
            return hash;
        }    
 
        public IPermission Copy()
        {
            return new PrincipalPermission( m_array );  
        }
 
        [System.Security.SecurityCritical]  // auto-generated
        private void ThrowSecurityException()
        {
            System.Reflection.AssemblyName name = null;
            System.Security.Policy.Evidence evid = null;
            PermissionSet.s_fullTrust.Assert();                    
            try
            {
                System.Reflection.Assembly asm = Reflection.Assembly.GetCallingAssembly();
                name = asm.GetName();
#if FEATURE_CAS_POLICY
                if(asm != Assembly.GetExecutingAssembly()) // this condition is to avoid having to marshal mscorlib's evidence (which is always in teh default domain) to the current domain
                    evid = asm.Evidence;
#endif // FEATURE_CAS_POLICY
            }
            catch
            {
            }
            PermissionSet.RevertAssert();
            throw new SecurityException(Environment.GetResourceString("Security_PrincipalPermission"), name, null, null, null, SecurityAction.Demand, this, this, evid);
        }
 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Demand()
        {
            IPrincipal principal = null;
#if FEATURE_IMPERSONATION
            new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
            principal = Thread.CurrentPrincipal;
#endif // FEATURE_IMPERSONATION
 
            if (principal == null)
                ThrowSecurityException();
 
            if (m_array == null)
                return;
 
            // A demand passes when the grant satisfies all entries.
 
            int count = this.m_array.Length;
            bool foundMatch = false;
            for (int i = 0; i < count; ++i)
            {
                // If the demand is authenticated, we need to check the identity and role
 
                if (m_array[i].m_authenticated)
                {
                    IIdentity identity = principal.Identity;
 
                    if ((identity.IsAuthenticated &&
                         (m_array[i].m_id == null || String.Compare( identity.Name, m_array[i].m_id, StringComparison.OrdinalIgnoreCase) == 0)))
                    {
                        if (m_array[i].m_role == null) {
                            foundMatch = true;
                        }
                        else {
#if !FEATURE_PAL && FEATURE_IMPERSONATION 
                            WindowsPrincipal wp = principal as WindowsPrincipal;
                            if (wp != null && m_array[i].Sid != null)
                                foundMatch = wp.IsInRole(m_array[i].Sid);
                            else
#endif // !FEATURE_PAL && FEATURE_IMPERSONATION 
                                foundMatch = principal.IsInRole(m_array[i].m_role);
                        }
 
                        if (foundMatch)
                            break;
                    }
                }
                else
                {
                    foundMatch = true;
                    break;
                }
            }
 
            if (!foundMatch)
                ThrowSecurityException();
        }
        
#if FEATURE_CAS_POLICY
        public SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement( "IPermission" );
            
            XMLUtil.AddClassAttribute( root, this.GetType(), "System.Security.Permissions.PrincipalPermission" );
            // If you hit this assert then most likely you are trying to change the name of this class. 
            // This is ok as long as you change the hard coded string above and change the assert below.
            Contract.Assert( this.GetType().FullName.Equals( "System.Security.Permissions.PrincipalPermission" ), "Class name changed!" );
 
            root.AddAttribute( "version", "1" );
            
            int count = m_array.Length;
            for (int i = 0; i < count; ++i)
            {
                root.AddChild( m_array[i].ToXml() );
            }
            
            return root;
        }
 
        public void FromXml(SecurityElement elem)
        {
            CodeAccessPermission.ValidateElement( elem, this );
 
            if (elem.InternalChildren != null && elem.InternalChildren.Count != 0)
            { 
                int numChildren = elem.InternalChildren.Count;
                int count = 0;
                
                m_array = new IDRole[numChildren];
            
                IEnumerator enumerator = elem.Children.GetEnumerator();
            
                while (enumerator.MoveNext())  
                {
                    IDRole idrole = new IDRole();
                    
                    idrole.FromXml( (SecurityElement)enumerator.Current );
                    
                    m_array[count++] = idrole;
                }
            }
            else
                m_array = new IDRole[0];
        }
                 
        public override String ToString()
        {
            return ToXml().ToString();
        }    
#endif // FEATURE_CAS_POLICY
 
        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return PrincipalPermission.GetTokenIndex();
        }
 
        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.PrincipalPermissionIndex;
        }
 
    }
 
}