// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.IO;
    using System.Security.Util;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [ComVisible(true)]
    [Flags]
    [Serializable]
    public enum ReflectionPermissionFlag
    {
        NoFlags = 0x00,
        [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        TypeInformation = 0x01,
        MemberAccess = 0x02,
        [Obsolete("This permission is no longer used by the CLR.")]
        ReflectionEmit = 0x04,
        [ComVisible(false)]
        RestrictedMemberAccess = 0x08,
        [Obsolete("This permission has been deprecated. Use PermissionState.Unrestricted to get full access.")]
        AllFlags = 0x07
    }

    [ComVisible(true)]
    [Serializable]
    sealed public class ReflectionPermission
           : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        // ReflectionPermissionFlag.AllFlags doesn't contain the new value RestrictedMemberAccess,
        // but we cannot change its value now because that will break apps that have that old value baked in. 
        // We should use this const that truely contains "all" flags instead of ReflectionPermissionFlag.AllFlags.
#pragma warning disable 618
        internal const ReflectionPermissionFlag AllFlagsAndMore = ReflectionPermissionFlag.AllFlags | ReflectionPermissionFlag.RestrictedMemberAccess;
#pragma warning restore 618

        private ReflectionPermissionFlag m_flags;

        //
        // Public Constructors
        //
        
        public ReflectionPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted( true );
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted( false );
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }    
        
         // Parameters:
         //
        public ReflectionPermission(ReflectionPermissionFlag flag)
        {
            VerifyAccess(flag);
            
            SetUnrestricted(false);
            m_flags = flag;
        }
    
        //------------------------------------------------------
        //
        // PRIVATE AND PROTECTED MODIFIERS 
        //
        //------------------------------------------------------
        
        
        private void SetUnrestricted(bool unrestricted)
        {
            if (unrestricted)
            {
                m_flags = ReflectionPermission.AllFlagsAndMore;
            }
            else
            {
                Reset();
            }
        }
        
        
        private void Reset()
        {
            m_flags = ReflectionPermissionFlag.NoFlags;
        }    
        
     
        public ReflectionPermissionFlag Flags
        {
            set
            {
                VerifyAccess(value);
            
                m_flags = value;
            }
            
            get
            {
                return m_flags;
            }
        }
        
            
    #if ZERO   // Do not remove this code, useful for debugging
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ReflectionPermission(");
            if (IsUnrestricted())
            {
                sb.Append("Unrestricted");
            }
            else
            {
                if (GetFlag(ReflectionPermissionFlag.TypeInformation))
                    sb.Append("TypeInformation; ");
                if (GetFlag(ReflectionPermissionFlag.MemberAccess))
                    sb.Append("MemberAccess; ");
#pragma warning disable 618
                if (GetFlag(ReflectionPermissionFlag.ReflectionEmit))
                    sb.Append("ReflectionEmit; ");
#pragma warning restore 618
            }
            
            sb.Append(")");
            return sb.ToString();
        }
    #endif
    
    
        //
        // CodeAccessPermission implementation
        //
        
        public bool IsUnrestricted()
        {
            return m_flags == ReflectionPermission.AllFlagsAndMore;
        }
        
        //
        // IPermission implementation
        //
        
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
            
            ReflectionPermission operand = (ReflectionPermission)other;
    
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new ReflectionPermission( PermissionState.Unrestricted );
            }
            else
            {
                ReflectionPermissionFlag flag_union = (ReflectionPermissionFlag)(m_flags | operand.m_flags);
                return(new ReflectionPermission(flag_union));
            }
        }  
        
        
        
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return m_flags == ReflectionPermissionFlag.NoFlags;
            }

            try
            {
                ReflectionPermission operand = (ReflectionPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                    return (((int)this.m_flags) & ~((int)operand.m_flags)) == 0;
            }
            catch (InvalidCastException)
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }                

        }
        
        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }

            ReflectionPermission operand = (ReflectionPermission)target;

            ReflectionPermissionFlag newFlags = operand.m_flags & this.m_flags;
            
            if (newFlags == ReflectionPermissionFlag.NoFlags)
                return null;
            else
                return new ReflectionPermission( newFlags );
        }
    
        public override IPermission Copy()
        {
            if (this.IsUnrestricted())
            {
                return new ReflectionPermission(PermissionState.Unrestricted);
            }
            else
            {
                return new ReflectionPermission((ReflectionPermissionFlag)m_flags);
            }
        }
        
        
        //
        // IEncodable Interface 
    
        private
        void VerifyAccess(ReflectionPermissionFlag type)
        {
            if ((type & ~ReflectionPermission.AllFlagsAndMore) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)type));
            Contract.EndContractBlock();
        }
        
#if FEATURE_CAS_POLICY
        //------------------------------------------------------
        //
        // PUBLIC ENCODING METHODS
        //
        //------------------------------------------------------
        
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.ReflectionPermission" );
            if (!IsUnrestricted())
            {
                esd.AddAttribute( "Flags", XMLUtil.BitFieldEnumToString( typeof( ReflectionPermissionFlag ), m_flags ) );
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
            if (XMLUtil.IsUnrestricted( esd ))
            {
                m_flags = ReflectionPermission.AllFlagsAndMore;
                return;
            }
           
            Reset () ;
            SetUnrestricted (false) ;
    
            String flags = esd.Attribute( "Flags" );
            if (flags != null)
                m_flags = (ReflectionPermissionFlag)Enum.Parse( typeof( ReflectionPermissionFlag ), flags );
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return ReflectionPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.ReflectionPermissionIndex;
        }
    }
}
