// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

/*=============================================================================
**
**
** Purpose: Exception class for HostProtection
**
**
=============================================================================*/

namespace System.Security
{
    using System.Security;
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Text;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class HostProtectionException : SystemException
    {
        private HostProtectionResource m_protected;
        private HostProtectionResource m_demanded;

        private const String ProtectedResourcesName = "ProtectedResources";
        private const String DemandedResourcesName = "DemandedResources";

        public HostProtectionException() : base()
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        public HostProtectionException(string message) : base(message)
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        public HostProtectionException(string message, Exception e) : base(message, e)
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        protected HostProtectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info==null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            m_protected = (HostProtectionResource)info.GetValue(ProtectedResourcesName, typeof(HostProtectionResource));
            m_demanded = (HostProtectionResource)info.GetValue(DemandedResourcesName, typeof(HostProtectionResource));
        }

        public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources)
            : base(message)
        {
            SetErrorCode(__HResults.COR_E_HOSTPROTECTION);
            m_protected = protectedResources;
            m_demanded = demandedResources;
        }

        // Called from the VM to create a HP Exception
        private HostProtectionException(HostProtectionResource protectedResources, HostProtectionResource demandedResources)
            : base(SecurityException.GetResString("HostProtection_HostProtection"))
        {
            SetErrorCode(__HResults.COR_E_HOSTPROTECTION);
            m_protected = protectedResources;
            m_demanded = demandedResources;
        }


        public HostProtectionResource ProtectedResources
        {
            get
            {
                return m_protected;
            }
        }

        public HostProtectionResource DemandedResources
        {
            get
            {
                return m_demanded;
            }
        }

        private String ToStringHelper(String resourceString, Object attr)
        {
            if (attr == null)
                return String.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.GetResourceString( resourceString ));
            sb.Append(Environment.NewLine);
            sb.Append(attr);
            return sb.ToString();
        }

        public override String ToString() 
        {
            String protectedResStrValue = ToStringHelper("HostProtection_ProtectedResources", ProtectedResources);
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());

            sb.Append(protectedResStrValue);
            sb.Append(ToStringHelper("HostProtection_DemandedResources", DemandedResources));

            return sb.ToString();

        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            base.GetObjectData( info, context );

            info.AddValue(ProtectedResourcesName, ProtectedResources, typeof(HostProtectionResource));
            info.AddValue(DemandedResourcesName, DemandedResources, typeof(HostProtectionResource));
        }
    }
}
