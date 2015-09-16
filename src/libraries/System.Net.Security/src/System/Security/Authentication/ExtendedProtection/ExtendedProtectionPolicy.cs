// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text;

namespace System.Security.Authentication.ExtendedProtection
{
    /// <summary>
    /// This class contains the necessary settings for specifying how Extended Protection 
    /// should behave. Use one of the Build* methods to create an instance of this type.
    /// </summary>
    public class ExtendedProtectionPolicy
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
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, "policyEnforcement");
            }

            if (customServiceNames != null && customServiceNames.Count == 0)
            {
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_NoEmptyServiceNameCollection, "customServiceNames");
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
                throw new ArgumentException(SR.security_ExtendedProtectionPolicy_UseDifferentConstructorForNever, "policyEnforcement");
            }

            if (customChannelBinding == null)
            {
                throw new ArgumentNullException("customChannelBinding");
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
