// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

//
// A HostSecurityManager gives a hosting application the chance to 
// participate in the security decisions in the AppDomain.
//

namespace System.Security
{
    using System.Collections;
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

    [Serializable]
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

        public virtual Evidence ProvideAppDomainEvidence (Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence;
        }

        public virtual Evidence ProvideAssemblyEvidence (Assembly loadedAssembly, Evidence inputEvidence) {
            // The default implementation does not modify the input evidence.
            return inputEvidence;
        }

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
