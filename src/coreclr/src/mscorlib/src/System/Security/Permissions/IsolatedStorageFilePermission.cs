// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Purpose : This permission is used to controls/administer access to 
//  IsolatedStorageFile
//

namespace System.Security.Permissions {

    using System.Globalization;
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    sealed public class IsolatedStorageFilePermission : IsolatedStoragePermission, IBuiltInPermission
    {
        public IsolatedStorageFilePermission(PermissionState state)
        : base(state) { }

        internal IsolatedStorageFilePermission(IsolatedStorageContainment UsageAllowed, 
            long ExpirationDays, bool PermanentData)
        : base(UsageAllowed, ExpirationDays, PermanentData) { }

        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            
            IsolatedStorageFilePermission operand = (IsolatedStorageFilePermission)target;
    
            if (this.IsUnrestricted() || operand.IsUnrestricted()) 
            {
                return new IsolatedStorageFilePermission( PermissionState.Unrestricted );
            }
            else
            {
                IsolatedStorageFilePermission union;
                union = new IsolatedStorageFilePermission( PermissionState.None );
                union.m_userQuota = max(m_userQuota,operand.m_userQuota);   
                union.m_machineQuota = max(m_machineQuota,operand.m_machineQuota);  
                union.m_expirationDays = max(m_expirationDays,operand.m_expirationDays);    
                union.m_permanentData = m_permanentData || operand.m_permanentData; 
                union.m_allowed = (IsolatedStorageContainment)max((long)m_allowed,(long)operand.m_allowed); 
                return union;
            }
        }   

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return ((m_userQuota == 0) &&
                        (m_machineQuota == 0) &&
                        (m_expirationDays == 0) &&
                        (m_permanentData == false) &&
                        (m_allowed == IsolatedStorageContainment.None));
            }

            try
            {
                IsolatedStorageFilePermission operand = (IsolatedStorageFilePermission)target;

                if (operand.IsUnrestricted())
                    return true;

                return ((operand.m_userQuota >= m_userQuota) &&
                        (operand.m_machineQuota >= m_machineQuota) &&
                        (operand.m_expirationDays >= m_expirationDays) &&
                        (operand.m_permanentData || !m_permanentData) &&
                        (operand.m_allowed >= m_allowed));
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

            IsolatedStorageFilePermission operand = (IsolatedStorageFilePermission)target;

            if(operand.IsUnrestricted()) 
                return Copy();
            else if(IsUnrestricted())
                return target.Copy();
            
            IsolatedStorageFilePermission intersection;
            intersection = new IsolatedStorageFilePermission( PermissionState.None );
            intersection.m_userQuota = min(m_userQuota,operand.m_userQuota);    
            intersection.m_machineQuota = min(m_machineQuota,operand.m_machineQuota);   
            intersection.m_expirationDays = min(m_expirationDays,operand.m_expirationDays); 
            intersection.m_permanentData = m_permanentData && operand.m_permanentData;  
            intersection.m_allowed = (IsolatedStorageContainment)min((long)m_allowed,(long)operand.m_allowed);  

            if ((intersection.m_userQuota == 0) &&
            (intersection.m_machineQuota == 0) &&
            (intersection.m_expirationDays == 0) &&
            (intersection.m_permanentData == false) &&
            (intersection.m_allowed == IsolatedStorageContainment.None))
                return null;

            return intersection;
        }
      
        public override IPermission Copy()
        {
            IsolatedStorageFilePermission copy ;
            copy = new IsolatedStorageFilePermission(PermissionState.Unrestricted);
            if(!IsUnrestricted()){
                copy.m_userQuota = m_userQuota; 
                copy.m_machineQuota = m_machineQuota;   
                copy.m_expirationDays = m_expirationDays;   
                copy.m_permanentData = m_permanentData; 
                copy.m_allowed = m_allowed; 
            }
            return copy;
        }


        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return IsolatedStorageFilePermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.IsolatedStorageFilePermissionIndex;
        }

        //------------------------------------------------------
        //
        // IsolatedStoragePermission OVERRIDES
        //
        //------------------------------------------------------
#if FEATURE_CAS_POLICY
        [System.Runtime.InteropServices.ComVisible(false)]
        public override SecurityElement ToXml()
        {
            return base.ToXml( "System.Security.Permissions.IsolatedStorageFilePermission" );
        }
#endif // FEATURE_CAS_POLICY
    }
}

