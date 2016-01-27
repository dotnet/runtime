// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Threading;
    using System.Globalization;
    using System.Runtime.Versioning;
    using Microsoft.Win32;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class CryptoConfig {
        private static volatile Dictionary<string, string> defaultOidHT = null;
        private static volatile Dictionary<string, object> defaultNameHT = null;
        private static volatile Dictionary<string, string> machineOidHT = null;
        private static volatile Dictionary<string, string> machineNameHT = null;
        private static volatile Dictionary<string, Type> appNameHT = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static volatile Dictionary<string, string> appOidHT = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string MachineConfigFilename = "machine.config";

        private static volatile string version = null;

#if FEATURE_CRYPTO
        private static volatile bool s_fipsAlgorithmPolicy;
        private static volatile bool s_haveFipsAlgorithmPolicy;

        /// <summary>
        ///     Determine if the runtime should enforce that only FIPS certified algorithms are created. This
        ///     property returns true if this policy should be enforced, false if any algorithm may be created.
        /// </summary>
        public static bool AllowOnlyFipsAlgorithms {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (!s_haveFipsAlgorithmPolicy)
                {
                    //
                    // If the user has not disabled FIPS enforcement in a config file, check the CNG settings
                    // on Vista and the FIPS registry key downlevel.
                    //

#if !FEATURE_CORECLR
                    if (Utils._GetEnforceFipsPolicySetting()) {
                        if (Environment.OSVersion.Version.Major >= 6) {
                            bool fipsEnabled;
                            uint policyReadStatus = Win32Native.BCryptGetFipsAlgorithmMode(out fipsEnabled);

                            bool readPolicy = policyReadStatus == Win32Native.STATUS_SUCCESS ||
                                              policyReadStatus == Win32Native.STATUS_OBJECT_NAME_NOT_FOUND;

                            s_fipsAlgorithmPolicy = !readPolicy || fipsEnabled;
                            s_haveFipsAlgorithmPolicy = true;
                        }
                        else {
                            s_fipsAlgorithmPolicy = Utils.ReadLegacyFipsPolicy();
                            s_haveFipsAlgorithmPolicy = true;
                        }
                    }
                    else
#endif // !FEATURE_CORECLR
                    {
                        s_fipsAlgorithmPolicy = false;
                        s_haveFipsAlgorithmPolicy = true;
                    }
                }

                return s_fipsAlgorithmPolicy;
            }
        }
#endif // FEATURE_CRYPTO

        private static string Version
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if(version == null)
                    version = ((RuntimeType)typeof(CryptoConfig)).GetRuntimeAssembly().GetVersion().ToString();

                return version;
            }
        }

        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        private static Dictionary<string, string> DefaultOidHT {
            get {
                if (defaultOidHT == null) {
                    Dictionary<string, string> ht = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("SHA", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA1", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1", Constants.OID_OIWSEC_SHA1);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", Constants.OID_OIWSEC_SHA1);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("System.Security.Cryptography.SHA1Managed", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256", Constants.OID_OIWSEC_SHA256);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO 
                    ht.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256Cng", Constants.OID_OIWSEC_SHA256);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("System.Security.Cryptography.SHA256Managed", Constants.OID_OIWSEC_SHA256);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Cng", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Managed", Constants.OID_OIWSEC_SHA384);
                    ht.Add("SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Cng", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Managed", Constants.OID_OIWSEC_SHA512);
                    ht.Add("RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5CryptoServiceProvider", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5Managed", Constants.OID_RSA_MD5);
                    ht.Add("TripleDESKeyWrap", Constants.OID_RSA_SMIMEalgCMS3DESwrap);
                    ht.Add("RC2", Constants.OID_RSA_RC2CBC);
                    ht.Add("System.Security.Cryptography.RC2CryptoServiceProvider", Constants.OID_RSA_RC2CBC);
                    ht.Add("DES", Constants.OID_OIWSEC_desCBC);
                    ht.Add("System.Security.Cryptography.DESCryptoServiceProvider", Constants.OID_OIWSEC_desCBC);
                    ht.Add("TripleDES", Constants.OID_RSA_DES_EDE3_CBC);
                    ht.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", Constants.OID_RSA_DES_EDE3_CBC);
#endif // FEATURE_CRYPTO
                    defaultOidHT = ht;
                }
                return defaultOidHT;
            }
        }

        private static Dictionary<string, object> DefaultNameHT {
            get {
                if (defaultNameHT == null) {
                    Dictionary<string, object> ht = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
#if FEATURE_CRYPTO
                    Type SHA1CryptoServiceProviderType = typeof(System.Security.Cryptography.SHA1CryptoServiceProvider);
                    Type MD5CryptoServiceProviderType = typeof(System.Security.Cryptography.MD5CryptoServiceProvider);
                    Type RIPEMD160ManagedType  = typeof(System.Security.Cryptography.RIPEMD160Managed); 
                    Type HMACMD5Type       = typeof(System.Security.Cryptography.HMACMD5);
                    Type HMACRIPEMD160Type = typeof(System.Security.Cryptography.HMACRIPEMD160);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    Type HMACSHA1Type      = typeof(System.Security.Cryptography.HMACSHA1);
                    Type HMACSHA256Type    = typeof(System.Security.Cryptography.HMACSHA256);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    Type HMACSHA384Type    = typeof(System.Security.Cryptography.HMACSHA384);
                    Type HMACSHA512Type    = typeof(System.Security.Cryptography.HMACSHA512);
                    Type MAC3DESType       = typeof(System.Security.Cryptography.MACTripleDES);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    Type RSACryptoServiceProviderType = typeof(System.Security.Cryptography.RSACryptoServiceProvider); 
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO && !FEATURE_CORECLR
                    Type DSACryptoServiceProviderType = typeof(System.Security.Cryptography.DSACryptoServiceProvider); 
                    Type DESCryptoServiceProviderType = typeof(System.Security.Cryptography.DESCryptoServiceProvider); 
                    Type TripleDESCryptoServiceProviderType = typeof(System.Security.Cryptography.TripleDESCryptoServiceProvider); 
                    Type RC2CryptoServiceProviderType = typeof(System.Security.Cryptography.RC2CryptoServiceProvider); 
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    Type RijndaelManagedType = typeof(System.Security.Cryptography.RijndaelManaged); 
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    Type DSASignatureDescriptionType = typeof(System.Security.Cryptography.DSASignatureDescription);
                    Type RSAPKCS1SHA1SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA1SignatureDescription);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    Type RNGCryptoServiceProviderType = typeof(System.Security.Cryptography.RNGCryptoServiceProvider);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO

                    // Cryptography algorithms in System.Core are referenced by name rather than type so
                    // that we don't force System.Core to load if we don't need any of its algorithms
                    string AesCryptoServiceProviderType = "System.Security.Cryptography.AesCryptoServiceProvider, " + AssemblyRef.SystemCore;
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    string AesManagedType = "System.Security.Cryptography.AesManaged, " + AssemblyRef.SystemCore;
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
#if !FEATURE_CORECLR
                    string ECDiffieHellmanCngType = "System.Security.Cryptography.ECDiffieHellmanCng, " + AssemblyRef.SystemCore;
                    string ECDsaCngType = "System.Security.Cryptography.ECDsaCng, " + AssemblyRef.SystemCore;
#endif
                    string MD5CngType = "System.Security.Cryptography.MD5Cng, " + AssemblyRef.SystemCore;
                    string SHA1CngType = "System.Security.Cryptography.SHA1Cng, " + AssemblyRef.SystemCore;
                    string SHA256CngType = "System.Security.Cryptography.SHA256Cng, " + AssemblyRef.SystemCore;
                    string SHA256CryptoServiceProviderType = "System.Security.Cryptography.SHA256CryptoServiceProvider, " + AssemblyRef.SystemCore;
                    string SHA384CngType = "System.Security.Cryptography.SHA384Cng, " + AssemblyRef.SystemCore;
                    string SHA384CryptoSerivceProviderType = "System.Security.Cryptography.SHA384CryptoServiceProvider, " + AssemblyRef.SystemCore;
                    string SHA512CngType = "System.Security.Cryptography.SHA512Cng, " + AssemblyRef.SystemCore;
                    string SHA512CryptoServiceProviderType = "System.Security.Cryptography.SHA512CryptoServiceProvider, " + AssemblyRef.SystemCore;
#endif //FEATURE_CRYPTO


#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    bool fipsOnly = AllowOnlyFipsAlgorithms;
                    object SHA256DefaultType = typeof(SHA256Managed);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO

#if FEATURE_CRYPTO
                    if (fipsOnly)
                    {
                        SHA256DefaultType = SHA256CngType;
                    }
                    object SHA384DefaultType = fipsOnly ? (object)SHA384CngType : (object)typeof(SHA384Managed);
                    object SHA512DefaultType = fipsOnly ? (object)SHA512CngType : (object)typeof(SHA512Managed);

                    // Cryptography algorithms in System.Security
                    string DpapiDataProtectorType = "System.Security.Cryptography.DpapiDataProtector, " + AssemblyRef.SystemSecurity;
#endif //FEATURE_CRYPTO

#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    // Random number generator
                    ht.Add("RandomNumberGenerator", RNGCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RandomNumberGenerator", RNGCryptoServiceProviderType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO

                    // Hash functions
                    ht.Add("SHA", SHA1CryptoServiceProviderType);
                    ht.Add("SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.SHA1Cng", SHA1CngType);
                    ht.Add("System.Security.Cryptography.HashAlgorithm", SHA1CryptoServiceProviderType);
                    ht.Add("MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5Cng", MD5CngType);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("SHA256", SHA256DefaultType);
                    ht.Add("SHA-256", SHA256DefaultType);
                    ht.Add("System.Security.Cryptography.SHA256", SHA256DefaultType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("System.Security.Cryptography.SHA256Cng", SHA256CngType);
                    ht.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", SHA256CryptoServiceProviderType);
                    ht.Add("SHA384", SHA384DefaultType);
                    ht.Add("SHA-384", SHA384DefaultType);
                    ht.Add("System.Security.Cryptography.SHA384", SHA384DefaultType);
                    ht.Add("System.Security.Cryptography.SHA384Cng", SHA384CngType);
                    ht.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", SHA384CryptoSerivceProviderType);
                    ht.Add("SHA512", SHA512DefaultType);
                    ht.Add("SHA-512", SHA512DefaultType);
                    ht.Add("System.Security.Cryptography.SHA512", SHA512DefaultType);
                    ht.Add("System.Security.Cryptography.SHA512Cng", SHA512CngType);
                    ht.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", SHA512CryptoServiceProviderType);
                    ht.Add("RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("RIPEMD-160", RIPEMD160ManagedType);
                    ht.Add("System.Security.Cryptography.RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", RIPEMD160ManagedType);

                    // Keyed Hash Algorithms
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("System.Security.Cryptography.HMAC", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.KeyedHashAlgorithm", HMACSHA1Type);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("HMACMD5", HMACMD5Type);
                    ht.Add("System.Security.Cryptography.HMACMD5", HMACMD5Type);
                    ht.Add("HMACRIPEMD160", HMACRIPEMD160Type);
                    ht.Add("System.Security.Cryptography.HMACRIPEMD160", HMACRIPEMD160Type);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("HMACSHA1", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.HMACSHA1", HMACSHA1Type);
                    ht.Add("HMACSHA256", HMACSHA256Type);
                    ht.Add("System.Security.Cryptography.HMACSHA256", HMACSHA256Type);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("HMACSHA384", HMACSHA384Type);
                    ht.Add("System.Security.Cryptography.HMACSHA384", HMACSHA384Type);
                    ht.Add("HMACSHA512", HMACSHA512Type);
                    ht.Add("System.Security.Cryptography.HMACSHA512", HMACSHA512Type);
                    ht.Add("MACTripleDES", MAC3DESType);
                    ht.Add("System.Security.Cryptography.MACTripleDES", MAC3DESType);

                    // Asymmetric algorithms
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AsymmetricAlgorithm", RSACryptoServiceProviderType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO && !FEATURE_CORECLR
                    ht.Add("DSA", DSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.DSA", DSACryptoServiceProviderType);
                    ht.Add("ECDsa", ECDsaCngType);
                    ht.Add("ECDsaCng", ECDsaCngType);
                    ht.Add("System.Security.Cryptography.ECDsaCng", ECDsaCngType);
                    ht.Add("ECDH", ECDiffieHellmanCngType);
                    ht.Add("ECDiffieHellman", ECDiffieHellmanCngType);
                    ht.Add("ECDiffieHellmanCng", ECDiffieHellmanCngType);
                    ht.Add("System.Security.Cryptography.ECDiffieHellmanCng", ECDiffieHellmanCngType);

                    // Symmetric algorithms
                    ht.Add("DES", DESCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.DES", DESCryptoServiceProviderType);
                    ht.Add("3DES", TripleDESCryptoServiceProviderType);
                    ht.Add("TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("Triple DES", TripleDESCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.TripleDES", TripleDESCryptoServiceProviderType);
                    ht.Add("RC2", RC2CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RC2", RC2CryptoServiceProviderType);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("Rijndael", RijndaelManagedType);
                    ht.Add("System.Security.Cryptography.Rijndael", RijndaelManagedType);
                    // Rijndael is the default symmetric cipher because (a) it's the strongest and (b) we know we have an implementation everywhere
                    ht.Add("System.Security.Cryptography.SymmetricAlgorithm", RijndaelManagedType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("AES", AesCryptoServiceProviderType);
                    ht.Add("AesCryptoServiceProvider", AesCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AesCryptoServiceProvider", AesCryptoServiceProviderType);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("AesManaged", AesManagedType);
                    ht.Add("System.Security.Cryptography.AesManaged", AesManagedType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    // Data protectors
                    ht.Add("DpapiDataProtector", DpapiDataProtectorType);
                    ht.Add("System.Security.Cryptography.DpapiDataProtector", DpapiDataProtectorType);

                    // Asymmetric signature descriptions
                    ht.Add("http://www.w3.org/2000/09/xmldsig#dsa-sha1", DSASignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.DSASignatureDescription", DSASignatureDescriptionType);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#rsa-sha1", RSAPKCS1SHA1SignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.RSASignatureDescription", RSAPKCS1SHA1SignatureDescriptionType);

                    // Xml Dsig/Enc Hash algorithms
                    ht.Add("http://www.w3.org/2000/09/xmldsig#sha1", SHA1CryptoServiceProviderType);
                    // Add the other hash algorithms introduced with XML Encryption
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha256", SHA256DefaultType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO && !FEATURE_CORECLR
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha512", SHA512DefaultType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#ripemd160", RIPEMD160ManagedType);

                    // Xml Encryption symmetric keys
                    ht.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", DESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", TripleDESCryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", TripleDESCryptoServiceProviderType);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", RijndaelManagedType);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO

                    // Xml Dsig Transforms
                    // First arg must match the constants defined in System.Security.Cryptography.Xml.SignedXml
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", "System.Security.Cryptography.Xml.XmlDsigC14NTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", "System.Security.Cryptography.Xml.XmlDsigC14NWithCommentsTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#", "System.Security.Cryptography.Xml.XmlDsigExcC14NTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", "System.Security.Cryptography.Xml.XmlDsigExcC14NWithCommentsTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#base64", "System.Security.Cryptography.Xml.XmlDsigBase64Transform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/1999/REC-xpath-19991116", "System.Security.Cryptography.Xml.XmlDsigXPathTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/1999/REC-xslt-19991116", "System.Security.Cryptography.Xml.XmlDsigXsltTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#enveloped-signature", "System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform, " + AssemblyRef.SystemSecurity);

                    // the decryption transform
                    ht.Add("http://www.w3.org/2002/07/decrypt#XML", "System.Security.Cryptography.Xml.XmlDecryptionTransform, " + AssemblyRef.SystemSecurity);

                    // Xml licence transform.
                    ht.Add("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform", "System.Security.Cryptography.Xml.XmlLicenseTransform, " + AssemblyRef.SystemSecurity);

                    // Xml Dsig KeyInfo
                    // First arg (the key) is formed as elem.NamespaceURI + " " + elem.LocalName
                    ht.Add("http://www.w3.org/2000/09/xmldsig# X509Data", "System.Security.Cryptography.Xml.KeyInfoX509Data, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyName", "System.Security.Cryptography.Xml.KeyInfoName, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/DSAKeyValue", "System.Security.Cryptography.Xml.DSAKeyValue, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/RSAKeyValue", "System.Security.Cryptography.Xml.RSAKeyValue, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# RetrievalMethod", "System.Security.Cryptography.Xml.KeyInfoRetrievalMethod, " + AssemblyRef.SystemSecurity);

                    // Xml EncryptedKey
                    ht.Add("http://www.w3.org/2001/04/xmlenc# EncryptedKey", "System.Security.Cryptography.Xml.KeyInfoEncryptedKey, " + AssemblyRef.SystemSecurity);

#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    // Xml Dsig HMAC URIs from http://www.w3.org/TR/xmldsig-core/
                    ht.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1", HMACSHA1Type);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO

                    // Xml Dsig-more Uri's as defined in http://www.ietf.org/rfc/rfc4051.txt
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#md5", MD5CryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", SHA384DefaultType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", HMACMD5Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160", HMACRIPEMD160Type);
#endif //FEATURE_CRYPTO
#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", HMACSHA256Type);
#endif //FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
#if FEATURE_CRYPTO
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", HMACSHA384Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", HMACSHA512Type);
                    // X509 Extensions (custom decoders)
                    // Basic Constraints OID value
                    ht.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyRef.System);
                    ht.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyRef.System);
                    // Subject Key Identifier OID value
                    ht.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, " + AssemblyRef.System);
                    // Key Usage OID value
                    ht.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, " + AssemblyRef.System);
                    // Enhanced Key Usage OID value
                    ht.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, " + AssemblyRef.System);

                    // X509Chain class can be overridden to use a different chain engine.
                    ht.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, " + AssemblyRef.System);

                    // PKCS9 attributes
                    ht.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, " + AssemblyRef.SystemSecurity);
#endif // FEATURE_CRYPTO

                    defaultNameHT = ht;
                }
                return defaultNameHT;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void InitializeConfigInfo()
        {
#if FEATURE_CRYPTO && !FEATURE_CORECLR
            if (machineNameHT == null)
            {
                lock(InternalSyncObject)
                {
                    if(machineNameHT == null)
                    {
                        ConfigNode cryptoConfig = OpenCryptoConfig();
                        if (cryptoConfig != null)
                        {
                            foreach (ConfigNode node in cryptoConfig.Children)
                            {
                                if (machineNameHT != null && machineOidHT != null)
                                {
                                    break;
                                }
                                else if (machineNameHT == null &&
                                    String.Compare(node.Name, "cryptoNameMapping", StringComparison.Ordinal) == 0)
                                {
                                    machineNameHT = InitializeNameMappings(node);
                                }
                                else if (machineOidHT == null &&
                                         String.Compare(node.Name, "oidMap", StringComparison.Ordinal) == 0)
                                {
                                    machineOidHT = InitializeOidMappings(node);
                                }
                            }
                        }

                        // if we couldn't access the config file, or it didn't contain our config section
                        // just create empty tables so that we don't end up trying to read the file
                        // on every access to InitializeConfigInfo()
                        if (machineNameHT == null)
                            machineNameHT = new Dictionary<string, string>();
                        if (machineOidHT == null)
                            machineOidHT = new Dictionary<string, string>();

                    }
                }
            }

#else
            if (machineNameHT == null)
                machineNameHT = new Dictionary<string, string>();
            if (machineOidHT == null)
                machineOidHT = new Dictionary<string, string>();

#endif //FEATURE_CRYPTO
        }

        /// <summary>
        ///     Add a set of name -> algorithm mappings to be used for the current AppDomain.  These mappings
        ///     take precidense over the built-in mappings and the mappings in machine.config.  This API is
        ///     critical to prevent partial trust code from hooking trusted crypto operations.
        /// </summary>
        [SecurityCritical]
        public static void AddAlgorithm(Type algorithm, params string[] names) {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            if (!algorithm.IsVisible)
                throw new ArgumentException(Environment.GetResourceString("Cryptography_AlgorithmTypesMustBeVisible"), "algorithm");
            if (names == null)
                throw new ArgumentNullException("names");
            Contract.EndContractBlock();

            string[] algorithmNames = new string[names.Length];
            Array.Copy(names, algorithmNames, algorithmNames.Length);

            // Pre-check the algorithm names for validity so that we don't add a few of the names and then
            // throw an exception if we find an invalid name partway through the list.
            foreach (string name in algorithmNames) {
                if (String.IsNullOrEmpty(name)) {
                    throw new ArgumentException(Environment.GetResourceString("Cryptography_AddNullOrEmptyName"));
                }
            }

            // Everything looks valid, so we're safe to take the table lock and add the name mappings.
            lock (InternalSyncObject) {
                foreach (string name in algorithmNames) {
                    appNameHT[name] = algorithm;
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static object CreateFromName (string name, params object[] args) {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            Type retvalType = null;
            Object retval;

            // First we'll do the machine-wide stuff, initializing if necessary
            InitializeConfigInfo();

            // Check to see if we have an applicaiton defined mapping
            lock (InternalSyncObject) {
                retvalType = appNameHT.GetValueOrDefault(name);
            }

            // If we don't have a application defined mapping, search the machine table
            if (retvalType == null) {
                BCLDebug.Assert(machineNameHT != null, "machineNameHT != null");
                String retvalTypeString = machineNameHT.GetValueOrDefault(name);
                if (retvalTypeString != null) {
                    retvalType = Type.GetType(retvalTypeString, false, false);
                    if (retvalType != null && !retvalType.IsVisible) 
                        retvalType = null;
                }
            }

            // If we didn't find it in the machine-wide table,  look in the default table
            if (retvalType == null) {
                // We allow the default table to Types and Strings
                // Types get used for other stuff in mscorlib.dll
                // strings get used for delay-loaded stuff like System.Security.dll
                Object retvalObj  = DefaultNameHT.GetValueOrDefault(name);
                if (retvalObj != null) {
                    if (retvalObj is Type) {
                        retvalType = (Type) retvalObj;
                    } else if (retvalObj is String) {
                        retvalType = Type.GetType((String) retvalObj, false, false);
                        if (retvalType != null && !retvalType.IsVisible) 
                            retvalType = null;
                    }
                }
            }

            // Maybe they gave us a classname.
            if (retvalType == null) {
                retvalType = Type.GetType(name, false, false);
                if (retvalType != null && !retvalType.IsVisible) 
                    retvalType = null;
            }

            // Still null? Then we didn't find it 
            if (retvalType == null)
                return null;

            // Perform a CreateInstance by hand so we can check that the
            // constructor doesn't have a linktime demand attached (which would
            // be incorrrectly applied against mscorlib otherwise).
            RuntimeType rtType = retvalType as RuntimeType;
            if (rtType == null)
                return null;
            if (args == null)
                args = new Object[]{};

            // Locate all constructors.
            MethodBase[] cons = rtType.GetConstructors(Activator.ConstructorDefault);

            if (cons == null)
                return null;

            List<MethodBase> candidates = new List<MethodBase>();
            for (int i = 0; i < cons.Length; i ++) {
                MethodBase con = cons[i];
                if (con.GetParameters().Length == args.Length) {
                    candidates.Add(con);
                }
            }

            if (candidates.Count == 0) 
                return null;

            cons = candidates.ToArray();

            // Bind to matching ctor.
            Object state;
            RuntimeConstructorInfo rci = Type.DefaultBinder.BindToMethod(Activator.ConstructorDefault,
                                                                         cons,
                                                                         ref args,
                                                                         null,
                                                                         null,
                                                                         null,
                                                                         out state) as RuntimeConstructorInfo;

            // Check for ctor we don't like (non-existant, delegate or decorated
            // with declarative linktime demand).
            if (rci == null || typeof(Delegate).IsAssignableFrom(rci.DeclaringType))
                return null;

            // Ctor invoke (actually causes the allocation as well).
            retval = rci.Invoke(Activator.ConstructorDefault, Type.DefaultBinder, args, null);

            // Reset any parameter re-ordering performed by the binder.
            if (state != null)
                Type.DefaultBinder.ReorderArgumentArray(ref args, state);

            return retval;
        }

        public static object CreateFromName (string name) {
            return CreateFromName(name, null);
        }

        /// <summary>
        ///     Add a set of name -> OID mappings to be used for the current AppDomain.  These mappings
        ///     take precidense over the built-in mappings and the mappings in machine.config.  This API is
        ///     critical to prevent partial trust code from hooking trusted crypto operations.
        /// </summary>
        [SecurityCritical]
        public static void AddOID(string oid, params string[] names) {
            if (oid == null)
                throw new ArgumentNullException("oid");
            if (names == null)
                throw new ArgumentNullException("names");
            Contract.EndContractBlock();

            string[] oidNames = new string[names.Length];
            Array.Copy(names, oidNames, oidNames.Length);

            // Pre-check the input names for validity, so that we don't add a few of the names and throw an
            // exception if an invalid name is found further down the array. 
            foreach (string name in oidNames) {
                if (String.IsNullOrEmpty(name)) {
                    throw new ArgumentException(Environment.GetResourceString("Cryptography_AddNullOrEmptyName"));
                }
            }

            // Everything is valid, so we're good to lock the hash table and add the application mappings
            lock (InternalSyncObject) {
                foreach (string name in oidNames) {
                    appOidHT[name] = oid;
                }
            }
        }

        public static string MapNameToOID (string name) {
            return MapNameToOID(name, OidGroup.AllGroups);
        }

        [SecuritySafeCritical]
        internal static string MapNameToOID(string name, OidGroup oidGroup) {
            if (name == null) 
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            // First we'll do the machine-wide stuff, initializing if necessary
            InitializeConfigInfo();

            string oid = null;

            // Check to see if we have an application defined mapping
            lock (InternalSyncObject) {
                oid = appOidHT.GetValueOrDefault(name);
            }

            // If we didn't find an application defined mapping, search the machine table
            if (oid == null)
                oid = machineOidHT.GetValueOrDefault(name);

            // If we didn't find it in the machine-wide table, look in the default table
            if (oid == null)
                oid = DefaultOidHT.GetValueOrDefault(name);

#if FEATURE_CRYPTO || FEATURE_LEGACYNETCFCRYPTO
            // Try the CAPI table association
            if (oid == null)
                oid = X509Utils.GetOidFromFriendlyName(name, oidGroup);
#endif // FEATURE_CRYPTO

            return oid;
        }

        static public byte[] EncodeOID (string str) {
            if (str == null) {
                throw new ArgumentNullException("str");
            }
            Contract.EndContractBlock();
            char[] sepArray = { '.' }; // valid ASN.1 separators
            String[] oidString = str.Split(sepArray);
            uint[] oidNums = new uint[oidString.Length];
            for (int i = 0; i < oidString.Length; i++) {
                oidNums[i] = (uint) Int32.Parse(oidString[i], CultureInfo.InvariantCulture);
            }

            // Allocate the array to receive encoded oidNums
            byte[] encodedOidNums = new byte[oidNums.Length * 5]; // this is guaranteed to be longer than necessary
            int encodedOidNumsIndex = 0;
            // Handle the first two oidNums special
            if (oidNums.Length < 2) {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }
            uint firstTwoOidNums = (oidNums[0] * 40) + oidNums[1];
            byte[] retval = EncodeSingleOIDNum(firstTwoOidNums);
            Array.Copy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
            encodedOidNumsIndex += retval.Length;
            for (int i = 2; i < oidNums.Length; i++) {
                retval = EncodeSingleOIDNum(oidNums[i]);
                Buffer.InternalBlockCopy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
                encodedOidNumsIndex += retval.Length;
            }

            // final return value is 06 <length> || encodedOidNums
            if (encodedOidNumsIndex > 0x7f) {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_Config_EncodedOIDError"));
            }
            retval = new byte[ encodedOidNumsIndex + 2];
            retval[0] = (byte) 0x06;
            retval[1] = (byte) encodedOidNumsIndex;
            Buffer.InternalBlockCopy(encodedOidNums, 0, retval, 2, encodedOidNumsIndex);
            return retval;
        }

        static private byte[] EncodeSingleOIDNum(uint dwValue) {
            byte[] retval;

            if ((int)dwValue < 0x80) {
                retval = new byte[1];
                retval[0] = (byte) dwValue;
                return retval;
            }
            else if (dwValue < 0x4000) {
                retval = new byte[2];
                retval[0]   = (byte) ((dwValue >> 7) | 0x80);
                retval[1] = (byte) (dwValue & 0x7f);
                return retval;
            }
            else if (dwValue < 0x200000) {
                retval = new byte[3];
                retval[0] = (byte) ((dwValue >> 14) | 0x80);
                retval[1] = (byte) ((dwValue >> 7) | 0x80);
                retval[2] = (byte) (dwValue & 0x7f);
                return retval;
            }
            else if (dwValue < 0x10000000) {
                retval = new byte[4];
                retval[0] = (byte) ((dwValue >> 21) | 0x80);
                retval[1] = (byte) ((dwValue >> 14) | 0x80);
                retval[2] = (byte) ((dwValue >> 7) | 0x80);
                retval[3] = (byte) (dwValue & 0x7f);
                return retval;
            }
            else {
                retval = new byte[5];
                retval[0] = (byte) ((dwValue >> 28) | 0x80);
                retval[1] = (byte) ((dwValue >> 21) | 0x80);
                retval[2] = (byte) ((dwValue >> 14) | 0x80);
                retval[3] = (byte) ((dwValue >> 7) | 0x80);
                retval[4] = (byte) (dwValue & 0x7f);
                return retval;
            }
        }

        private static Dictionary<string, string> InitializeNameMappings(ConfigNode nameMappingNode)
        {
            Contract.Assert(nameMappingNode != null, "No name mappings");
            Contract.Assert(String.Compare(nameMappingNode.Name, "cryptoNameMapping", StringComparison.Ordinal) == 0, "Invalid name mapping root");

            Dictionary<string, string> nameMappings = new Dictionary<string, string>();
            Dictionary<string, string> typeAliases = new Dictionary<string, string>();

            // find the cryptoClases element
            foreach (ConfigNode node in nameMappingNode.Children)
            {
                if (String.Compare(node.Name, "cryptoClasses", StringComparison.Ordinal) == 0)
                {
                    foreach(ConfigNode cryptoClass in node.Children)
                    {
                        if (String.Compare(cryptoClass.Name, "cryptoClass", StringComparison.Ordinal) == 0)
                        {
                            if (cryptoClass.Attributes.Count > 0)
                            {
                                DictionaryEntry attribute = (DictionaryEntry)cryptoClass.Attributes[0];
                                typeAliases.Add((string)attribute.Key, (string)attribute.Value);
                            }
                        }
                    }
                }
                else if(String.Compare(node.Name, "nameEntry", StringComparison.Ordinal) == 0)
                {
                    string friendlyName = null;
                    string className = null;

                    foreach(DictionaryEntry attribute in node.Attributes)
                    {
                        if(String.Compare((string)attribute.Key, "name", StringComparison.Ordinal) == 0)
                            friendlyName = (string)attribute.Value;
                        else if(String.Compare((string)attribute.Key, "class", StringComparison.Ordinal) == 0)
                            className = (string)attribute.Value;
                    }

                    if (friendlyName != null && className != null)
                    {
                        string typeName = typeAliases.GetValueOrDefault(className);
                        if (typeName != null)
                            nameMappings.Add(friendlyName, typeName);
                    }
                }
            }

            return nameMappings;
        }

        private static Dictionary<string, string> InitializeOidMappings(ConfigNode oidMappingNode)
        {
            Contract.Assert(oidMappingNode != null, "No OID mappings");
            Contract.Assert(String.Compare(oidMappingNode.Name, "oidMap", StringComparison.Ordinal) == 0, "Invalid OID mapping root");

            Dictionary<string, string> oidMap = new Dictionary<string, string>();
            foreach (ConfigNode node in oidMappingNode.Children)
            {
                if (String.Compare(node.Name, "oidEntry", StringComparison.Ordinal) == 0)
                {
                    string oidString = null;
                    string friendlyName = null;

                    foreach (DictionaryEntry attribute in node.Attributes)
                    {
                        if (String.Compare((string)attribute.Key, "OID", StringComparison.Ordinal) == 0)
                            oidString = (string)attribute.Value;
                        else if (String.Compare((string)attribute.Key, "name", StringComparison.Ordinal) == 0)
                            friendlyName = (string)attribute.Value;
                    }

                    if ((friendlyName != null) && (oidString != null))
                        oidMap.Add(friendlyName, oidString);
                }
            }

            return oidMap;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static ConfigNode OpenCryptoConfig()
        {
            string machineConfigFile = System.Security.Util.Config.MachineDirectory + MachineConfigFilename;
            new FileIOPermission(FileIOPermissionAccess.Read, machineConfigFile).Assert();
            if (!File.Exists(machineConfigFile))
                return null;
            CodeAccessPermission.RevertAssert();

            ConfigTreeParser parser = new ConfigTreeParser();
            ConfigNode rootNode = parser.Parse(machineConfigFile, "configuration", true);
            if (rootNode == null)
                return null;

            // now, find the mscorlib tag with our version
            ConfigNode mscorlibNode = null;
            foreach (ConfigNode node in rootNode.Children)
            {
                bool versionSpecificMscorlib = false;

                if (String.Compare(node.Name, "mscorlib", StringComparison.Ordinal) == 0)
                {
                    foreach (DictionaryEntry attribute in node.Attributes)
                    {
                        if (String.Compare((string)attribute.Key, "version", StringComparison.Ordinal) == 0)
                        {
                            versionSpecificMscorlib = true;

                            if (String.Compare((string)attribute.Value, Version, StringComparison.Ordinal) == 0)
                            {
                                mscorlibNode = node;
                                break;
                            }
                        }
                    }

                    // if this mscorlib element did not have a version attribute, then use it
                    if (!versionSpecificMscorlib)
                        mscorlibNode = node;
                }

                // use the first matching mscorlib we find
                if (mscorlibNode != null)
                    break;
            }

            if (mscorlibNode == null)
                return null;

            // now look for the first crypto settings element
            foreach (ConfigNode node in mscorlibNode.Children)
            {
                if (String.Compare(node.Name, "cryptographySettings", StringComparison.Ordinal) == 0)
                    return node;
            }

            return null;
        }
    }
}
