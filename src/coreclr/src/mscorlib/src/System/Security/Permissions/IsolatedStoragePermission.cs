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
    }
}
