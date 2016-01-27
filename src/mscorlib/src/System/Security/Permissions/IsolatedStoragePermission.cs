// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security.Permissions {
    
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Util;
    using System.Globalization;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum IsolatedStorageContainment {
        None                                    = 0x00,
        DomainIsolationByUser                   = 0x10,
        ApplicationIsolationByUser              = 0x15,
        AssemblyIsolationByUser                 = 0x20,
        DomainIsolationByMachine                = 0x30,
        AssemblyIsolationByMachine              = 0x40,
        ApplicationIsolationByMachine          = 0x45,
        DomainIsolationByRoamingUser            = 0x50,
        AssemblyIsolationByRoamingUser          = 0x60,
        ApplicationIsolationByRoamingUser          = 0x65,
        AdministerIsolatedStorageByUser         = 0x70,
        //AdministerIsolatedStorageByMachine    = 0x80,
        UnrestrictedIsolatedStorage             = 0xF0
    };

    
    [Serializable]
#if !FEATURE_CORECLR
    [SecurityPermissionAttribute( SecurityAction.InheritanceDemand, ControlEvidence = true, ControlPolicy = true )]
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    abstract public class IsolatedStoragePermission
           : CodeAccessPermission, IUnrestrictedPermission
    {

        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------
        
        /// <internalonly/>
        internal long m_userQuota;
        /// <internalonly/>
        internal long m_machineQuota;
        /// <internalonly/>
        internal long m_expirationDays;
        /// <internalonly/>
        internal bool m_permanentData;
        /// <internalonly/>
        internal IsolatedStorageContainment m_allowed;
    
        //------------------------------------------------------
        //
        // CONSTRUCTORS
        //
        //------------------------------------------------------
    
        protected IsolatedStoragePermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_userQuota = Int64.MaxValue;
                m_machineQuota = Int64.MaxValue;
                m_expirationDays = Int64.MaxValue ;
                m_permanentData = true;
                m_allowed = IsolatedStorageContainment.UnrestrictedIsolatedStorage;
            }
            else if (state == PermissionState.None)
            {
                m_userQuota = 0;
                m_machineQuota = 0;
                m_expirationDays = 0;
                m_permanentData = false;
                m_allowed = IsolatedStorageContainment.None;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }
    
        internal IsolatedStoragePermission(IsolatedStorageContainment UsageAllowed, 
            long ExpirationDays, bool PermanentData)

        {
                m_userQuota = 0;    // typical demand won't include quota
                m_machineQuota = 0; // typical demand won't include quota
                m_expirationDays = ExpirationDays;
                m_permanentData = PermanentData;
                m_allowed = UsageAllowed;
        }
    
        internal IsolatedStoragePermission(IsolatedStorageContainment UsageAllowed, 
            long ExpirationDays, bool PermanentData, long UserQuota)

        {
                m_machineQuota = 0;
                m_userQuota = UserQuota;
                m_expirationDays = ExpirationDays;
                m_permanentData = PermanentData;
                m_allowed = UsageAllowed;
        }
    
        
        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------
        
        // properties
        public long UserQuota {
            set{
                m_userQuota = value;
            }
            get{
                return m_userQuota;
            }
        }

#if false
        internal long MachineQuota {
            set{
                m_machineQuota = value;
            }
            get{
                return m_machineQuota;
            }
        }
        internal long ExpirationDays {
            set{
                m_expirationDays = value;
            }
            get{
                return m_expirationDays;
            }
        }
        internal bool PermanentData {
            set{
                m_permanentData = value;
            }
            get{
                return m_permanentData;
            }
        }
#endif

        public IsolatedStorageContainment UsageAllowed {
            set{
                m_allowed = value;
            }
            get{
                return m_allowed;
            }
        }

    
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsUnrestricted()
        {
            return m_allowed == IsolatedStorageContainment.UnrestrictedIsolatedStorage;
        }
        
    
        //------------------------------------------------------
        //
        // INTERNAL METHODS
        //
        //------------------------------------------------------
        internal static long min(long x,long y) {return x>y?y:x;}
        internal static long max(long x,long y) {return x<y?y:x;}

#if FEATURE_CAS_POLICY
        //------------------------------------------------------
        //
        // PUBLIC ENCODING METHODS
        //
        //------------------------------------------------------
        
        private const String _strUserQuota   = "UserQuota";
        private const String _strMachineQuota   = "MachineQuota";
        private const String _strExpiry  = "Expiry";
        private const String _strPermDat = "Permanent";

        public override SecurityElement ToXml()
        {
            return ToXml ( this.GetType().FullName );
        }
    
        internal SecurityElement ToXml(String permName)
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, permName );
            if (!IsUnrestricted())
            {
                esd.AddAttribute( "Allowed", Enum.GetName( typeof( IsolatedStorageContainment ), m_allowed ) );
                if (m_userQuota>0)
                {
                    esd.AddAttribute(_strUserQuota, (m_userQuota).ToString(CultureInfo.InvariantCulture)) ;
                }
                if (m_machineQuota>0)
                {
                    esd.AddAttribute(_strMachineQuota, (m_machineQuota).ToString(CultureInfo.InvariantCulture)) ;
                }
                if (m_expirationDays>0)
                {
                    esd.AddAttribute( _strExpiry, (m_expirationDays).ToString(CultureInfo.InvariantCulture)) ;
                }
                if (m_permanentData)
                {
                    esd.AddAttribute(_strPermDat, (m_permanentData).ToString()) ;
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

            m_allowed = IsolatedStorageContainment.None;    // default if no match

            if (XMLUtil.IsUnrestricted(esd))
            {
                m_allowed = IsolatedStorageContainment.UnrestrictedIsolatedStorage;
            }
            else
            {
                String allowed = esd.Attribute( "Allowed" );

                if (allowed != null)
                    m_allowed = (IsolatedStorageContainment)Enum.Parse( typeof( IsolatedStorageContainment ), allowed );
            }
                    
            if (m_allowed == IsolatedStorageContainment.UnrestrictedIsolatedStorage)
            {
                m_userQuota = Int64.MaxValue;
                m_machineQuota = Int64.MaxValue;
                m_expirationDays = Int64.MaxValue ;
                m_permanentData = true;
            }
            else 
            {
                String param;
                param = esd.Attribute (_strUserQuota) ;
                m_userQuota = param != null ? Int64.Parse(param, CultureInfo.InvariantCulture) : 0 ;
                param = esd.Attribute (_strMachineQuota) ;
                m_machineQuota = param != null ? Int64.Parse(param, CultureInfo.InvariantCulture) : 0 ;
                param = esd.Attribute (_strExpiry) ;
                m_expirationDays = param != null ? Int64.Parse(param, CultureInfo.InvariantCulture) : 0 ;
                param = esd.Attribute (_strPermDat) ;
                m_permanentData = param != null ? (Boolean.Parse(param)) : false ;
            }
        }
#endif // FEATURE_CAS_POLICY
    }
}
