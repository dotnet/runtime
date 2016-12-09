// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.IO;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    // The csharp compiler requires these types to be public, but they are not used elsewhere.
    [Obsolete("SecurityPermissionFlag is no longer accessible to application code.")]
    public enum SecurityPermissionFlag
    {
        NoFlags = 0x00,
        /* The following enum value is used in the EE (ASSERT_PERMISSION in security.cpp)
         * Should this value change, make corresponding changes there
         */ 
        Assertion = 0x01,
        UnmanagedCode = 0x02,       // Update vm\Security.h if you change this !
        SkipVerification = 0x04,    // Update vm\Security.h if you change this !
        Execution = 0x08,
        ControlThread = 0x10,
        ControlEvidence = 0x20,
        ControlPolicy = 0x40,
        SerializationFormatter = 0x80,
        ControlDomainPolicy = 0x100,
        ControlPrincipal = 0x200,
        ControlAppDomain = 0x400,
        RemotingConfiguration = 0x800,
        Infrastructure = 0x1000,
        BindingRedirects = 0x2000,
        AllFlags = 0x3fff,
    }

[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class SecurityPermission 
           : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
#pragma warning disable 618
        private SecurityPermissionFlag m_flags;
#pragma warning restore 618
        
        //
        // Public Constructors
        //
    
        public SecurityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted( true );
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted( false );
                Reset();
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }
        
        
        // SecurityPermission
        //
#pragma warning disable 618
        public SecurityPermission(SecurityPermissionFlag flag)
#pragma warning restore 618
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
#pragma warning disable 618
                m_flags = SecurityPermissionFlag.AllFlags;
#pragma warning restore 618
            }
        }
    
        private void Reset()
        {
#pragma warning disable 618
            m_flags = SecurityPermissionFlag.NoFlags;
#pragma warning restore 618
        }
        
        
#pragma warning disable 618
        public SecurityPermissionFlag Flags
#pragma warning restore 618
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
        
        //
        // CodeAccessPermission methods
        // 
        
       /*
         * IPermission interface implementation
         */
         
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return m_flags == 0;
            }
        
            SecurityPermission operand = target as SecurityPermission;
            if (operand != null)
            {
                return (((int)this.m_flags) & ~((int)operand.m_flags)) == 0;
            }
            else
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }

        }
        
        public override IPermission Union(IPermission target) {
            if (target == null) return(this.Copy());
            if (!VerifyType(target)) {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            SecurityPermission sp_target = (SecurityPermission) target;
            if (sp_target.IsUnrestricted() || IsUnrestricted()) {
                return(new SecurityPermission(PermissionState.Unrestricted));
            }
#pragma warning disable 618
            SecurityPermissionFlag flag_union = (SecurityPermissionFlag)(m_flags | sp_target.m_flags);
#pragma warning restore 618
            return(new SecurityPermission(flag_union));
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
            
            SecurityPermission operand = (SecurityPermission)target;
#pragma warning disable 618
            SecurityPermissionFlag isectFlags = SecurityPermissionFlag.NoFlags;
#pragma warning restore 618
           
            if (operand.IsUnrestricted())
            {
                if (this.IsUnrestricted())
                    return new SecurityPermission(PermissionState.Unrestricted);
                else
#pragma warning disable 618
                    isectFlags = (SecurityPermissionFlag)this.m_flags;
#pragma warning restore 618
            }
            else if (this.IsUnrestricted())
            {
#pragma warning disable 618
                isectFlags = (SecurityPermissionFlag)operand.m_flags;
#pragma warning restore 618
            }
            else
            {
#pragma warning disable 618
                isectFlags = (SecurityPermissionFlag)m_flags & (SecurityPermissionFlag)operand.m_flags;
#pragma warning restore 618
            }
            
            if (isectFlags == 0)
                return null;
            else
                return new SecurityPermission(isectFlags);
        }
    
        public override IPermission Copy()
        {
            if (IsUnrestricted())
                return new SecurityPermission(PermissionState.Unrestricted);
            else
#pragma warning disable 618
                return new SecurityPermission((SecurityPermissionFlag)m_flags);
#pragma warning restore 618
        }
    
        public bool IsUnrestricted()
        {
#pragma warning disable 618
            return m_flags == SecurityPermissionFlag.AllFlags;
#pragma warning restore 618
        }
        
        private
#pragma warning disable 618
        void VerifyAccess(SecurityPermissionFlag type)
#pragma warning restore 618
        {
#pragma warning disable 618
            if ((type & ~SecurityPermissionFlag.AllFlags) != 0)
#pragma warning restore 618
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)type));
            Contract.EndContractBlock();
        }

        //
        // Object Overrides
        //

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return SecurityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.SecurityPermissionIndex;
        }
    }
}
