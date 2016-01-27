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

    // Keep this enum in sync with tools\ngen\ngen.cpp and inc\mscoree.idl

[Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum HostProtectionResource
    {
        None                        = 0x0,
        //--------------------------------
        Synchronization             = 0x1,
        SharedState                 = 0x2,
        ExternalProcessMgmt         = 0x4,
        SelfAffectingProcessMgmt    = 0x8,
        ExternalThreading           = 0x10,
        SelfAffectingThreading      = 0x20,
        SecurityInfrastructure      = 0x40,
        UI                          = 0x80,
        MayLeakOnAbort              = 0x100,
        //---------------------------------
        All                         = 0x1ff,
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#if FEATURE_CORECLR
    // This needs to be in the asmmeta to enable SecAnnotate to successfully resolve and run the security rules. It gets marked
    // as internal by BCLRewriter so we are simply marking it as FriendAccessAllowed so it stays in the asmmeta.
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
#endif // FEATURE_CORECLR
#pragma warning disable 618
    sealed public class HostProtectionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private HostProtectionResource m_resources = HostProtectionResource.None;

        public HostProtectionAttribute()
#pragma warning disable 618
            : base( SecurityAction.LinkDemand )
#pragma warning restore 618
        {
        }

#pragma warning disable 618
        public HostProtectionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
#pragma warning disable 618
            if (action != SecurityAction.LinkDemand)
#pragma warning restore 618
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
            Contract.EndContractBlock();
        }

        public HostProtectionResource Resources {
            get { return m_resources; }
            set { m_resources = value; }
        }

        public bool Synchronization {
            get { return (m_resources & HostProtectionResource.Synchronization) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.Synchronization : m_resources & ~HostProtectionResource.Synchronization); }
        }

        public bool SharedState {
            get { return (m_resources & HostProtectionResource.SharedState) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.SharedState : m_resources & ~HostProtectionResource.SharedState); }
        }

        public bool ExternalProcessMgmt {
            get { return (m_resources & HostProtectionResource.ExternalProcessMgmt) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.ExternalProcessMgmt : m_resources & ~HostProtectionResource.ExternalProcessMgmt); }
        }

        public bool SelfAffectingProcessMgmt {
            get { return (m_resources & HostProtectionResource.SelfAffectingProcessMgmt) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.SelfAffectingProcessMgmt : m_resources & ~HostProtectionResource.SelfAffectingProcessMgmt); }
        }

        public bool ExternalThreading {
            get { return (m_resources & HostProtectionResource.ExternalThreading) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.ExternalThreading : m_resources & ~HostProtectionResource.ExternalThreading); }
        }

        public bool SelfAffectingThreading {
            get { return (m_resources & HostProtectionResource.SelfAffectingThreading) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.SelfAffectingThreading : m_resources & ~HostProtectionResource.SelfAffectingThreading); }
        }

[System.Runtime.InteropServices.ComVisible(true)]
        public bool SecurityInfrastructure {
            get { return (m_resources & HostProtectionResource.SecurityInfrastructure) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.SecurityInfrastructure : m_resources & ~HostProtectionResource.SecurityInfrastructure); }
        }

        public bool UI {
            get { return (m_resources & HostProtectionResource.UI) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.UI : m_resources & ~HostProtectionResource.UI); }
        }

        public bool MayLeakOnAbort {
            get { return (m_resources & HostProtectionResource.MayLeakOnAbort) != 0; }
            set { m_resources = (value ? m_resources | HostProtectionResource.MayLeakOnAbort : m_resources & ~HostProtectionResource.MayLeakOnAbort); }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new HostProtectionPermission( PermissionState.Unrestricted );
            }
            else
            {
                return new HostProtectionPermission( m_resources );
            }
        }
    }

    [Serializable]
    sealed internal class HostProtectionPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // GLOBALS
        //
        //------------------------------------------------------

        // This value is set by PermissionSet.FilterHostProtectionPermissions.  It is only used for
        // constructing a HostProtectionException object.  Changing it will not affect HostProtection.
        internal static volatile HostProtectionResource protectedResources = HostProtectionResource.None;

        //------------------------------------------------------
        //
        // MEMBERS
        //
        //------------------------------------------------------
        private HostProtectionResource m_resources;

        //------------------------------------------------------
        //
        // CONSTRUCTORS
        //
        //------------------------------------------------------
        public HostProtectionPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
                Resources = HostProtectionResource.All;
            else if (state == PermissionState.None)
                Resources = HostProtectionResource.None;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
        }

        public HostProtectionPermission(HostProtectionResource resources)
        {
            Resources = resources;
        }

        //------------------------------------------------------
        //
        // IPermission interface implementation
        //
        //------------------------------------------------------
        public bool IsUnrestricted()
        {
            return Resources == HostProtectionResource.All;
        }

        //------------------------------------------------------
        //
        // Properties
        //
        //------------------------------------------------------
        public HostProtectionResource Resources
        {
            set
            {
                if(value < HostProtectionResource.None || value > HostProtectionResource.All)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)value));
                Contract.EndContractBlock();
                m_resources = value;
            }

            get
            {
                return m_resources;
            }
        }

        //------------------------------------------------------
        //
        // IPermission interface implementation
        //
        //------------------------------------------------------
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return m_resources == HostProtectionResource.None;
            if(this.GetType() != target.GetType())
                throw new ArgumentException( Environment.GetResourceString("Argument_WrongType", this.GetType().FullName) );
            return ((uint)this.m_resources & (uint)((HostProtectionPermission)target).m_resources) == (uint)this.m_resources;
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return(this.Copy());
            if(this.GetType() != target.GetType())
                throw new ArgumentException( Environment.GetResourceString("Argument_WrongType", this.GetType().FullName) );
            HostProtectionResource newResources = (HostProtectionResource)((uint)this.m_resources | (uint)((HostProtectionPermission)target).m_resources);
            return new HostProtectionPermission(newResources);
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            if(this.GetType() != target.GetType())
                throw new ArgumentException( Environment.GetResourceString("Argument_WrongType", this.GetType().FullName) );
            HostProtectionResource newResources = (HostProtectionResource)((uint)this.m_resources & (uint)((HostProtectionPermission)target).m_resources);
            if(newResources == HostProtectionResource.None)
                return null;
            return new HostProtectionPermission(newResources);
        }

        public override IPermission Copy()
        {
            return new HostProtectionPermission(m_resources);
        }

#if FEATURE_CAS_POLICY
        //------------------------------------------------------
        //
        // XML
        //
        //------------------------------------------------------
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, this.GetType().FullName );
            if(IsUnrestricted())
                esd.AddAttribute( "Unrestricted", "true" );
            else
                esd.AddAttribute( "Resources", XMLUtil.BitFieldEnumToString( typeof( HostProtectionResource ), Resources ) );
            return esd;
        }

        public override void FromXml(SecurityElement esd)
        {
            CodeAccessPermission.ValidateElement( esd, this );
            if (XMLUtil.IsUnrestricted( esd ))
                Resources = HostProtectionResource.All;
            else
            {
                String resources = esd.Attribute( "Resources" );
                if (resources == null)
                    Resources = HostProtectionResource.None;
                else
                    Resources = (HostProtectionResource)Enum.Parse( typeof( HostProtectionResource ), resources );
            }
        }
#endif // FEATURE_CAS_POLICY

        //------------------------------------------------------
        //
        // OBJECT OVERRIDES
        //
        //------------------------------------------------------

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return HostProtectionPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.HostProtectionPermissionIndex;
        }
    }
}
