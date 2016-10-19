// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Security.Authentication.ExtendedProtection
{
    /// <summary>
    /// This class contains the necessary settings for specifying how Extended Protection 
    /// should behave. Use one of the Build* methods to create an instance of this type.
    /// </summary>
    [Serializable]
    public class ExtendedProtectionPolicy : ISerializable
    {
        private const string policyEnforcementName = "policyEnforcement";
        private const string protectionScenarioName = "protectionScenario";
        private const string customServiceNamesName = "customServiceNames";
        private const string customChannelBindingName = "customChannelBinding";

        private ServiceNameCollection _customServiceNames;
        private PolicyEnforcement _policyEnforcement;
        private ProtectionScenario _protectionScenario;
        private ChannelBinding _customChannelBinding;

        public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement,
                                        ProtectionScenario protectionScenario,
                                        ServiceNameCollection customServiceNames)
        {
            if (policyEnforcement == PolicyEnforcement.Never)
            {
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, nameof(policyEnforcement));
            }

            if (customServiceNames != null && customServiceNames.Count == 0)
            {
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_NoEmptyServiceNameCollection, nameof(customServiceNames));
            }

            _policyEnforcement = policyEnforcement;
            _protectionScenario = protectionScenario;
            _customServiceNames = customServiceNames;
        }

        public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement,
                                        ProtectionScenario protectionScenario,
                                        ICollection customServiceNames)
            : this(policyEnforcement, protectionScenario,
                   customServiceNames == null ? (ServiceNameCollection)null : new ServiceNameCollection(customServiceNames))
        {
        }

        public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement,
                                        ChannelBinding customChannelBinding)
        {
            if (policyEnforcement == PolicyEnforcement.Never)
            {
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, nameof(policyEnforcement));
            }

            if (customChannelBinding == null)
            {
                throw new ArgumentNullException(nameof(customChannelBinding));
            }

            _policyEnforcement = policyEnforcement;
            _protectionScenario = ProtectionScenario.TransportSelected;
            _customChannelBinding = customChannelBinding;
        }

        public ExtendedProtectionPolicy(PolicyEnforcement policyEnforcement)
        {
            // This is the only constructor which allows PolicyEnforcement.Never.
            _policyEnforcement = policyEnforcement;
            _protectionScenario = ProtectionScenario.TransportSelected;
        }

        protected ExtendedProtectionPolicy(SerializationInfo info, StreamingContext context)
        {
            _policyEnforcement = (PolicyEnforcement)info.GetInt32(policyEnforcementName);
            _protectionScenario = (ProtectionScenario)info.GetInt32(protectionScenarioName);
            _customServiceNames = (ServiceNameCollection)info.GetValue(customServiceNamesName, typeof(ServiceNameCollection));

            byte[] channelBindingData = (byte[])info.GetValue(customChannelBindingName, typeof(byte[]));
            if (channelBindingData != null)
            {
                throw new PlatformNotSupportedException();
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_customChannelBinding != null)
            {
                throw new PlatformNotSupportedException();
            }

            info.AddValue(policyEnforcementName, (int)_policyEnforcement);
            info.AddValue(protectionScenarioName, (int)_protectionScenario);
            info.AddValue(customServiceNamesName, _customServiceNames, typeof(ServiceNameCollection));
            info.AddValue(customChannelBindingName, null, typeof(byte[]));
        }

        public ServiceNameCollection CustomServiceNames
        {
            get { return _customServiceNames; }
        }

        public PolicyEnforcement PolicyEnforcement
        {
            get { return _policyEnforcement; }
        }

        public ProtectionScenario ProtectionScenario
        {
            get { return _protectionScenario; }
        }

        public ChannelBinding CustomChannelBinding
        {
            get { return _customChannelBinding; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ProtectionScenario=");
            sb.Append(_protectionScenario.ToString());
            sb.Append("; PolicyEnforcement=");
            sb.Append(_policyEnforcement.ToString());

            sb.Append("; CustomChannelBinding=");
            if (_customChannelBinding == null)
            {
                sb.Append("<null>");
            }
            else
            {
                sb.Append(_customChannelBinding.ToString());
            }

            sb.Append("; ServiceNames=");
            if (_customServiceNames == null)
            {
                sb.Append("<null>");
            }
            else
            {
                bool first = true;
                foreach (string serviceName in _customServiceNames)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append(serviceName);
                }
            }

            return sb.ToString();
        }

        public static bool OSSupportsExtendedProtection
        {
            get
            {
                // CoreFX is supported only on Win7+ where ExtendedProtection is supported.
                return true;
            }
        }
    }
}
