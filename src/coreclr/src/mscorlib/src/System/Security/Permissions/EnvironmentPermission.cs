// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions {
    using System.Security;
    using System;
    using SecurityElement = System.Security.SecurityElement;
    using System.Security.Util;
    using System.IO;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum EnvironmentPermissionAccess
    {
        NoAccess = 0x00,
        Read = 0x01,
        Write = 0x02,
        AllAccess = 0x03,
    }
    
    [Serializable]
    internal class EnvironmentStringExpressionSet : StringExpressionSet
    {
        public EnvironmentStringExpressionSet()
            : base( true, null, false )
        {
        }
        
        public EnvironmentStringExpressionSet( String str )
            : base( true, str, false )
        {
        }
        
        protected override StringExpressionSet CreateNewEmpty()
        {
            return new EnvironmentStringExpressionSet();
        }

        protected override bool StringSubsetString( String left, String right, bool ignoreCase )
        {
            return (ignoreCase?(String.Compare( left, right, StringComparison.OrdinalIgnoreCase) == 0):
                (String.Compare( left, right, StringComparison.Ordinal) == 0));
        }

        protected override String ProcessWholeString( String str )
        {
            return str;
        }

        protected override String ProcessSingleString( String str )
        {
            return str;
        }

        [SecuritySafeCritical]
        public override string ToString()
        {
            // SafeCritical: we're not storing path information in the strings, so exposing them out is fine ...
            // they're just the same strings that came in to the .ctor.
            return base.UnsafeToString();
        }
    }
    
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class EnvironmentPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        private StringExpressionSet m_read;
        private StringExpressionSet m_write;
        private bool m_unrestricted;
    
        public EnvironmentPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
                m_unrestricted = true;
            else if (state == PermissionState.None)
                m_unrestricted = false;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
        }

        public EnvironmentPermission( EnvironmentPermissionAccess flag, String pathList )
        {
            SetPathList( flag, pathList );
        }
        
        public void SetPathList( EnvironmentPermissionAccess flag, String pathList )
        {
            VerifyFlag( flag );
            
            m_unrestricted = false;

            if ((flag & EnvironmentPermissionAccess.Read) != 0)
                m_read = null;
            
            if ((flag & EnvironmentPermissionAccess.Write) != 0)
                m_write = null;
            
            AddPathList( flag, pathList );
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddPathList( EnvironmentPermissionAccess flag, String pathList )
        {
            VerifyFlag( flag );
            
            if (FlagIsSet( flag, EnvironmentPermissionAccess.Read ))
            {
                if (m_read == null)
                    m_read = new EnvironmentStringExpressionSet();
                m_read.AddExpressions( pathList );
            }
            
            if (FlagIsSet( flag, EnvironmentPermissionAccess.Write ))
            {
                if (m_write == null)
                    m_write = new EnvironmentStringExpressionSet();
                m_write.AddExpressions( pathList );
            }
    
        }
    
        public String GetPathList( EnvironmentPermissionAccess flag )
        {
            VerifyFlag( flag );
            ExclusiveFlag( flag );
    
            if (FlagIsSet( flag, EnvironmentPermissionAccess.Read ))
            {
                if (m_read == null)
                {
                    return "";
                }
                return m_read.ToString();
            }
            
            if (FlagIsSet( flag, EnvironmentPermissionAccess.Write ))
            {
                if (m_write == null)
                {
                    return "";
                }
                return m_write.ToString();
            }
    
            /* not reached */
            
            return "";
        }     
        
            
        private void VerifyFlag( EnvironmentPermissionAccess flag )
        {
            if ((flag & ~EnvironmentPermissionAccess.AllAccess) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
            Contract.EndContractBlock();
        }
    
        private void ExclusiveFlag( EnvironmentPermissionAccess flag )
        {
            if (flag == EnvironmentPermissionAccess.NoAccess)
            {
                throw new ArgumentException( Environment.GetResourceString("Arg_EnumNotSingleFlag") ); 
            }
    
            if (((int)flag & ((int)flag-1)) != 0)
            {
                throw new ArgumentException( Environment.GetResourceString("Arg_EnumNotSingleFlag") );
            }
            Contract.EndContractBlock();
        }
        
        
        private bool FlagIsSet( EnvironmentPermissionAccess flag, EnvironmentPermissionAccess question )
        {
            return (flag & question) != 0;
        }
        
        private bool IsEmpty()
        {
            return (!m_unrestricted &&
                    (this.m_read == null || this.m_read.IsEmpty()) &&
                    (this.m_write == null || this.m_write.IsEmpty()));
        }
        
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsUnrestricted()
        {
            return m_unrestricted;
        }
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return this.IsEmpty();
            }

            try
            {
                EnvironmentPermission operand = (EnvironmentPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                    return ((this.m_read == null || this.m_read.IsSubsetOf( operand.m_read )) &&
                            (this.m_write == null || this.m_write.IsSubsetOf( operand.m_write )));
            }
            catch (InvalidCastException)
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override IPermission Intersect(IPermission target)
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
    
            EnvironmentPermission operand = (EnvironmentPermission)target;
    
            if (operand.IsUnrestricted())
            {
                return this.Copy();
            }
            
            StringExpressionSet intersectRead = this.m_read == null ? null : this.m_read.Intersect( operand.m_read );
            StringExpressionSet intersectWrite = this.m_write == null ? null : this.m_write.Intersect( operand.m_write );
            
            if ((intersectRead == null || intersectRead.IsEmpty()) &&
                (intersectWrite == null || intersectWrite.IsEmpty()))
            {
                return null;
            }
            
            EnvironmentPermission intersectPermission = new EnvironmentPermission(PermissionState.None);
            intersectPermission.m_unrestricted = false;
            intersectPermission.m_read = intersectRead;
            intersectPermission.m_write = intersectWrite;
            
            return intersectPermission;
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override IPermission Union(IPermission other)
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
    
            EnvironmentPermission operand = (EnvironmentPermission)other;
           
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new EnvironmentPermission( PermissionState.Unrestricted );
            }
    
            StringExpressionSet unionRead = this.m_read == null ? operand.m_read : this.m_read.Union( operand.m_read );
            StringExpressionSet unionWrite = this.m_write == null ? operand.m_write : this.m_write.Union( operand.m_write );
            
            if ((unionRead == null || unionRead.IsEmpty()) &&
                (unionWrite == null || unionWrite.IsEmpty()))
            {
                return null;
            }
            
            EnvironmentPermission unionPermission = new EnvironmentPermission(PermissionState.None);
            unionPermission.m_unrestricted = false;
            unionPermission.m_read = unionRead;
            unionPermission.m_write = unionWrite;
            
            return unionPermission;
        }    
        
        public override IPermission Copy()
        {
            EnvironmentPermission copy = new EnvironmentPermission(PermissionState.None);
            if (this.m_unrestricted)
            {
                copy.m_unrestricted = true;
            }
            else
            {
                copy.m_unrestricted = false;
                if (this.m_read != null)
                {
                    copy.m_read = this.m_read.Copy();
                }
                if (this.m_write != null)
                {
                    copy.m_write = this.m_write.Copy();
                }
    
            }
            return copy;   
        }
       
#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.EnvironmentPermission" );
            if (!IsUnrestricted())
            {
                if (this.m_read != null && !this.m_read.IsEmpty())
                {
                    esd.AddAttribute( "Read", SecurityElement.Escape( m_read.ToString() ) );
                }
                if (this.m_write != null && !this.m_write.IsEmpty())
                {
                    esd.AddAttribute( "Write", SecurityElement.Escape( m_write.ToString() ) );
                }
            }
            else
            {
                esd.AddAttribute( "Unrestricted", "true" );
            }
            return esd;
        }

        public override void FromXml(SecurityElement esd)
        {
            CodeAccessPermission.ValidateElement( esd, this );

            String et;
            
            if (XMLUtil.IsUnrestricted(esd))
            {
                m_unrestricted = true;
                return;
            }

            m_unrestricted = false;
            m_read = null;
            m_write = null;

            et = esd.Attribute( "Read" );
            if (et != null)
            {
                m_read = new EnvironmentStringExpressionSet( et );
            }
            
            et = esd.Attribute( "Write" );
            if (et != null)
            {
                m_write = new EnvironmentStringExpressionSet( et );
            }
    
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return EnvironmentPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.EnvironmentPermissionIndex;
        }
    }

}
