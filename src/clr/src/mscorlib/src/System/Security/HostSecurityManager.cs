// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
// A HostSecurityManager gives a hosting application the chance to 
// participate in the security decisions in the AppDomain.
//

namespace System.Security {
    using System.Collections;
#if FEATURE_CLICKONCE        
    using System.Deployment.Internal.Isolation;
    using System.Deployment.Internal.Isolation.Manifest;
    using System.Runtime.Hosting;    
#endif
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;


[Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum HostSecurityManagerOptions {
        None                            = 0x0000,
        HostAppDomainEvidence           = 0x0001,
        [Obsolete("AppDomain policy levels are obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        HostPolicyLevel                 = 0x0002,
        HostAssemblyEvidence            = 0x0004,
        HostDetermineApplicationTrust   = 0x0008,
        HostResolvePolicy               = 0x0010,
        AllFlags                        = 0x001F
    }

    [System.Security.SecurityCritical]  // auto-generated_required
    [Serializable]
#if !FEATURE_CORECLR
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.Infrastructure)]
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HostSecurityManager {
        public HostSecurityManager () {}

        // The host can choose which events he wants to participate in. This property can be set when
        // the host only cares about a subset of the capabilities exposed through the HostSecurityManager.
        public virtual HostSecurityManagerOptions Flags {
            get {
                // We use AllFlags as the default.
                return HostSecurityManagerOptions.AllFlags;
            }
        }

#if FEATURE_CAS_POLICY
        // provide policy for the AppDomain.
        [Obsolete("AppDomain policy levels are obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public virtual PolicyLevel DomainPolicy {
            get {
                if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
                }

                return null;
            }
        }
#endif
        public virtual Evidence ProvideAppDomainEvidence (Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence;
        }

        public virtual Evidence ProvideAssemblyEvidence (Assembly loadedAssembly, Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence;
        }

#if FEATURE_CLICKONCE
        [System.Security.SecurityCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Assert, Unrestricted=true)]
        public virtual ApplicationTrust DetermineApplicationTrust(Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context)
        {
            if (applicationEvidence == null)
                throw new ArgumentNullException("applicationEvidence");
            Contract.EndContractBlock();

            // This method looks for a trust decision for the ActivationContext in three locations, in order
            // of preference:
            //
            // 1. Supplied by the host in the AppDomainSetup. If the host supplied a decision this way, it
            //    will be in the applicationEvidence.
            // 2. Reuse the ApplicationTrust from the current AppDomain
            // 3. Ask the TrustManager for a trust decision

            // get the activation context from the application evidence.
            // The default HostSecurityManager does not examine the activatorEvidence
            // but other security managers could use it to figure out the 
            // evidence of the domain attempting to activate the application.

            ActivationArguments activationArgs = applicationEvidence.GetHostEvidence<ActivationArguments>();
            if (activationArgs == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence"));

            ActivationContext actCtx = activationArgs.ActivationContext;
            if (actCtx == null)
                throw new ArgumentException(Environment.GetResourceString("Policy_MissingActivationContextInAppEvidence"));

            // Make sure that any ApplicationTrust we find applies to the ActivationContext we're
            // creating the new AppDomain for.
            ApplicationTrust appTrust = applicationEvidence.GetHostEvidence<ApplicationTrust>();
            if (appTrust != null &&
                !CmsUtils.CompareIdentities(appTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion))
            {
                appTrust = null;
            }

            // If there was not a trust decision supplied in the Evidence, we can reuse the existing trust
            // decision from this domain if its identity matches the ActivationContext of the new domain.
            // Otherwise consult the TrustManager for a trust decision
            if (appTrust == null)
            {
                if (AppDomain.CurrentDomain.ApplicationTrust != null &&
                    CmsUtils.CompareIdentities(AppDomain.CurrentDomain.ApplicationTrust.ApplicationIdentity, activationArgs.ApplicationIdentity, ApplicationVersionMatch.MatchExactVersion))
                {
                    appTrust = AppDomain.CurrentDomain.ApplicationTrust;
                }
                else
                {
                    appTrust = ApplicationSecurityManager.DetermineApplicationTrustInternal(actCtx, context);
                }
            }

            // If the trust decision allows the application to run, then it should also have a permission set
            // which is at least the permission set the application requested.
            ApplicationSecurityInfo appRequest = new ApplicationSecurityInfo(actCtx);
            if (appTrust != null && 
                appTrust.IsApplicationTrustedToRun &&
                !appRequest.DefaultRequestSet.IsSubsetOf(appTrust.DefaultGrantSet.PermissionSet))
            {
                throw new InvalidOperationException(Environment.GetResourceString("Policy_AppTrustMustGrantAppRequest"));
            }

                return appTrust;
        }
#endif // FEATURE_CLICKONCE

#if FEATURE_CAS_POLICY        
        // Query the CLR to see what it would have granted a specific set of evidence
        public virtual PermissionSet ResolvePolicy(Evidence evidence)
        {
            if (evidence == null)
                throw new ArgumentNullException("evidence");
            Contract.EndContractBlock();

            //
            // If the evidence is from the GAC then the result is full trust.
            // In a homogenous domain, then the application trust object provides the grant set.
            // When CAS policy is disabled, the result is full trust.
            // Otherwise, the result comes from evaluating CAS policy.
            //

            if (evidence.GetHostEvidence<GacInstalled>() != null)
            {
                return new PermissionSet(PermissionState.Unrestricted);
            }
            else if (AppDomain.CurrentDomain.IsHomogenous)
            {
                return AppDomain.CurrentDomain.GetHomogenousGrantSet(evidence);
            }
            else if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                return new PermissionSet(PermissionState.Unrestricted);
            }
            else
            {
                return SecurityManager.PolicyManager.CodeGroupResolve(evidence, false);
            }
        }
#endif

        /// <summary>
        ///     Determine what types of evidence the host might be able to supply for the AppDomain if requested
        /// </summary>
        /// <returns></returns>
        public virtual Type[] GetHostSuppliedAppDomainEvidenceTypes() {
            return null;
        }

        /// <summary>
        ///     Determine what types of evidence the host might be able to supply for an assembly if requested
        /// </summary>
        public virtual Type[] GetHostSuppliedAssemblyEvidenceTypes(Assembly assembly) {
            return null;
        }

        /// <summary>
        ///     Ask the host to supply a specific type of evidence for the AppDomain
        /// </summary>
        public virtual EvidenceBase GenerateAppDomainEvidence(Type evidenceType) {
            return null;
        }

        /// <summary>
        ///     Ask the host to supply a specific type of evidence for an assembly
        /// </summary>
        public virtual EvidenceBase GenerateAssemblyEvidence(Type evidenceType, Assembly assembly) {
            return null;
        }
    }
}
