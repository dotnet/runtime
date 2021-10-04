// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public partial class CryptoConfig
    {
        private const string AssemblyName_Cng = "System.Security.Cryptography.Cng";
        private const string AssemblyName_Csp = "System.Security.Cryptography.Csp";
        private const string AssemblyName_Pkcs = "System.Security.Cryptography.Pkcs";
        private const string AssemblyName_X509Certificates = "System.Security.Cryptography.X509Certificates";

        private const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        private const string OID_RSA_SMIMEalgCMS3DESwrap = "1.2.840.113549.1.9.16.3.6";
        private const string OID_RSA_MD5 = "1.2.840.113549.2.5";
        private const string OID_RSA_RC2CBC = "1.2.840.113549.3.2";
        private const string OID_RSA_DES_EDE3_CBC = "1.2.840.113549.3.7";
        private const string OID_OIWSEC_desCBC = "1.3.14.3.2.7";
        private const string OID_OIWSEC_SHA1 = "1.3.14.3.2.26";
        private const string OID_OIWSEC_SHA256 = "2.16.840.1.101.3.4.2.1";
        private const string OID_OIWSEC_SHA384 = "2.16.840.1.101.3.4.2.2";
        private const string OID_OIWSEC_SHA512 = "2.16.840.1.101.3.4.2.3";
        private const string OID_OIWSEC_RIPEMD160 = "1.3.36.3.2.1";

        private const string ECDsaIdentifier = "ECDsa";

        private static volatile Dictionary<string, string>? s_defaultOidHT;
        private static volatile Dictionary<string, object>? s_defaultNameHT;
        private static readonly ConcurrentDictionary<string, Type> appNameHT = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, string> appOidHT = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string> DefaultOidHT
        {
            get
            {
                if (s_defaultOidHT != null)
                {
                    return s_defaultOidHT;
                }

                int capacity = 37;
                Dictionary<string, string> ht = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);

                ht.Add("SHA", OID_OIWSEC_SHA1);
                ht.Add("SHA1", OID_OIWSEC_SHA1);
                ht.Add("System.Security.Cryptography.SHA1", OID_OIWSEC_SHA1);
                ht.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", OID_OIWSEC_SHA1);
                ht.Add("System.Security.Cryptography.SHA1Cng", OID_OIWSEC_SHA1);
                ht.Add("System.Security.Cryptography.SHA1Managed", OID_OIWSEC_SHA1);

                ht.Add("SHA256", OID_OIWSEC_SHA256);
                ht.Add("System.Security.Cryptography.SHA256", OID_OIWSEC_SHA256);
                ht.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", OID_OIWSEC_SHA256);
                ht.Add("System.Security.Cryptography.SHA256Cng", OID_OIWSEC_SHA256);
                ht.Add("System.Security.Cryptography.SHA256Managed", OID_OIWSEC_SHA256);

                ht.Add("SHA384", OID_OIWSEC_SHA384);
                ht.Add("System.Security.Cryptography.SHA384", OID_OIWSEC_SHA384);
                ht.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", OID_OIWSEC_SHA384);
                ht.Add("System.Security.Cryptography.SHA384Cng", OID_OIWSEC_SHA384);
                ht.Add("System.Security.Cryptography.SHA384Managed", OID_OIWSEC_SHA384);

                ht.Add("SHA512", OID_OIWSEC_SHA512);
                ht.Add("System.Security.Cryptography.SHA512", OID_OIWSEC_SHA512);
                ht.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", OID_OIWSEC_SHA512);
                ht.Add("System.Security.Cryptography.SHA512Cng", OID_OIWSEC_SHA512);
                ht.Add("System.Security.Cryptography.SHA512Managed", OID_OIWSEC_SHA512);

                ht.Add("RIPEMD160", OID_OIWSEC_RIPEMD160);
                ht.Add("System.Security.Cryptography.RIPEMD160", OID_OIWSEC_RIPEMD160);
                ht.Add("System.Security.Cryptography.RIPEMD160Managed", OID_OIWSEC_RIPEMD160);

                ht.Add("MD5", OID_RSA_MD5);
                ht.Add("System.Security.Cryptography.MD5", OID_RSA_MD5);
                ht.Add("System.Security.Cryptography.MD5CryptoServiceProvider", OID_RSA_MD5);
                ht.Add("System.Security.Cryptography.MD5Managed", OID_RSA_MD5);

                ht.Add("TripleDESKeyWrap", OID_RSA_SMIMEalgCMS3DESwrap);

                ht.Add("RC2", OID_RSA_RC2CBC);
                ht.Add("System.Security.Cryptography.RC2CryptoServiceProvider", OID_RSA_RC2CBC);

                ht.Add("DES", OID_OIWSEC_desCBC);
                ht.Add("System.Security.Cryptography.DESCryptoServiceProvider", OID_OIWSEC_desCBC);

                ht.Add("TripleDES", OID_RSA_DES_EDE3_CBC);
                ht.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", OID_RSA_DES_EDE3_CBC);

                Debug.Assert(ht.Count <= capacity); // if more entries are added in the future, increase initial capacity.
                s_defaultOidHT = ht;
                return s_defaultOidHT;
            }
        }

        private static Dictionary<string, object> DefaultNameHT
        {
            get
            {
                if (s_defaultNameHT != null)
                {
                    return s_defaultNameHT;
                }

                const int capacity = 89;

                Dictionary<string, object> ht = new Dictionary<string, object>(capacity: capacity, comparer: StringComparer.OrdinalIgnoreCase);

                Type HMACMD5Type = typeof(System.Security.Cryptography.HMACMD5);
                Type HMACSHA1Type = typeof(System.Security.Cryptography.HMACSHA1);
                Type HMACSHA256Type = typeof(System.Security.Cryptography.HMACSHA256);
                Type HMACSHA384Type = typeof(System.Security.Cryptography.HMACSHA384);
                Type HMACSHA512Type = typeof(System.Security.Cryptography.HMACSHA512);
#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
                Type RijndaelManagedType = typeof(System.Security.Cryptography.RijndaelManaged);
#pragma warning restore SYSLIB0022
#pragma warning disable SYSLIB0021 // Obsolete: derived cryptographic types
                Type AesManagedType = typeof(System.Security.Cryptography.AesManaged);
                Type SHA256DefaultType = typeof(System.Security.Cryptography.SHA256Managed);
                Type SHA384DefaultType = typeof(System.Security.Cryptography.SHA384Managed);
                Type SHA512DefaultType = typeof(System.Security.Cryptography.SHA512Managed);
#pragma warning restore SYSLIB0021

                string SHA1CryptoServiceProviderType = "System.Security.Cryptography.SHA1CryptoServiceProvider, " + AssemblyName_Csp;
                string MD5CryptoServiceProviderType = "System.Security.Cryptography.MD5CryptoServiceProvider," + AssemblyName_Csp;
                string RSACryptoServiceProviderType = "System.Security.Cryptography.RSACryptoServiceProvider, " + AssemblyName_Csp;
                string DSACryptoServiceProviderType = "System.Security.Cryptography.DSACryptoServiceProvider, " + AssemblyName_Csp;
                string DESCryptoServiceProviderType = "System.Security.Cryptography.DESCryptoServiceProvider, " + AssemblyName_Csp;
                string TripleDESCryptoServiceProviderType = "System.Security.Cryptography.TripleDESCryptoServiceProvider, " + AssemblyName_Csp;
                string RC2CryptoServiceProviderType = "System.Security.Cryptography.RC2CryptoServiceProvider, " + AssemblyName_Csp;
                string RNGCryptoServiceProviderType = "System.Security.Cryptography.RNGCryptoServiceProvider, " + AssemblyName_Csp;
                string AesCryptoServiceProviderType = "System.Security.Cryptography.AesCryptoServiceProvider, " + AssemblyName_Csp;

                string ECDsaCngType = "System.Security.Cryptography.ECDsaCng, " + AssemblyName_Cng;

                // Random number generator
                ht.Add("RandomNumberGenerator", RNGCryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.RandomNumberGenerator", RNGCryptoServiceProviderType);

                // Hash functions
                ht.Add("SHA", SHA1CryptoServiceProviderType);
                ht.Add("SHA1", SHA1CryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.SHA1", SHA1CryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.HashAlgorithm", SHA1CryptoServiceProviderType);

                ht.Add("MD5", MD5CryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.MD5", MD5CryptoServiceProviderType);

                ht.Add("SHA256", SHA256DefaultType);
                ht.Add("SHA-256", SHA256DefaultType);
                ht.Add("System.Security.Cryptography.SHA256", SHA256DefaultType);

                ht.Add("SHA384", SHA384DefaultType);
                ht.Add("SHA-384", SHA384DefaultType);
                ht.Add("System.Security.Cryptography.SHA384", SHA384DefaultType);

                ht.Add("SHA512", SHA512DefaultType);
                ht.Add("SHA-512", SHA512DefaultType);
                ht.Add("System.Security.Cryptography.SHA512", SHA512DefaultType);

                // Keyed Hash Algorithms
                ht.Add("System.Security.Cryptography.HMAC", HMACSHA1Type);
                ht.Add("System.Security.Cryptography.KeyedHashAlgorithm", HMACSHA1Type);
                ht.Add("HMACMD5", HMACMD5Type);
                ht.Add("System.Security.Cryptography.HMACMD5", HMACMD5Type);
                ht.Add("HMACSHA1", HMACSHA1Type);
                ht.Add("System.Security.Cryptography.HMACSHA1", HMACSHA1Type);
                ht.Add("HMACSHA256", HMACSHA256Type);
                ht.Add("System.Security.Cryptography.HMACSHA256", HMACSHA256Type);
                ht.Add("HMACSHA384", HMACSHA384Type);
                ht.Add("System.Security.Cryptography.HMACSHA384", HMACSHA384Type);
                ht.Add("HMACSHA512", HMACSHA512Type);
                ht.Add("System.Security.Cryptography.HMACSHA512", HMACSHA512Type);

                // Asymmetric algorithms
                ht.Add("RSA", RSACryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.RSA", RSACryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.AsymmetricAlgorithm", RSACryptoServiceProviderType);

                if (!OperatingSystem.IsIOS() &&
                    !OperatingSystem.IsTvOS())
                {
                    ht.Add("DSA", DSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.DSA", DSACryptoServiceProviderType);
                }

                // Windows will register the public ECDsaCng type.  Non-Windows gets a special handler.
                if (OperatingSystem.IsWindows())
                {
                    ht.Add(ECDsaIdentifier, ECDsaCngType);
                }

                ht.Add("ECDsaCng", ECDsaCngType);
                ht.Add("System.Security.Cryptography.ECDsaCng", ECDsaCngType);

                // Symmetric algorithms
                ht.Add("DES", DESCryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.DES", DESCryptoServiceProviderType);

                ht.Add("3DES", TripleDESCryptoServiceProviderType);
                ht.Add("TripleDES", TripleDESCryptoServiceProviderType);
                ht.Add("Triple DES", TripleDESCryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.TripleDES", TripleDESCryptoServiceProviderType);

                ht.Add("RC2", RC2CryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.RC2", RC2CryptoServiceProviderType);

                ht.Add("Rijndael", RijndaelManagedType);
                ht.Add("System.Security.Cryptography.Rijndael", RijndaelManagedType);
                // Rijndael is the default symmetric cipher because (a) it's the strongest and (b) we know we have an implementation everywhere
                ht.Add("System.Security.Cryptography.SymmetricAlgorithm", RijndaelManagedType);

                ht.Add("AES", AesCryptoServiceProviderType);
                ht.Add("AesCryptoServiceProvider", AesCryptoServiceProviderType);
                ht.Add("System.Security.Cryptography.AesCryptoServiceProvider", AesCryptoServiceProviderType);
                ht.Add("AesManaged", AesManagedType);
                ht.Add("System.Security.Cryptography.AesManaged", AesManagedType);

                // Xml Dsig/ Enc Hash algorithms
                ht.Add("http://www.w3.org/2000/09/xmldsig#sha1", SHA1CryptoServiceProviderType);
                // Add the other hash algorithms introduced with XML Encryption
                ht.Add("http://www.w3.org/2001/04/xmlenc#sha256", SHA256DefaultType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#sha512", SHA512DefaultType);

                // Xml Encryption symmetric keys
                ht.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", DESCryptoServiceProviderType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", TripleDESCryptoServiceProviderType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", TripleDESCryptoServiceProviderType);

                ht.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", RijndaelManagedType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", RijndaelManagedType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", RijndaelManagedType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", RijndaelManagedType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", RijndaelManagedType);
                ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", RijndaelManagedType);

                // Xml Dsig HMAC URIs from http://www.w3.org/TR/xmldsig-core/
                ht.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1", HMACSHA1Type);

                // Xml Dsig-more Uri's as defined in http://www.ietf.org/rfc/rfc4051.txt
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#md5", MD5CryptoServiceProviderType);
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", SHA384DefaultType);
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", HMACMD5Type);
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", HMACSHA256Type);
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", HMACSHA384Type);
                ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", HMACSHA512Type);
                // X509 Extensions (custom decoders)
                // Basic Constraints OID value
                ht.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyName_X509Certificates);
                ht.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyName_X509Certificates);
                // Subject Key Identifier OID value
                ht.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, " + AssemblyName_X509Certificates);
                // Key Usage OID value
                ht.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, " + AssemblyName_X509Certificates);
                // Enhanced Key Usage OID value
                ht.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, " + AssemblyName_X509Certificates);

                // X509Chain class can be overridden to use a different chain engine.
                ht.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, " + AssemblyName_X509Certificates);

                // PKCS9 attributes
                ht.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, " + AssemblyName_Pkcs);
                ht.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, " + AssemblyName_Pkcs);
                ht.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, " + AssemblyName_Pkcs);
                ht.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, " + AssemblyName_Pkcs);
                ht.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, " + AssemblyName_Pkcs);

                Debug.Assert(ht.Count <= capacity); // // if more entries are added in the future, increase initial capacity.

                s_defaultNameHT = ht;
                return s_defaultNameHT;

                // Types in .NET Framework but currently unsupported in .NET Core:
                // Type HMACRIPEMD160Type = typeof(System.Security.Cryptography.HMACRIPEMD160);
                // Type MAC3DESType = typeof(System.Security.Cryptography.MACTripleDES);
                // Type DSASignatureDescriptionType = typeof(System.Security.Cryptography.DSASignatureDescription);
                // Type RSAPKCS1SHA1SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA1SignatureDescription);
                // Type RSAPKCS1SHA256SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA256SignatureDescription);
                // Type RSAPKCS1SHA384SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA384SignatureDescription);
                // Type RSAPKCS1SHA512SignatureDescriptionType = typeof(System.Security.Cryptography.RSAPKCS1SHA512SignatureDescription);
                // string RIPEMD160ManagedType = "System.Security.Cryptography.RIPEMD160Managed" + AssemblyName_Encoding;
                // string ECDiffieHellmanCngType = "System.Security.Cryptography.ECDiffieHellmanCng, " + AssemblyName_Cng;
                // string MD5CngType = "System.Security.Cryptography.MD5Cng, " + AssemblyName_Cng;
                // string SHA1CngType = "System.Security.Cryptography.SHA1Cng, " + AssemblyName_Cng;
                // string SHA256CngType = "System.Security.Cryptography.SHA256Cng, " + AssemblyName_Cng;
                // string SHA384CngType = "System.Security.Cryptography.SHA384Cng, " + AssemblyName_Cng;
                // string SHA512CngType = "System.Security.Cryptography.SHA512Cng, " + AssemblyName_Cng;
                // string SHA256CryptoServiceProviderType = "System.Security.Cryptography.SHA256CryptoServiceProvider, " + AssemblyName_Csp;
                // string SHA384CryptoSerivceProviderType = "System.Security.Cryptography.SHA384CryptoServiceProvider, " + AssemblyName_Csp;
                // string SHA512CryptoServiceProviderType = "System.Security.Cryptography.SHA512CryptoServiceProvider, " + AssemblyName_Csp;
                // string DpapiDataProtectorType = "System.Security.Cryptography.DpapiDataProtector, " + AssemblyRef.SystemSecurity;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public static void AddAlgorithm(Type algorithm, params string[] names)
        {
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm));
            if (!algorithm.IsVisible)
                throw new ArgumentException(SR.Cryptography_AlgorithmTypesMustBeVisible, nameof(algorithm));
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            string[] algorithmNames = new string[names.Length];
            Array.Copy(names, algorithmNames, algorithmNames.Length);

            // Pre-check the algorithm names for validity so that we don't add a few of the names and then
            // throw an exception if we find an invalid name partway through the list.
            foreach (string name in algorithmNames)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(SR.Cryptography_AddNullOrEmptyName);
                }
            }

            // Everything looks valid, so we're safe to add the name mappings.
            foreach (string name in algorithmNames)
            {
                appNameHT[name] = algorithm;
            }
        }

        [RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
        public static object? CreateFromName(string name, params object?[]? args)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // Check to see if we have an application defined mapping
            appNameHT.TryGetValue(name, out Type? retvalType);

            // We allow the default table to Types and Strings
            // Types get used for types in .Algorithms assembly.
            // strings get used for delay-loaded stuff in other assemblies such as .Csp.
            if (retvalType == null && DefaultNameHT.TryGetValue(name, out object? retvalObj))
            {
                retvalType = retvalObj as Type;

                if (retvalType == null)
                {
                    if (retvalObj is string retvalString)
                    {
                        retvalType = Type.GetType(retvalString, false, false);
                        if (retvalType != null && !retvalType.IsVisible)
                        {
                            retvalType = null;
                        }

                        if (retvalType != null)
                        {
                            // Add entry to the appNameHT, which makes subsequent calls much faster.
                            appNameHT[name] = retvalType;
                        }
                    }
                    else
                    {
                        Debug.Fail("Unsupported Dictionary value:" + retvalObj.ToString());
                    }
                }
            }

            // Special case asking for "ECDsa" since the default map from .NET Framework uses
            // a Windows-only type.
            if (retvalType == null &&
                (args == null || args.Length == 1) &&
                name == ECDsaIdentifier)
            {
                return ECDsa.Create();
            }

            // Maybe they gave us a classname.
            if (retvalType == null)
            {
                retvalType = Type.GetType(name, false, false);
                if (retvalType != null && !retvalType.IsVisible)
                {
                    retvalType = null;
                }
            }

            // Still null? Then we didn't find it.
            if (retvalType == null)
            {
                return null;
            }

            // Locate all constructors.
            MethodBase[] cons = retvalType.GetConstructors(ConstructorDefault);
            if (cons == null)
            {
                return null;
            }

            if (args == null)
            {
                args = Array.Empty<object>();
            }

            List<MethodBase> candidates = new List<MethodBase>();
            for (int i = 0; i < cons.Length; i++)
            {
                MethodBase con = cons[i];
                if (con.GetParameters().Length == args.Length)
                {
                    candidates.Add(con);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            cons = candidates.ToArray();

            // Bind to matching ctor.
            ConstructorInfo? rci = Type.DefaultBinder.BindToMethod(
                ConstructorDefault,
                cons,
                ref args,
                null,
                null,
                null,
                out object? state) as ConstructorInfo;

            // Check for ctor we don't like (non-existent, delegate or decorated with declarative linktime demand).
            if (rci == null || typeof(Delegate).IsAssignableFrom(rci.DeclaringType))
            {
                return null;
            }

            // Ctor invoke and allocation.
            object retval = rci.Invoke(ConstructorDefault, Type.DefaultBinder, args, null);

            // Reset any parameter re-ordering performed by the binder.
            if (state != null)
            {
                Type.DefaultBinder.ReorderArgumentArray(ref args, state);
            }

            return retval;
        }

        [RequiresUnreferencedCode(CreateFromNameUnreferencedCodeMessage)]
        public static object? CreateFromName(string name)
        {
            return CreateFromName(name, null);
        }

        [UnsupportedOSPlatform("browser")]
        public static void AddOID(string oid, params string[] names)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            string[] oidNames = new string[names.Length];
            Array.Copy(names, oidNames, oidNames.Length);

            // Pre-check the input names for validity, so that we don't add a few of the names and throw an
            // exception if an invalid name is found further down the array.
            foreach (string name in oidNames)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(SR.Cryptography_AddNullOrEmptyName);
                }
            }

            // Everything is valid, so we're good to lock the hash table and add the application mappings
            foreach (string name in oidNames)
            {
                appOidHT[name] = oid;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public static string? MapNameToOID(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            appOidHT.TryGetValue(name, out string? oidName);

            if (string.IsNullOrEmpty(oidName) && !DefaultOidHT.TryGetValue(name, out oidName))
            {
                try
                {
                    Oid oid = Oid.FromFriendlyName(name, OidGroup.All);
                    oidName = oid.Value;
                }
                catch (CryptographicException) { }
            }

            return oidName;
        }

        [UnsupportedOSPlatform("browser")]
        [Obsolete(Obsoletions.CryptoConfigEncodeOIDMessage, DiagnosticId = Obsoletions.CryptoConfigEncodeOIDDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static byte[] EncodeOID(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            string[] oidString = str.Split('.'); // valid ASN.1 separator
            uint[] oidNums = new uint[oidString.Length];
            for (int i = 0; i < oidString.Length; i++)
            {
                oidNums[i] = unchecked((uint)int.Parse(oidString[i], CultureInfo.InvariantCulture));
            }

            // Handle the first two oidNums special
            if (oidNums.Length < 2)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_InvalidOID);

            uint firstTwoOidNums = unchecked((oidNums[0] * 40) + oidNums[1]);

            // Determine length of output array
            int encodedOidNumsLength = 2; // Reserve first two bytes for later
            EncodeSingleOidNum(firstTwoOidNums, null, ref encodedOidNumsLength);
            for (int i = 2; i < oidNums.Length; i++)
            {
                EncodeSingleOidNum(oidNums[i], null, ref encodedOidNumsLength);
            }

            // Allocate the array to receive encoded oidNums
            byte[] encodedOidNums = new byte[encodedOidNumsLength];
            int encodedOidNumsIndex = 2;

            // Encode each segment
            EncodeSingleOidNum(firstTwoOidNums, encodedOidNums, ref encodedOidNumsIndex);
            for (int i = 2; i < oidNums.Length; i++)
            {
                EncodeSingleOidNum(oidNums[i], encodedOidNums, ref encodedOidNumsIndex);
            }

            Debug.Assert(encodedOidNumsIndex == encodedOidNumsLength);

            // Final return value is 06 <length> encodedOidNums[]
            if (encodedOidNumsIndex - 2 > 0x7f)
                throw new CryptographicUnexpectedOperationException(SR.Cryptography_Config_EncodedOIDError);

            encodedOidNums[0] = (byte)0x06;
            encodedOidNums[1] = (byte)(encodedOidNumsIndex - 2);

            return encodedOidNums;
        }

        private static void EncodeSingleOidNum(uint value, byte[]? destination, ref int index)
        {
            // Write directly to destination starting at index, and update index based on how many bytes written.
            // If destination is null, just return updated index.
            if (unchecked((int)value) < 0x80)
            {
                if (destination != null)
                {
                    destination[index++] = unchecked((byte)value);
                }
                else
                {
                    index += 1;
                }
            }
            else if (value < 0x4000)
            {
                if (destination != null)
                {
                    destination[index++] = (byte)((value >> 7) | 0x80);
                    destination[index++] = (byte)(value & 0x7f);
                }
                else
                {
                    index += 2;
                }
            }
            else if (value < 0x200000)
            {
                if (destination != null)
                {
                    unchecked
                    {
                        destination[index++] = (byte)((value >> 14) | 0x80);
                        destination[index++] = (byte)((value >> 7) | 0x80);
                        destination[index++] = (byte)(value & 0x7f);
                    }
                }
                else
                {
                    index += 3;
                }
            }
            else if (value < 0x10000000)
            {
                if (destination != null)
                {
                    unchecked
                    {
                        destination[index++] = (byte)((value >> 21) | 0x80);
                        destination[index++] = (byte)((value >> 14) | 0x80);
                        destination[index++] = (byte)((value >> 7) | 0x80);
                        destination[index++] = (byte)(value & 0x7f);
                    }
                }
                else
                {
                    index += 4;
                }
            }
            else
            {
                if (destination != null)
                {
                    unchecked
                    {
                        destination[index++] = (byte)((value >> 28) | 0x80);
                        destination[index++] = (byte)((value >> 21) | 0x80);
                        destination[index++] = (byte)((value >> 14) | 0x80);
                        destination[index++] = (byte)((value >> 7) | 0x80);
                        destination[index++] = (byte)(value & 0x7f);
                    }
                }
                else
                {
                    index += 5;
                }
            }
        }
    }
}
