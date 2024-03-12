// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class CertificatePolicy
    {
        public bool ImplicitAnyCertificatePolicy { get; set; }
        public bool SpecifiedAnyCertificatePolicy { get; set; }
        public ISet<string>? DeclaredCertificatePolicies { get; set; }
        public bool ImplicitAnyApplicationPolicy { get; set; }
        public bool SpecifiedAnyApplicationPolicy { get; set; }
        public ISet<string>? DeclaredApplicationPolicies { get; set; }
        public int? InhibitAnyDepth { get; set; }
        public List<CertificatePolicyMappingAsn>? PolicyMapping { get; set; }
        public int? InhibitMappingDepth { get; set; }
        public int? RequireExplicitPolicyDepth { get; set; }

        public bool AllowsAnyCertificatePolicy
        {
            get { return ImplicitAnyCertificatePolicy || SpecifiedAnyCertificatePolicy; }
        }

        public bool AllowsAnyApplicationPolicy
        {
            get { return ImplicitAnyApplicationPolicy || SpecifiedAnyApplicationPolicy; }
        }
    }

    internal sealed class CertificatePolicyChain
    {
        private readonly CertificatePolicy[] _policies;
        private bool _failAllCertificatePolicies;

        public CertificatePolicyChain(List<X509Certificate2> chain)
        {
            _policies = new CertificatePolicy[chain.Count];

            ReadPolicies(chain);
        }

        internal bool MatchesCertificatePolicies(OidCollection policyOids)
        {
            foreach (Oid oid in policyOids)
            {
                if (!MatchesCertificatePolicies(oid))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool MatchesCertificatePolicies(Oid policyOid)
        {
            if (_failAllCertificatePolicies)
            {
                return false;
            }

            string nextOid = policyOid.Value!;

            for (int i = 1; i <= _policies.Length; i++)
            {
                // The loop variable (i) matches the definition in RFC 3280,
                // section 6.1.3. In that description i=1 is the root CA, and n
                // is the EE/leaf certificate.  In our chain object 0 is the EE cert
                // and _policies.Length-1 is the root cert.  So we will index things as
                // _policies.Length - i (because i is 1 indexed).
                int dataIdx = _policies.Length - i;
                CertificatePolicy policy = _policies[dataIdx];
                string oidToCheck = nextOid;

                if (policy.PolicyMapping != null)
                {
                    for (int iMapping = 0; iMapping < policy.PolicyMapping.Count; iMapping++)
                    {
                        CertificatePolicyMappingAsn mapping = policy.PolicyMapping[iMapping];
                        if (StringComparer.Ordinal.Equals(mapping.IssuerDomainPolicy, oidToCheck))
                        {
                            nextOid = mapping.SubjectDomainPolicy;
                        }
                    }
                }

                if (policy.AllowsAnyCertificatePolicy)
                {
                    continue;
                }

                if (policy.DeclaredCertificatePolicies == null)
                {
                    return false;
                }

                if (!policy.DeclaredCertificatePolicies.Contains(oidToCheck))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool MatchesApplicationPolicies(OidCollection policyOids)
        {
            foreach (Oid oid in policyOids)
            {
                if (!MatchesApplicationPolicies(oid))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool MatchesApplicationPolicies(Oid policyOid)
        {
            string oidToCheck = policyOid.Value!;

            for (int i = 1; i <= _policies.Length; i++)
            {
                // The loop variable (i) matches the definition in RFC 3280,
                // section 6.1.3. In that description i=1 is the root CA, and n
                // is the EE/leaf certificate.  In our chain object 0 is the EE cert
                // and _policies.Length-1 is the root cert.  So we will index things as
                // _policies.Length - i (because i is 1 indexed).
                int dataIdx = _policies.Length - i;
                CertificatePolicy policy = _policies[dataIdx];

                if (policy.AllowsAnyApplicationPolicy)
                {
                    continue;
                }

                if (policy.DeclaredApplicationPolicies == null)
                {
                    return false;
                }

                if (!policy.DeclaredApplicationPolicies.Contains(oidToCheck))
                {
                    return false;
                }
            }

            return true;
        }

        private void ReadPolicies(List<X509Certificate2> chain)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                _policies[i] = ReadPolicy(chain[i]);
            }

            int explicitPolicyDepth = chain.Count;
            int inhibitAnyPolicyDepth = explicitPolicyDepth;
            int inhibitPolicyMappingDepth = explicitPolicyDepth;

            for (int i = 1; i <= chain.Count; i++)
            {
                // The loop variable (i) matches the definition in RFC 3280,
                // section 6.1.3. In that description i=1 is the root CA, and n
                // is the EE/leaf certificate.  In our chain object 0 is the EE cert
                // and chain.Count-1 is the root cert.  So we will index things as
                // chain.Count - i (because i is 1 indexed).
                int dataIdx = chain.Count - i;

                CertificatePolicy policy = _policies[dataIdx];

                if (policy.DeclaredCertificatePolicies == null && explicitPolicyDepth <= 0)
                {
                    _failAllCertificatePolicies = true;
                }

                if (inhibitAnyPolicyDepth <= 0)
                {
                    policy.ImplicitAnyCertificatePolicy = false;
                    policy.SpecifiedAnyCertificatePolicy = false;
                }
                else
                {
                    inhibitAnyPolicyDepth--;
                }

                if (inhibitPolicyMappingDepth <= 0)
                {
                    policy.PolicyMapping = null;
                }
                else
                {
                    inhibitAnyPolicyDepth--;
                }

                if (explicitPolicyDepth <= 0)
                {
                    policy.ImplicitAnyCertificatePolicy = false;
                    policy.ImplicitAnyApplicationPolicy = false;
                }
                else
                {
                    explicitPolicyDepth--;
                }

                ApplyRestriction(ref inhibitAnyPolicyDepth, policy.InhibitAnyDepth);
                ApplyRestriction(ref inhibitPolicyMappingDepth, policy.InhibitMappingDepth);
                ApplyRestriction(ref explicitPolicyDepth, policy.RequireExplicitPolicyDepth);
            }
        }

        private static void ApplyRestriction(ref int restriction, int? policyRestriction)
        {
            if (policyRestriction.HasValue)
            {
                restriction = Math.Min(restriction, policyRestriction.Value);
            }
        }

        private static CertificatePolicy ReadPolicy(X509Certificate2 cert)
        {
            // If no ApplicationCertPolicies extension is provided then it uses the EKU
            // OIDS.
            HashSet<string>? applicationCertPolicies = null;
            HashSet<string>? ekus = null;
            CertificatePolicy policy = new CertificatePolicy();

            PolicyData policyData = cert.Pal.GetPolicyData();

            if (policyData.ApplicationCertPolicies != null)
            {
                applicationCertPolicies = ReadCertPolicyExtension(policyData.ApplicationCertPolicies);
            }

            if (policyData.CertPolicies != null)
            {
                policy.DeclaredCertificatePolicies = ReadCertPolicyExtension(policyData.CertPolicies);
            }

            if (policyData.CertPolicyMappings != null)
            {
                policy.PolicyMapping = ReadCertPolicyMappingsExtension(policyData.CertPolicyMappings);
            }

            if (policyData.CertPolicyConstraints != null)
            {
                ReadCertPolicyConstraintsExtension(policyData.CertPolicyConstraints, policy);
            }

            if (policyData.EnhancedKeyUsage != null && applicationCertPolicies == null)
            {
                // No reason to do this if the applicationCertPolicies was already read
                ekus = ReadExtendedKeyUsageExtension(policyData.EnhancedKeyUsage);
            }

            if (policyData.InhibitAnyPolicyExtension != null)
            {
                policy.InhibitAnyDepth = ReadInhibitAnyPolicyExtension(policyData.InhibitAnyPolicyExtension);
            }

            policy.DeclaredApplicationPolicies = applicationCertPolicies ?? ekus;

            policy.ImplicitAnyApplicationPolicy = policy.DeclaredApplicationPolicies == null;
            policy.ImplicitAnyCertificatePolicy = policy.DeclaredCertificatePolicies == null;

            policy.SpecifiedAnyApplicationPolicy = CheckExplicitAnyPolicy(policy.DeclaredApplicationPolicies);
            policy.SpecifiedAnyCertificatePolicy = CheckExplicitAnyPolicy(policy.DeclaredCertificatePolicies);

            return policy;
        }

        private static bool CheckExplicitAnyPolicy(ISet<string>? declaredPolicies)
        {
            if (declaredPolicies == null)
            {
                return false;
            }

            return declaredPolicies.Remove(Oids.AnyCertPolicy);
        }

        private static int ReadInhibitAnyPolicyExtension(byte[] rawData)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                int inhibitAnyPolicy;
                reader.TryReadInt32(out inhibitAnyPolicy);
                reader.ThrowIfNotEmpty();
                return inhibitAnyPolicy;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void ReadCertPolicyConstraintsExtension(byte[] rawData, CertificatePolicy policy)
        {
            PolicyConstraintsAsn constraints = PolicyConstraintsAsn.Decode(
                rawData,
                AsnEncodingRules.DER);

            policy.RequireExplicitPolicyDepth = constraints.RequireExplicitPolicyDepth;
            policy.InhibitMappingDepth = constraints.InhibitMappingDepth;
        }

        private static HashSet<string> ReadExtendedKeyUsageExtension(byte[] rawData)
        {
            HashSet<string> oids = new HashSet<string>();

            try
            {
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                //OidCollection usages
                while (sequenceReader.HasData)
                {
                    oids.Add(sequenceReader.ReadObjectIdentifier());
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            return oids;
        }

        internal static HashSet<string> ReadCertPolicyExtension(byte[] rawData)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                HashSet<string> policies = new HashSet<string>();
                while (sequenceReader.HasData)
                {
                    PolicyInformationAsn.Decode(ref sequenceReader, rawData, out PolicyInformationAsn policyInformation);
                    policies.Add(policyInformation.PolicyIdentifier);

                    // There is an optional policy qualifier here, but it is for information
                    // purposes, there is no logic that would be changed.

                    // Since reader (the outer one) has already skipped past the rest of the
                    // sequence we don't particularly need to drain out here.
                }

                return policies;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static List<CertificatePolicyMappingAsn> ReadCertPolicyMappingsExtension(byte[] rawData)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                List<CertificatePolicyMappingAsn> mappings = new List<CertificatePolicyMappingAsn>();
                while (sequenceReader.HasData)
                {
                    CertificatePolicyMappingAsn.Decode(ref sequenceReader, out CertificatePolicyMappingAsn mapping);
                    mappings.Add(mapping);
                }

                return mappings;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
