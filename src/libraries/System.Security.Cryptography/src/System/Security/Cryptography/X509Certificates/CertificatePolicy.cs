// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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

        private CertificatePolicyChain(int count)
        {
            _policies = new CertificatePolicy[count];
        }

        internal static CertificatePolicyChain Build(
            IEnumerable<X509Certificate2> chain,
            int chainLength,
            bool isPartialChain,
            ref ErrorVector extensionErrors)
        {
            CertificatePolicyChain policies = new CertificatePolicyChain(chainLength);
            bool corruptDeclaredPolicies = false;

            int rootDepth = isPartialChain ? -1 : chainLength - 1;
            int i = 0;

            foreach (X509Certificate2 cert in chain)
            {
                bool error;

                // Windows ignores declared policy corruption on the root cert.
                if (i == rootDepth)
                {
                    bool ignored = false;
                    policies._policies[i] = ReadPolicy(cert, out error, ref ignored);
                }
                else
                {
                    policies._policies[i] = ReadPolicy(cert, out error, ref corruptDeclaredPolicies);
                }

                if (error)
                {
                    extensionErrors.Set(i);
                }

                i++;
            }

            policies.ApplyRestrictions();
            policies._failAllCertificatePolicies |= corruptDeclaredPolicies;

            Debug.Assert(i == chainLength);
            return policies;
        }

        internal static ErrorVector CheckEncodingOnly(IEnumerable<X509Certificate2> chain, int chainLength)
        {
            ErrorVector vector = default;
            int i = 0;

            foreach (X509Certificate2 cert in chain)
            {
                PolicyData policyData = cert.Pal.GetPolicyData();

                try
                {
                    if (policyData.ApplicationCertPolicies != null)
                    {
                        CheckCertPolicyExtension(policyData.ApplicationCertPolicies);
                    }

                    if (policyData.CertPolicies != null)
                    {
                        CheckCertPolicyExtension(policyData.CertPolicies);
                    }

                    if (policyData.CertPolicyMappings != null)
                    {
                        CheckCertPolicyMappingsExtension(policyData.CertPolicyMappings);
                    }

                    if (policyData.CertPolicyConstraints != null)
                    {
                        _ = PolicyConstraintsAsn.Decode(policyData.CertPolicyConstraints, AsnEncodingRules.DER);
                    }

                    if (policyData.EnhancedKeyUsage != null)
                    {
                        if (policyData.ApplicationCertPolicies == null)
                        {
                            CheckExtendedKeyUsageExtension(policyData.EnhancedKeyUsage);
                        }
                    }

                    if (policyData.InhibitAnyPolicyExtension != null)
                    {
                        // Structural read returning a value type
                        _ = ReadInhibitAnyPolicyExtension(policyData.InhibitAnyPolicyExtension);
                    }
                }
                catch (AsnContentException)
                {
                    vector.Set(i);
                }
                catch (CryptographicException)
                {
                    vector.Set(i);
                }

                i++;
            }

            Debug.Assert(i == chainLength);
            return vector;
        }

        internal void MatchCertificatePolicies(OidCollection policyOids, ref ErrorVector usageErrors)
        {
            foreach (Oid oid in policyOids)
            {
                MatchCertificatePolicies(oid, ref usageErrors);
            }
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

        internal void MatchCertificatePolicies(Oid policyOid, ref ErrorVector usageErrors)
        {
            if (!MatchesCertificatePolicies(policyOid))
            {
                usageErrors.Set(0);
            }
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

        internal void MatchApplicationPolicies(OidCollection policyOids, ref ErrorVector usageErrors)
        {
            foreach (Oid oid in policyOids)
            {
                MatchApplicationPolicies(oid, ref usageErrors);
            }
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

        private void MatchApplicationPolicies(Oid policyOid, ref ErrorVector usageErrors)
        {
            string oidToCheck = policyOid.Value!;
            bool invalid = false;

            for (int i = 1; i <= _policies.Length; i++)
            {
                // The loop variable (i) matches the definition in RFC 3280,
                // section 6.1.3. In that description i=1 is the root CA, and n
                // is the EE/leaf certificate.  In our chain object 0 is the EE cert
                // and _policies.Length-1 is the root cert.  So we will index things as
                // _policies.Length - i (because i is 1 indexed).
                int dataIdx = _policies.Length - i;
                CertificatePolicy policy = _policies[dataIdx];

                // NotValidForUsage can be inherited from the parent.
                if (!invalid)
                {
                    if (!policy.AllowsAnyApplicationPolicy && policy.DeclaredApplicationPolicies is not null)
                    {
                        invalid = !policy.DeclaredApplicationPolicies.Contains(oidToCheck);
                    }
                }

                if (invalid)
                {
                    usageErrors.Set(dataIdx);
                }
            }
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

        private void ApplyRestrictions()
        {
            int explicitPolicyDepth = _policies.Length;
            int inhibitAnyPolicyDepth = explicitPolicyDepth;
            int inhibitPolicyMappingDepth = explicitPolicyDepth;

            for (int i = 1; i <= _policies.Length; i++)
            {
                // The loop variable (i) matches the definition in RFC 3280,
                // section 6.1.3. In that description i=1 is the root CA, and n
                // is the EE/leaf certificate.  In our chain object 0 is the EE cert
                // and _policies.Length-1 is for the root cert.  So we will index things as
                // _policies.Length - i (because i is 1 indexed).
                int dataIdx = _policies.Length - i;

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
                    inhibitPolicyMappingDepth--;
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

        private static CertificatePolicy ReadPolicy(X509Certificate2 cert, out bool error, ref bool corruptDeclaredPolicies)
        {
            // If no ApplicationCertPolicies extension is provided then it uses the EKU
            // OIDS.
            HashSet<string>? applicationCertPolicies = null;
            CertificatePolicy policy = new CertificatePolicy();
            error = false;

            PolicyData policyData = cert.Pal.GetPolicyData();

            if (policyData.ApplicationCertPolicies != null)
            {
                try
                {
                    applicationCertPolicies = ReadCertPolicyExtension(policyData.ApplicationCertPolicies);
                }
                catch (CryptographicException)
                {
                    error = true;
                }
            }

            if (policyData.CertPolicies != null)
            {
                try
                {
                    policy.DeclaredCertificatePolicies = ReadCertPolicyExtension(policyData.CertPolicies);
                }
                catch (CryptographicException)
                {
                    corruptDeclaredPolicies = true;
                    error = true;
                }
            }

            if (policyData.CertPolicyMappings != null)
            {
                try
                {
                    policy.PolicyMapping = ReadCertPolicyMappingsExtension(policyData.CertPolicyMappings);
                }
                catch (CryptographicException)
                {
                    error = true;
                }
            }

            if (policyData.CertPolicyConstraints != null)
            {
                try
                {
                    ReadCertPolicyConstraintsExtension(policyData.CertPolicyConstraints, policy);
                }
                catch (CryptographicException)
                {
                    error = true;
                }
            }

            if (policyData.EnhancedKeyUsage != null)
            {
                try
                {
                    // If policyData.ApplicationCertPolicies is present, but corrupt, applicationCertPolicies
                    // should stay null, don't even check EKU for structural validity.
                    if (policyData.ApplicationCertPolicies is null)
                    {
                        applicationCertPolicies = ReadExtendedKeyUsageExtension(policyData.EnhancedKeyUsage);
                    }
                }
                catch (CryptographicException)
                {
                    error = true;
                }
            }

            if (policyData.InhibitAnyPolicyExtension != null)
            {
                try
                {
                    policy.InhibitAnyDepth = ReadInhibitAnyPolicyExtension(policyData.InhibitAnyPolicyExtension);
                }
                catch (CryptographicException)
                {
                    error = true;
                }
            }

            policy.DeclaredApplicationPolicies = applicationCertPolicies;

            policy.ImplicitAnyApplicationPolicy = policy.DeclaredApplicationPolicies == null;
            policy.ImplicitAnyCertificatePolicy = policy.DeclaredCertificatePolicies == null;

            policy.SpecifiedAnyApplicationPolicy = CheckExplicitAnyPolicy(policy.DeclaredApplicationPolicies, Oids.AnyEnhancedKeyUsage);
            policy.SpecifiedAnyCertificatePolicy = CheckExplicitAnyPolicy(policy.DeclaredCertificatePolicies, Oids.AnyCertPolicy);

            return policy;
        }

        private static bool CheckExplicitAnyPolicy(ISet<string>? declaredPolicies, string anyPolicyOid)
        {
            if (declaredPolicies == null)
            {
                return false;
            }

            return declaredPolicies.Remove(anyPolicyOid);
        }

        private static int ReadInhibitAnyPolicyExtension(byte[] rawData)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
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

        private static void CheckExtendedKeyUsageExtension(byte[] rawData)
        {
            ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
            ValueAsnReader sequenceReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            //OidCollection usages
            while (sequenceReader.HasData)
            {
                // OBJECT IDENTIFIER only has a primitive encoding, so != is fine,
                // doesn't need to be HasSameClassAndValue
                if (sequenceReader.PeekTag() != Asn1Tag.ObjectIdentifier)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // This won't detect an invalidly encoded OID, but Windows doesn't check
                // that either, at this stage.
                sequenceReader.ReadEncodedValue();
            }
        }

        private static HashSet<string> ReadExtendedKeyUsageExtension(byte[] rawData)
        {
            HashSet<string> oids = new HashSet<string>();

            try
            {
                ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
                ValueAsnReader sequenceReader = reader.ReadSequence();
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

        private static void CheckCertPolicyExtension(byte[] rawData)
        {
            ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
            ValueAsnReader sequenceReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            while (sequenceReader.HasData)
            {
                PolicyInformationAsn.Decode(ref sequenceReader, rawData, out _);
            }
        }

        internal static HashSet<string> ReadCertPolicyExtension(byte[] rawData)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
                ValueAsnReader sequenceReader = reader.ReadSequence();
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

        private static void CheckCertPolicyMappingsExtension(byte[] rawData)
        {
            ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
            ValueAsnReader sequenceReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            while (sequenceReader.HasData)
            {
                CertificatePolicyMappingAsn.Decode(ref sequenceReader, out _);
            }
        }

        private static List<CertificatePolicyMappingAsn> ReadCertPolicyMappingsExtension(byte[] rawData)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(rawData, AsnEncodingRules.DER);
                ValueAsnReader sequenceReader = reader.ReadSequence();
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

        internal struct ErrorVector
        {
            private UInt128 _data;

            internal bool Any => _data != 0;

            internal bool this[int index]
            {
                get
                {
                    Debug.Assert(index < 128);
                    Debug.Assert(index >= 0);

                    index = int.Min(index, 127);

                    UInt128 test = 1;
                    test <<= index;

                    return (_data & test) != 0;
                }
            }

            internal void Set(int index)
            {
                // In Debug, complain if we see 128 or higher.
                //
                // In Release, we'll clamp to 128 items, so any platform
                // with a ridiculously long chain will report any errors
                // at 128 or above on all of them.
                Debug.Assert(index < 128);
                Debug.Assert(index >= 0);

                index = int.Min(index, 127);

                UInt128 test = 1;
                test <<= index;
                _data |= test;
            }
        }
    }
}
