// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace Test.Cryptography
{
    public class HashInfo
    {
        private const string Md5Oid = "1.2.840.113549.2.5";
        private const string Sha1Oid = "1.3.14.3.2.26";

        private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
        private const string Sha384Oid = "2.16.840.1.101.3.4.2.2";
        private const string Sha512Oid = "2.16.840.1.101.3.4.2.3";

        private const string Sha3_256Oid = "2.16.840.1.101.3.4.2.8";
        private const string Sha3_384Oid = "2.16.840.1.101.3.4.2.9";
        private const string Sha3_512Oid = "2.16.840.1.101.3.4.2.10";

        private const string Shake128Oid = "2.16.840.1.101.3.4.2.11";
        private const string Shake256Oid = "2.16.840.1.101.3.4.2.12";

        public static readonly HashInfo Md5 = new HashInfo(Md5Oid, 128 / 8, HashAlgorithmName.MD5);
        public static readonly HashInfo Sha1 = new HashInfo(Sha1Oid, 160 / 8, HashAlgorithmName.SHA1);
        public static readonly HashInfo Sha256 = new HashInfo(Sha256Oid, 256 / 8, HashAlgorithmName.SHA256);
        public static readonly HashInfo Sha384 = new HashInfo(Sha384Oid, 384 / 8, HashAlgorithmName.SHA384);
        public static readonly HashInfo Sha512 = new HashInfo(Sha512Oid, 512 / 8, HashAlgorithmName.SHA512);

        private static readonly HashAlgorithmName HashAlgSHAKE128 = new HashAlgorithmName("SHAKE128");
        private static readonly HashAlgorithmName HashAlgSHAKE256 = new HashAlgorithmName("SHAKE256");
#if NET
        private static readonly HashAlgorithmName HashAlgSHA3_256 = HashAlgorithmName.SHA3_256;
        private static readonly HashAlgorithmName HashAlgSHA3_384 = HashAlgorithmName.SHA3_384;
        private static readonly HashAlgorithmName HashAlgSHA3_512 = HashAlgorithmName.SHA3_512;
#else
        private static readonly HashAlgorithmName HashAlgSHA3_256 = new HashAlgorithmName("SHA3-256");
        private static readonly HashAlgorithmName HashAlgSHA3_384 = new HashAlgorithmName("SHA3-384");
        private static readonly HashAlgorithmName HashAlgSHA3_512 = new HashAlgorithmName("SHA3-512");
#endif

        public static readonly HashInfo Sha3_256 = new HashInfo(Sha3_256Oid, 256 / 8, HashAlgSHA3_256);
        public static readonly HashInfo Sha3_384 = new HashInfo(Sha3_384Oid, 384 / 8, HashAlgSHA3_384);
        public static readonly HashInfo Sha3_512 = new HashInfo(Sha3_512Oid, 512 / 8, HashAlgSHA3_512);
        public static readonly HashInfo Shake128 = new HashInfo(Shake128Oid, 256 / 8, HashAlgSHAKE128);
        public static readonly HashInfo Shake256 = new HashInfo(Shake256Oid, 512 / 8, HashAlgSHAKE256);

        public string Oid { get; }
        public HashAlgorithmName Name { get; }
        public int OutputSize { get; }

        public byte[] GetHash(byte[] data)
        {
#if NET
            if (Oid == Shake128Oid)
            {
                return System.Security.Cryptography.Shake128.HashData(data, OutputSize);
            }
            else if (Oid == Shake256Oid)
            {
                return System.Security.Cryptography.Shake256.HashData(data, OutputSize);
            }
            else
#endif
            {
                using (IncrementalHash hasher = IncrementalHash.CreateHash(Name))
                {
                    hasher.AppendData(data);
                    return hasher.GetHashAndReset();
                }
            }
        }

        public static byte[] HashData(string hashAlgOid, byte[] data)
        {
            HashInfo hashInfo = AllHashInfos().FirstOrDefault(h => h.Oid == hashAlgOid);
            if (hashInfo == null)
            {
                throw new ArgumentException($"Unknown hash algorithm OID: {hashAlgOid}", nameof(hashAlgOid));
            }

            return hashInfo.GetHash(data);
        }

        public static IEnumerable<HashInfo> AllHashInfos()
        {
            yield return Sha256;
            yield return Sha384;
            yield return Sha512;
#if NET
            yield return Sha3_256;
            yield return Sha3_384;
            yield return Sha3_512;
            yield return Shake128;
            yield return Shake256;
#endif
        }

        internal static HashSet<string> KnownHashAlgorithmOids => field ??= AllHashInfos().Select(h => h.Oid).ToHashSet();

        private HashInfo(string oid, int outputSize, HashAlgorithmName name)
        {
            Oid = oid;
            OutputSize = outputSize;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name.Name}";
        }
    }
}
