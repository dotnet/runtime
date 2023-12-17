// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class SignedXml
    {
        protected Signature m_signature;
        protected string? m_strSigningKeyName;

        private AsymmetricAlgorithm? _signingKey;
        private XmlDocument? _containingDocument;
        private IEnumerator? _keyInfoEnum;
        private X509Certificate2Collection? _x509Collection;
        private X509Certificate2Enumerator? _x509Enum;

        private bool[]? _refProcessed;
        private int[]? _refLevelCache;

        internal XmlResolver? _xmlResolver;
        internal XmlElement? _context;
        private bool _bResolverSet;

        private Func<SignedXml, bool> _signatureFormatValidator = DefaultSignatureFormatValidator;
        private Collection<string> _safeCanonicalizationMethods;

        // Built in canonicalization algorithm URIs
        private static IList<string>? s_knownCanonicalizationMethods;
        // Built in transform algorithm URIs (excluding canonicalization URIs)
        private static IList<string>? s_defaultSafeTransformMethods;

        // additional HMAC Url identifiers
        private const string XmlDsigMoreHMACMD5Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-md5";
        private const string XmlDsigMoreHMACSHA256Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256";
        private const string XmlDsigMoreHMACSHA384Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha384";
        private const string XmlDsigMoreHMACSHA512Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";
        private const string XmlDsigMoreHMACRIPEMD160Url = "http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160";

        // defines the XML encryption processing rules
        private EncryptedXml? _exml;

        //
        // public constant Url identifiers most frequently used within the XML Signature classes
        //

        public const string XmlDsigNamespaceUrl = "http://www.w3.org/2000/09/xmldsig#";
        public const string XmlDsigMinimalCanonicalizationUrl = "http://www.w3.org/2000/09/xmldsig#minimal";
        public const string XmlDsigCanonicalizationUrl = XmlDsigC14NTransformUrl;
        public const string XmlDsigCanonicalizationWithCommentsUrl = XmlDsigC14NWithCommentsTransformUrl;

        public const string XmlDsigSHA1Url = "http://www.w3.org/2000/09/xmldsig#sha1";
        public const string XmlDsigDSAUrl = "http://www.w3.org/2000/09/xmldsig#dsa-sha1";
        public const string XmlDsigRSASHA1Url = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        public const string XmlDsigHMACSHA1Url = "http://www.w3.org/2000/09/xmldsig#hmac-sha1";

        public const string XmlDsigSHA256Url = "http://www.w3.org/2001/04/xmlenc#sha256";
        public const string XmlDsigRSASHA256Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

        // Yes, SHA384 is in the xmldsig-more namespace even though all the other SHA variants are in xmlenc. That's the standard.
        public const string XmlDsigSHA384Url = "http://www.w3.org/2001/04/xmldsig-more#sha384";
        public const string XmlDsigRSASHA384Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";

        public const string XmlDsigSHA512Url = "http://www.w3.org/2001/04/xmlenc#sha512";
        public const string XmlDsigRSASHA512Url = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";

        public const string XmlDsigC14NTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        public const string XmlDsigC14NWithCommentsTransformUrl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments";
        public const string XmlDsigExcC14NTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#";
        public const string XmlDsigExcC14NWithCommentsTransformUrl = "http://www.w3.org/2001/10/xml-exc-c14n#WithComments";
        public const string XmlDsigBase64TransformUrl = "http://www.w3.org/2000/09/xmldsig#base64";
        public const string XmlDsigXPathTransformUrl = "http://www.w3.org/TR/1999/REC-xpath-19991116";
        public const string XmlDsigXsltTransformUrl = "http://www.w3.org/TR/1999/REC-xslt-19991116";
        public const string XmlDsigEnvelopedSignatureTransformUrl = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
        public const string XmlDecryptionTransformUrl = "http://www.w3.org/2002/07/decrypt#XML";
        public const string XmlLicenseTransformUrl = "urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform";

        //
        // public constructors
        //

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public SignedXml()
        {
            Initialize(null);
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public SignedXml(XmlDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Initialize(document.DocumentElement);
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public SignedXml(XmlElement elem)
        {
            if (elem is null)
            {
                throw new ArgumentNullException(nameof(elem));
            }

            Initialize(elem);
        }

        [MemberNotNull(nameof(m_signature))]
        [MemberNotNull(nameof(_safeCanonicalizationMethods))]
        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        private void Initialize(XmlElement? element)
        {
            _containingDocument = element?.OwnerDocument;
            _context = element;
            m_signature = new Signature();
            m_signature.SignedXml = this;
            m_signature.SignedInfo = new SignedInfo();
            _signingKey = null;

            _safeCanonicalizationMethods = new Collection<string>(KnownCanonicalizationMethods);
        }

        //
        // public properties
        //

        /// <internalonly/>
        public string? SigningKeyName
        {
            get { return m_strSigningKeyName; }
            set { m_strSigningKeyName = value; }
        }

        public XmlResolver Resolver
        {
            // This property only has a setter. The rationale for this is that we don't have a good value
            // to return when it has not been explicitely set, as we are using XmlSecureResolver by default
            set
            {
                _xmlResolver = value;
                _bResolverSet = true;
            }
        }

        internal bool ResolverSet
        {
            get { return _bResolverSet; }
        }

        public Func<SignedXml, bool> SignatureFormatValidator
        {
            get { return _signatureFormatValidator; }
            set { _signatureFormatValidator = value; }
        }

        public Collection<string> SafeCanonicalizationMethods
        {
            get { return _safeCanonicalizationMethods; }
        }

        public AsymmetricAlgorithm? SigningKey
        {
            get { return _signingKey; }
            set { _signingKey = value; }
        }

        [AllowNull]
        public EncryptedXml EncryptedXml
        {
            [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "ctors are marked as RDC")]
            [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RUC")]
            get => _exml ??= new EncryptedXml(_containingDocument!); // default processing rules
            set => _exml = value;
        }

        public Signature Signature
        {
            get { return m_signature; }
        }

        public SignedInfo? SignedInfo
        {
            get { return m_signature.SignedInfo; }
        }

        public string? SignatureMethod
        {
            get { return m_signature.SignedInfo!.SignatureMethod; }
        }

        public string? SignatureLength
        {
            get { return m_signature.SignedInfo!.SignatureLength; }
        }

        public byte[]? SignatureValue
        {
            get { return m_signature.SignatureValue; }
        }

        public KeyInfo KeyInfo
        {
            get { return m_signature.KeyInfo; }
            set { m_signature.KeyInfo = value; }
        }

        public XmlElement GetXml()
        {
            // If we have a document context, then return a signature element in this context
            if (_containingDocument != null)
                return m_signature.GetXml(_containingDocument);
            else
                return m_signature.GetXml();
        }

        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "ctors are marked as RDC")]
        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RUC")]
        public void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            m_signature.LoadXml(value);

            _context ??= value;

            _bCacheValid = false;
        }

        //
        // public methods
        //

        public void AddReference(Reference reference)
        {
            m_signature.SignedInfo!.AddReference(reference);
        }

        public void AddObject(DataObject dataObject)
        {
            m_signature.AddObject(dataObject);
        }

        public bool CheckSignature()
        {
            return CheckSignatureReturningKey(out _);
        }

        public bool CheckSignatureReturningKey(out AsymmetricAlgorithm? signingKey)
        {
            SignedXmlDebugLog.LogBeginSignatureVerification(this, _context);

            signingKey = null;
            bool bRet = false;
            AsymmetricAlgorithm? key;

            if (!CheckSignatureFormat())
            {
                return false;
            }

            do
            {
                key = GetPublicKey();
                if (key != null)
                {
                    bRet = CheckSignature(key);
                    SignedXmlDebugLog.LogVerificationResult(this, key, bRet);
                }
            } while (key != null && bRet == false);

            signingKey = key;
            return bRet;
        }

        public bool CheckSignature(AsymmetricAlgorithm key)
        {
            if (!CheckSignatureFormat())
            {
                return false;
            }

            if (!CheckSignedInfo(key))
            {
                SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_SignedInfo);
                return false;
            }

            // Now is the time to go through all the references and see if their DigestValues are good
            if (!CheckDigestedReferences())
            {
                SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_References);
                return false;
            }

            SignedXmlDebugLog.LogVerificationResult(this, key, true);
            return true;
        }

        public bool CheckSignature(KeyedHashAlgorithm macAlg)
        {
            if (!CheckSignatureFormat())
            {
                return false;
            }

            if (!CheckSignedInfo(macAlg))
            {
                SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_SignedInfo);
                return false;
            }

            if (!CheckDigestedReferences())
            {
                SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_References);
                return false;
            }

            SignedXmlDebugLog.LogVerificationResult(this, macAlg, true);
            return true;
        }

        public bool CheckSignature(X509Certificate2 certificate, bool verifySignatureOnly)
        {
            if (!verifySignatureOnly)
            {
                // Check key usages to make sure it is good for signing.
                foreach (X509Extension extension in certificate.Extensions)
                {
                    if (string.Equals(extension.Oid!.Value, "2.5.29.15" /* szOID_KEY_USAGE */, StringComparison.OrdinalIgnoreCase))
                    {
                        X509KeyUsageExtension keyUsage = new X509KeyUsageExtension();
                        keyUsage.CopyFrom(extension);
                        SignedXmlDebugLog.LogVerifyKeyUsage(this, certificate, keyUsage);

                        bool validKeyUsage = (keyUsage.KeyUsages & X509KeyUsageFlags.DigitalSignature) != 0 ||
                                             (keyUsage.KeyUsages & X509KeyUsageFlags.NonRepudiation) != 0;

                        if (!validKeyUsage)
                        {
                            SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_X509KeyUsage);
                            return false;
                        }
                        break;
                    }
                }

                // Do the chain verification to make sure the certificate is valid.
                X509Chain chain = new X509Chain();
                chain.ChainPolicy.ExtraStore.AddRange(BuildBagOfCerts());
                bool chainVerified = chain.Build(certificate);
                SignedXmlDebugLog.LogVerifyX509Chain(this, chain, certificate);

                if (!chainVerified)
                {
                    SignedXmlDebugLog.LogVerificationFailure(this, SR.Log_VerificationFailed_X509Chain);
                    return false;
                }
            }

            using (AsymmetricAlgorithm? publicKey = Utils.GetAnyPublicKey(certificate))
            {
                if (!CheckSignature(publicKey!))
                {
                    return false;
                }
            }

            SignedXmlDebugLog.LogVerificationResult(this, certificate, true);
            return true;
        }

        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        public void ComputeSignature()
        {
            SignedXmlDebugLog.LogBeginSignatureComputation(this, _context!);

            BuildDigestedReferences();

            // Load the key
            AsymmetricAlgorithm? key = SigningKey;

            if (key == null)
                throw new CryptographicException(SR.Cryptography_Xml_LoadKeyFailed);

            // Check the signature algorithm associated with the key so that we can accordingly set the signature method
            if (SignedInfo!.SignatureMethod == null)
            {
                if (key is DSA)
                {
                    SignedInfo.SignatureMethod = XmlDsigDSAUrl;
                }
                else if (key is RSA)
                {
                    // Default to RSA-SHA256
                    SignedInfo.SignatureMethod ??= XmlDsigRSASHA256Url;
                }
                else
                {
                    throw new CryptographicException(SR.Cryptography_Xml_CreatedKeyFailed);
                }
            }

            // See if there is a signature description class defined in the Config file
            SignatureDescription? signatureDescription = CryptoHelpers.CreateNonTransformFromName<SignatureDescription>(SignedInfo.SignatureMethod);
            if (signatureDescription == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignatureDescriptionNotCreated);
            HashAlgorithm? hashAlg = signatureDescription.CreateDigest();
            if (hashAlg == null)
                throw new CryptographicException(SR.Cryptography_Xml_CreateHashAlgorithmFailed);

            // Updates the HashAlgorithm's state for signing with the signature formatter below.
            // The return value is not needed.
            GetC14NDigest(hashAlg);

            AsymmetricSignatureFormatter asymmetricSignatureFormatter = signatureDescription.CreateFormatter(key);

            SignedXmlDebugLog.LogSigning(this, key, signatureDescription, hashAlg, asymmetricSignatureFormatter);
            m_signature.SignatureValue = asymmetricSignatureFormatter.CreateSignature(hashAlg);
        }

        public void ComputeSignature(KeyedHashAlgorithm macAlg)
        {
            if (macAlg is null)
            {
                throw new ArgumentNullException(nameof(macAlg));
            }

            HMAC? hash = macAlg as HMAC;
            if (hash == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignatureMethodKeyMismatch);

            int signatureLength;
            if (m_signature.SignedInfo!.SignatureLength == null)
                signatureLength = hash.HashSize;
            else
                signatureLength = Convert.ToInt32(m_signature.SignedInfo.SignatureLength, null);
            // signatureLength should be less than hash size
            if (signatureLength < 0 || signatureLength > hash.HashSize)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidSignatureLength);
            if (signatureLength % 8 != 0)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidSignatureLength2);

            BuildDigestedReferences();
            SignedInfo!.SignatureMethod = hash.HashName switch
            {
                "SHA1" => SignedXml.XmlDsigHMACSHA1Url,
                "SHA256" => SignedXml.XmlDsigMoreHMACSHA256Url,
                "SHA384" => SignedXml.XmlDsigMoreHMACSHA384Url,
                "SHA512" => SignedXml.XmlDsigMoreHMACSHA512Url,
                "MD5" => SignedXml.XmlDsigMoreHMACMD5Url,
                "RIPEMD160" => SignedXml.XmlDsigMoreHMACRIPEMD160Url,
                _ => throw new CryptographicException(SR.Cryptography_Xml_SignatureMethodKeyMismatch),
            };
            byte[] hashValue = GetC14NDigest(hash);

            SignedXmlDebugLog.LogSigning(this, hash);
            m_signature.SignatureValue = new byte[signatureLength / 8];
            Buffer.BlockCopy(hashValue, 0, m_signature.SignatureValue, 0, signatureLength / 8);
        }

        //
        // virtual methods
        //

        protected virtual AsymmetricAlgorithm? GetPublicKey()
        {
            if (KeyInfo == null)
                throw new CryptographicException(SR.Cryptography_Xml_KeyInfoRequired);

            if (_x509Enum != null)
            {
                AsymmetricAlgorithm? key = GetNextCertificatePublicKey();
                if (key != null)
                    return key;
            }

            _keyInfoEnum ??= KeyInfo.GetEnumerator();

            // In our implementation, we move to the next KeyInfo clause which is an RSAKeyValue, DSAKeyValue or KeyInfoX509Data
            while (_keyInfoEnum.MoveNext())
            {
                switch (_keyInfoEnum.Current)
                {
                    case RSAKeyValue rsaKeyValue:
                        return rsaKeyValue.Key;

                    case DSAKeyValue dsaKeyValue:
                        return dsaKeyValue.Key;

                    case KeyInfoX509Data x509Data:
                        _x509Collection = Utils.BuildBagOfCerts(x509Data, CertUsageType.Verification);
                        if (_x509Collection.Count > 0)
                        {
                            _x509Enum = _x509Collection.GetEnumerator();
                            AsymmetricAlgorithm? key = GetNextCertificatePublicKey();
                            if (key != null)
                                return key;
                        }
                        break;
                }
            }

            return null;
        }

        private X509Certificate2Collection BuildBagOfCerts()
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            if (KeyInfo != null)
            {
                foreach (KeyInfoClause clause in KeyInfo)
                {
                    if (clause is KeyInfoX509Data x509Data)
                    {
                        collection.AddRange(Utils.BuildBagOfCerts(x509Data, CertUsageType.Verification));
                    }
                }
            }

            return collection;
        }

        private AsymmetricAlgorithm? GetNextCertificatePublicKey()
        {
            while (_x509Enum!.MoveNext())
            {
                X509Certificate2? certificate = (X509Certificate2?)_x509Enum.Current;
                if (certificate != null)
                    return Utils.GetAnyPublicKey(certificate);
            }

            return null;
        }

        public virtual XmlElement? GetIdElement(XmlDocument? document, string idValue)
        {
            return DefaultGetIdElement(document, idValue);
        }

        internal static XmlElement? DefaultGetIdElement(XmlDocument? document, string idValue)
        {
            if (document == null)
                return null;

            try
            {
                XmlConvert.VerifyNCName(idValue);
            }
            catch (XmlException)
            {
                // Identifiers are required to be an NCName
                //   (xml:id version 1.0, part 4, paragraph 2, bullet 1)
                //
                // If it isn't an NCName, it isn't allowed to match.
                return null;
            }

            // Get the element with idValue
            XmlElement? elem = document.GetElementById(idValue);

            if (elem != null)
            {
                // Have to check for duplicate ID values from the DTD.

                XmlDocument docClone = (XmlDocument)document.CloneNode(true);
                XmlElement? cloneElem = docClone.GetElementById(idValue);

                // If it's null here we want to know about it, because it means that
                // GetElementById failed to work across the cloning, and our uniqueness
                // test is invalid.
                System.Diagnostics.Debug.Assert(cloneElem != null);

                // Guard against null anyways
                if (cloneElem != null)
                {
                    cloneElem.Attributes.RemoveAll();

                    XmlElement? cloneElem2 = docClone.GetElementById(idValue);

                    if (cloneElem2 != null)
                    {
                        throw new CryptographicException(
                            SR.Cryptography_Xml_InvalidReference);
                    }
                }

                return elem;
            }

            elem = GetSingleReferenceTarget(document, "Id", idValue);
            if (elem != null)
                return elem;
            elem = GetSingleReferenceTarget(document, "id", idValue);
            if (elem != null)
                return elem;
            elem = GetSingleReferenceTarget(document, "ID", idValue);

            return elem;
        }

        //
        // private methods
        //

        private bool _bCacheValid;
        private byte[]? _digestedSignedInfo;

        private static bool DefaultSignatureFormatValidator(SignedXml signedXml)
        {
            // Reject the signature if it uses a truncated HMAC
            if (signedXml.DoesSignatureUseTruncatedHmac())
            {
                return false;
            }

            // Reject the signature if it uses a canonicalization algorithm other than
            // one of the ones explicitly allowed
            if (!signedXml.DoesSignatureUseSafeCanonicalizationMethod())
            {
                return false;
            }

            // Otherwise accept it
            return true;
        }

        // Validation function to see if the current signature is signed with a truncated HMAC - one which
        // has a signature length of fewer bits than the whole HMAC output.
        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        private bool DoesSignatureUseTruncatedHmac()
        {
            // If we're not using the SignatureLength property, then we're not truncating the signature length
            if (SignedInfo!.SignatureLength == null)
            {
                return false;
            }

            // See if we're signed witn an HMAC algorithm
            HMAC? hmac = CryptoHelpers.CreateNonTransformFromName<HMAC>(SignatureMethod!);
            if (hmac == null)
            {
                // We aren't signed with an HMAC algorithm, so we cannot have a truncated HMAC
                return false;
            }

            // Figure out how many bits the signature is using
            int actualSignatureSize;
            if (!int.TryParse(SignedInfo.SignatureLength, out actualSignatureSize))
            {
                // If the value wasn't a valid integer, then we'll conservatively reject it all together
                return true;
            }

            // Make sure the full HMAC signature size is the same size that was specified in the XML
            // signature.  If the actual signature size is not exactly the same as the full HMAC size, then
            // reject the signature.
            return actualSignatureSize != hmac.HashSize;
        }

        // Validation function to see if the signature uses a canonicalization algorithm from our list
        // of approved algorithm URIs.
        private bool DoesSignatureUseSafeCanonicalizationMethod()
        {
            foreach (string safeAlgorithm in SafeCanonicalizationMethods)
            {
                if (string.Equals(safeAlgorithm, SignedInfo!.CanonicalizationMethod, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            SignedXmlDebugLog.LogUnsafeCanonicalizationMethod(this, SignedInfo!.CanonicalizationMethod, SafeCanonicalizationMethods);
            return false;
        }

        private bool ReferenceUsesSafeTransformMethods(Reference reference)
        {
            TransformChain transformChain = reference.TransformChain;
            int transformCount = transformChain.Count;

            for (int i = 0; i < transformCount; i++)
            {
                Transform transform = transformChain[i];

                if (!IsSafeTransform(transform.Algorithm!))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSafeTransform(string transformAlgorithm)
        {
            // All canonicalization algorithms are valid transform algorithms.
            foreach (string safeAlgorithm in SafeCanonicalizationMethods)
            {
                if (string.Equals(safeAlgorithm, transformAlgorithm, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (string safeAlgorithm in DefaultSafeTransformMethods)
            {
                if (string.Equals(safeAlgorithm, transformAlgorithm, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            SignedXmlDebugLog.LogUnsafeTransformMethod(
                this,
                transformAlgorithm,
                SafeCanonicalizationMethods,
                DefaultSafeTransformMethods);

            return false;
        }

        // Get a list of the built in canonicalization algorithms, as well as any that the machine admin has
        // added to the valid set.
        private static IList<string> KnownCanonicalizationMethods
        {
            get
            {
                if (s_knownCanonicalizationMethods == null)
                {
                    // Start with the list that the machine admin added, if any
                    List<string> safeAlgorithms = new List<string>();

                    // Built in algorithms
                    safeAlgorithms.Add(XmlDsigC14NTransformUrl);
                    safeAlgorithms.Add(XmlDsigC14NWithCommentsTransformUrl);
                    safeAlgorithms.Add(XmlDsigExcC14NTransformUrl);
                    safeAlgorithms.Add(XmlDsigExcC14NWithCommentsTransformUrl);

                    s_knownCanonicalizationMethods = safeAlgorithms;
                }

                return s_knownCanonicalizationMethods;
            }
        }

        private static IList<string> DefaultSafeTransformMethods
        {
            get
            {
                if (s_defaultSafeTransformMethods == null)
                {
                    List<string> safeAlgorithms = new List<string>();

                    // Built in algorithms

                    // KnownCanonicalizationMethods don't need to be added here, because
                    // the validator will automatically accept those.
                    //
                    // xmldsig 6.6.1:
                    //     Any canonicalization algorithm that can be used for
                    //     CanonicalizationMethod can be used as a Transform.
                    safeAlgorithms.Add(XmlDsigEnvelopedSignatureTransformUrl);
                    safeAlgorithms.Add(XmlDsigBase64TransformUrl);
                    safeAlgorithms.Add(XmlLicenseTransformUrl);
                    safeAlgorithms.Add(XmlDecryptionTransformUrl);

                    s_defaultSafeTransformMethods = safeAlgorithms;
                }

                return s_defaultSafeTransformMethods;
            }
        }

        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        private byte[] GetC14NDigest(HashAlgorithm hash)
        {
            bool isKeyedHashAlgorithm = hash is KeyedHashAlgorithm;
            if (isKeyedHashAlgorithm || !_bCacheValid || !SignedInfo!.CacheValid)
            {
                string? baseUri = _containingDocument?.BaseURI;
                XmlResolver? resolver = (_bResolverSet ? _xmlResolver : XmlResolverHelper.GetThrowingResolver());
                XmlDocument doc = Utils.PreProcessElementInput(SignedInfo!.GetXml(), resolver!, baseUri);

                // Add non default namespaces in scope
                CanonicalXmlNodeList? namespaces = (_context == null ? null : Utils.GetPropagatedAttributes(_context));
                SignedXmlDebugLog.LogNamespacePropagation(this, namespaces);
                Utils.AddNamespaces(doc.DocumentElement!, namespaces);

                Transform c14nMethodTransform = SignedInfo.CanonicalizationMethodObject;
                c14nMethodTransform.Resolver = resolver;
                c14nMethodTransform.BaseURI = baseUri;

                SignedXmlDebugLog.LogBeginCanonicalization(this, c14nMethodTransform);
                c14nMethodTransform.LoadInput(doc);
                SignedXmlDebugLog.LogCanonicalizedOutput(this, c14nMethodTransform);
                _digestedSignedInfo = c14nMethodTransform.GetDigestedOutput(hash);

                _bCacheValid = !isKeyedHashAlgorithm;
            }
            return _digestedSignedInfo!;
        }

        private int GetReferenceLevel(int index, ArrayList references)
        {
            Debug.Assert(_refProcessed != null);
            Debug.Assert(_refLevelCache != null);

            if (_refProcessed[index]) return _refLevelCache[index];
            _refProcessed[index] = true;
            Reference reference = (Reference)references[index]!;
            if (string.IsNullOrEmpty(reference.Uri) || (reference.Uri.Length > 0 && reference.Uri[0] != '#'))
            {
                _refLevelCache[index] = 0;
                return 0;
            }
            if (reference.Uri.Length > 0 && reference.Uri[0] == '#')
            {
                string idref = Utils.ExtractIdFromLocalUri(reference.Uri);
                if (idref == "xpointer(/)")
                {
                    _refLevelCache[index] = 0;
                    return 0;
                }
                // If this is pointing to another reference
                for (int j = 0; j < references.Count; ++j)
                {
                    if (((Reference)references[j]!).Id == idref)
                    {
                        _refLevelCache[index] = GetReferenceLevel(j, references) + 1;
                        return (_refLevelCache[index]);
                    }
                }
                // Then the reference points to an object tag
                _refLevelCache[index] = 0;
                return 0;
            }
            // Malformed reference
            throw new CryptographicException(SR.Cryptography_Xml_InvalidReference);
        }

        private sealed class ReferenceLevelSortOrder : IComparer
        {
            private ArrayList? _references;
            public ReferenceLevelSortOrder() { }

            public ArrayList References
            {
                get { return _references!; }
                set { _references = value; }
            }

            public int Compare(object? a, object? b)
            {
                Reference? referenceA = a as Reference;
                Reference? referenceB = b as Reference;

                // Get the indexes
                int iIndexA = 0;
                int iIndexB = 0;
                int i = 0;
                foreach (Reference reference in References)
                {
                    if (reference == referenceA) iIndexA = i;
                    if (reference == referenceB) iIndexB = i;
                    i++;
                }

                int iLevelA = referenceA!.SignedXml!.GetReferenceLevel(iIndexA, References);
                int iLevelB = referenceB!.SignedXml!.GetReferenceLevel(iIndexB, References);
                return iLevelA.CompareTo(iLevelB);
            }
        }

        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        private void BuildDigestedReferences()
        {
            // Default the DigestMethod and Canonicalization
            ArrayList references = SignedInfo!.References;
            // Reset the cache
            _refProcessed = new bool[references.Count];
            _refLevelCache = new int[references.Count];

            ReferenceLevelSortOrder sortOrder = new ReferenceLevelSortOrder();
            sortOrder.References = references;
            // Don't alter the order of the references array list
            ArrayList sortedReferences = new ArrayList();
            foreach (Reference reference in references)
            {
                sortedReferences.Add(reference);
            }
            sortedReferences.Sort(sortOrder);

            CanonicalXmlNodeList nodeList = new CanonicalXmlNodeList();
            foreach (DataObject obj in m_signature.ObjectList)
            {
                nodeList.Add(obj.GetXml());
            }
            foreach (Reference reference in sortedReferences)
            {
                // If no DigestMethod has yet been set, default it to sha1
                reference.DigestMethod ??= Reference.DefaultDigestMethod;

                SignedXmlDebugLog.LogSigningReference(this, reference);

                reference.UpdateHashValue(_containingDocument!, nodeList);
                // If this reference has an Id attribute, add it
                if (reference.Id != null)
                    nodeList.Add(reference.GetXml());
            }
        }

        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        private bool CheckDigestedReferences()
        {
            ArrayList references = m_signature.SignedInfo!.References;
            for (int i = 0; i < references.Count; ++i)
            {
                Reference digestedReference = (Reference)references[i]!;

                if (!ReferenceUsesSafeTransformMethods(digestedReference))
                {
                    return false;
                }

                SignedXmlDebugLog.LogVerifyReference(this, digestedReference);
                byte[]? calculatedHash;
                try
                {
                    calculatedHash = digestedReference.CalculateHashValue(_containingDocument!, m_signature.ReferencedItems);
                }
                catch (CryptoSignedXmlRecursionException)
                {
                    SignedXmlDebugLog.LogSignedXmlRecursionLimit(this, digestedReference);
                    return false;
                }
                // Compare both hashes
                SignedXmlDebugLog.LogVerifyReferenceHash(this, digestedReference, calculatedHash, digestedReference.DigestValue);

                if (!CryptographicEquals(calculatedHash, digestedReference.DigestValue))
                {
                    return false;
                }
            }

            return true;
        }

        // Methods _must_ be marked both No Inlining and No Optimization to be fully opted out of optimization.
        // This is because if a candidate method is inlined, its method level attributes, including the NoOptimization
        // attribute, are lost.
        // This method makes no attempt to disguise the length of either of its inputs. It is assumed the attacker has
        // knowledge of the algorithms used, and thus the output length. Length is difficult to properly blind in modern CPUs.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool CryptographicEquals(byte[]? a, byte[]? b)
        {
            System.Diagnostics.Debug.Assert(a != null);
            System.Diagnostics.Debug.Assert(b != null);

            int result = 0;

            // Short cut if the lengths are not identical
            if (a.Length != b.Length)
                return false;

            unchecked
            {
                // Normally this caching doesn't matter, but with the optimizer off, this nets a non-trivial speedup.
                int aLength = a.Length;

                for (int i = 0; i < aLength; i++)
                    // We use subtraction here instead of XOR because the XOR algorithm gets ever so
                    // slightly faster as more and more differences pile up.
                    // This cannot overflow more than once (and back to 0) because bytes are 1 byte
                    // in length, and result is 4 bytes. The OR propagates all set bytes, so the differences
                    // can't add up and overflow a second time.
                    result |= (a[i] - b[i]);
            }

            return (0 == result);
        }

        // If we have a signature format validation callback, check to see if this signature's format (not
        // the signautre itself) is valid according to the validator.  A return value of true indicates that
        // the signature format is acceptable, false means that the format is not valid.
        private bool CheckSignatureFormat()
        {
            if (_signatureFormatValidator == null)
            {
                // No format validator means that we default to accepting the signature.  (This is
                // effectively compatibility mode with v3.5).
                return true;
            }

            SignedXmlDebugLog.LogBeginCheckSignatureFormat(this, _signatureFormatValidator);

            bool formatValid = _signatureFormatValidator(this);
            SignedXmlDebugLog.LogFormatValidationResult(this, formatValid);
            return formatValid;
        }

        [UnconditionalSuppressMessage("ILLink", "IL2026:RequiresUnreferencedCode", Justification = "ctors are marked as RDC")]
        [UnconditionalSuppressMessage("ILLink", "IL2057:UnrecognizedReflectionPattern", Justification = "ctors are marked as RDC")]
        private bool CheckSignedInfo(AsymmetricAlgorithm key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            SignedXmlDebugLog.LogBeginCheckSignedInfo(this, m_signature.SignedInfo!);

            SignatureDescription? signatureDescription = CryptoHelpers.CreateNonTransformFromName<SignatureDescription>(SignatureMethod);
            if (signatureDescription == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignatureDescriptionNotCreated);

            // Let's see if the key corresponds with the SignatureMethod
            Type ta = Type.GetType(signatureDescription.KeyAlgorithm!)!;
            if (!IsKeyTheCorrectAlgorithm(key, ta))
                return false;

            HashAlgorithm? hashAlgorithm = signatureDescription.CreateDigest();
            if (hashAlgorithm == null)
                throw new CryptographicException(SR.Cryptography_Xml_CreateHashAlgorithmFailed);
            byte[] hashval = GetC14NDigest(hashAlgorithm);

            AsymmetricSignatureDeformatter asymmetricSignatureDeformatter = signatureDescription.CreateDeformatter(key);
            SignedXmlDebugLog.LogVerifySignedInfo(this,
                                                  key,
                                                  signatureDescription,
                                                  hashAlgorithm,
                                                  asymmetricSignatureDeformatter,
                                                  hashval,
                                                  m_signature.SignatureValue);
            return asymmetricSignatureDeformatter.VerifySignature(hashval, m_signature.SignatureValue!);
        }

        private bool CheckSignedInfo(KeyedHashAlgorithm macAlg)
        {
            if (macAlg is null)
            {
                throw new ArgumentNullException(nameof(macAlg));
            }

            SignedXmlDebugLog.LogBeginCheckSignedInfo(this, m_signature.SignedInfo!);

            int signatureLength;
            if (m_signature.SignedInfo!.SignatureLength == null)
                signatureLength = macAlg.HashSize;
            else
                signatureLength = Convert.ToInt32(m_signature.SignedInfo.SignatureLength, null);

            // signatureLength should be less than hash size
            if (signatureLength < 0 || signatureLength > macAlg.HashSize)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidSignatureLength);
            if (signatureLength % 8 != 0)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidSignatureLength2);
            if (m_signature.SignatureValue == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignatureValueRequired);
            if (m_signature.SignatureValue.Length != signatureLength / 8)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidSignatureLength);

            // Calculate the hash
            byte[] hashValue = GetC14NDigest(macAlg);
            SignedXmlDebugLog.LogVerifySignedInfo(this, macAlg, hashValue, m_signature.SignatureValue);

            return m_signature.SignatureValue.AsSpan().SequenceEqual(hashValue.AsSpan(0, m_signature.SignatureValue.Length));
        }

        private static XmlElement? GetSingleReferenceTarget(XmlDocument document, string idAttributeName, string idValue)
        {
            // idValue has already been tested as an NCName (unless overridden for compatibility), so there's no
            // escaping that needs to be done here.
            string xPath = "//*[@" + idAttributeName + "=\"" + idValue + "\"]";

            // http://www.w3.org/TR/xmldsig-core/#sec-ReferenceProcessingModel says that for the form URI="#chapter1":
            //
            //   Identifies a node-set containing the element with ID attribute value 'chapter1' ...
            //
            // Note that it uses the singular. Therefore, if the match is ambiguous, we should consider the document invalid.
            //
            // In this case, we'll treat it the same as having found nothing across all fallbacks (but shortcut so that we don't
            // fall into a trap of finding a secondary element which wasn't the originally signed one).

            XmlNodeList? nodeList = document.SelectNodes(xPath);

            if (nodeList == null || nodeList.Count == 0)
            {
                return null;
            }

            if (nodeList.Count == 1)
            {
                return nodeList[0] as XmlElement;
            }

            throw new CryptographicException(SR.Cryptography_Xml_InvalidReference);
        }

        private static bool IsKeyTheCorrectAlgorithm(AsymmetricAlgorithm key, Type expectedType)
        {
            Type actualType = key.GetType();

            if (actualType == expectedType)
                return true;

            // This check exists solely for compatibility with 4.6. Normally, we would expect "expectedType" to be the superclass type and
            // the actualType to be the subclass.
            if (expectedType.IsSubclassOf(actualType))
                return true;

            //
            // "expectedType" comes from the KeyAlgorithm property of a SignatureDescription. The BCL SignatureDescription classes have historically
            // denoted provider-specific implementations ("RSACryptoServiceProvider") rather than the base class for the algorithm ("RSA"). We could
            // change those (at the risk of creating other compat problems) but we have no control over third party SignatureDescriptions.
            //
            // So, in the absence of a better approach, walk up the parent hierarchy until we find the ancestor that's a direct subclass of
            // AsymmetricAlgorithm and treat that as the algorithm identifier.
            //
            while (expectedType != null && expectedType.BaseType != typeof(AsymmetricAlgorithm))
            {
                expectedType = expectedType.BaseType!;
            }

            if (expectedType == null)
                return false;   // SignatureDescription specified something that isn't even a subclass of AsymmetricAlgorithm. For compatibility with 4.6, return false rather throw.

            if (actualType.IsSubclassOf(expectedType))
                return true;

            return false;
        }
    }
}
