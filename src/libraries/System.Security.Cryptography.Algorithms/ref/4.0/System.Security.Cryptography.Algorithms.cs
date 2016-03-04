// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public abstract partial class Aes : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected Aes() { }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public static System.Security.Cryptography.Aes Create() { return default(System.Security.Cryptography.Aes); }
    }
    public abstract partial class DeriveBytes : System.IDisposable
    {
        protected DeriveBytes() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract byte[] GetBytes(int cb);
        public abstract void Reset();
    }
    public partial class HMACMD5 : System.Security.Cryptography.HMAC
    {
        public HMACMD5() { }
        public HMACMD5(byte[] key) { }
        public override int HashSize { get { return default(int); } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public partial class HMACSHA1 : System.Security.Cryptography.HMAC
    {
        public HMACSHA1() { }
        public HMACSHA1(byte[] key) { }
        public override int HashSize { get { return default(int); } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public partial class HMACSHA256 : System.Security.Cryptography.HMAC
    {
        public HMACSHA256() { }
        public HMACSHA256(byte[] key) { }
        public override int HashSize { get { return default(int); } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public partial class HMACSHA384 : System.Security.Cryptography.HMAC
    {
        public HMACSHA384() { }
        public HMACSHA384(byte[] key) { }
        public override int HashSize { get { return default(int); } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public partial class HMACSHA512 : System.Security.Cryptography.HMAC
    {
        public HMACSHA512() { }
        public HMACSHA512(byte[] key) { }
        public override int HashSize { get { return default(int); } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public sealed partial class IncrementalHash : System.IDisposable
    {
        internal IncrementalHash() { }
        public System.Security.Cryptography.HashAlgorithmName AlgorithmName { get { return default(System.Security.Cryptography.HashAlgorithmName); } }
        public void AppendData(byte[] data) { }
        public void AppendData(byte[] data, int offset, int count) { }
        public static System.Security.Cryptography.IncrementalHash CreateHash(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(System.Security.Cryptography.IncrementalHash); }
        public static System.Security.Cryptography.IncrementalHash CreateHMAC(System.Security.Cryptography.HashAlgorithmName hashAlgorithm, byte[] key) { return default(System.Security.Cryptography.IncrementalHash); }
        public void Dispose() { }
        public byte[] GetHashAndReset() { return default(byte[]); }
    }
    public abstract partial class MD5 : System.Security.Cryptography.HashAlgorithm
    {
        protected MD5() { }
        public static System.Security.Cryptography.MD5 Create() { return default(System.Security.Cryptography.MD5); }
    }
    public abstract partial class RandomNumberGenerator : System.IDisposable
    {
        protected RandomNumberGenerator() { }
        public static System.Security.Cryptography.RandomNumberGenerator Create() { return default(System.Security.Cryptography.RandomNumberGenerator); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void GetBytes(byte[] data);
    }
    public partial class Rfc2898DeriveBytes : System.Security.Cryptography.DeriveBytes
    {
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(string password, byte[] salt) { }
        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations) { }
        public Rfc2898DeriveBytes(string password, int saltSize) { }
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations) { }
        public int IterationCount { get { return default(int); } set { } }
        public byte[] Salt { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        public override byte[] GetBytes(int cb) { return default(byte[]); }
        public override void Reset() { }
    }
    public abstract partial class RSA : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected RSA() { }
        public static RSA Create() { return default(RSA); }
        public abstract byte[] Decrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding);
        public abstract byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding);
        public abstract System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters);
        protected abstract byte[] HashData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
        protected abstract byte[] HashData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm);
        public abstract void ImportParameters(System.Security.Cryptography.RSAParameters parameters);
        public virtual byte[] SignData(byte[] data, int offset, int count, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public byte[] SignData(byte[] data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public virtual byte[] SignData(System.IO.Stream data, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(byte[]); }
        public abstract byte[] SignHash(byte[] hash, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding);
        public bool VerifyData(byte[] data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
        public bool VerifyData(System.IO.Stream data, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding) { return default(bool); }
        public abstract bool VerifyHash(byte[] hash, byte[] signature, System.Security.Cryptography.HashAlgorithmName hashAlgorithm, System.Security.Cryptography.RSASignaturePadding padding);
    }
    public sealed partial class RSAEncryptionPadding : System.IEquatable<System.Security.Cryptography.RSAEncryptionPadding>
    {
        internal RSAEncryptionPadding() { }
        public System.Security.Cryptography.RSAEncryptionPaddingMode Mode { get { return default(System.Security.Cryptography.RSAEncryptionPaddingMode); } }
        public System.Security.Cryptography.HashAlgorithmName OaepHashAlgorithm { get { return default(System.Security.Cryptography.HashAlgorithmName); } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA1 { get { return default(System.Security.Cryptography.RSAEncryptionPadding); } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA256 { get { return default(System.Security.Cryptography.RSAEncryptionPadding); } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA384 { get { return default(System.Security.Cryptography.RSAEncryptionPadding); } }
        public static System.Security.Cryptography.RSAEncryptionPadding OaepSHA512 { get { return default(System.Security.Cryptography.RSAEncryptionPadding); } }
        public static System.Security.Cryptography.RSAEncryptionPadding Pkcs1 { get { return default(System.Security.Cryptography.RSAEncryptionPadding); } }
        public static System.Security.Cryptography.RSAEncryptionPadding CreateOaep(System.Security.Cryptography.HashAlgorithmName hashAlgorithm) { return default(System.Security.Cryptography.RSAEncryptionPadding); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.RSAEncryptionPadding other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.RSAEncryptionPadding left, System.Security.Cryptography.RSAEncryptionPadding right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.RSAEncryptionPadding left, System.Security.Cryptography.RSAEncryptionPadding right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public enum RSAEncryptionPaddingMode
    {
        Oaep = 1,
        Pkcs1 = 0,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RSAParameters
    {
        public byte[] D;
        public byte[] DP;
        public byte[] DQ;
        public byte[] Exponent;
        public byte[] InverseQ;
        public byte[] Modulus;
        public byte[] P;
        public byte[] Q;
    }
    public sealed partial class RSASignaturePadding : System.IEquatable<System.Security.Cryptography.RSASignaturePadding>
    {
        internal RSASignaturePadding() { }
        public System.Security.Cryptography.RSASignaturePaddingMode Mode { get { return default(System.Security.Cryptography.RSASignaturePaddingMode); } }
        public static System.Security.Cryptography.RSASignaturePadding Pkcs1 { get { return default(System.Security.Cryptography.RSASignaturePadding); } }
        public static System.Security.Cryptography.RSASignaturePadding Pss { get { return default(System.Security.Cryptography.RSASignaturePadding); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Security.Cryptography.RSASignaturePadding other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Security.Cryptography.RSASignaturePadding left, System.Security.Cryptography.RSASignaturePadding right) { return default(bool); }
        public static bool operator !=(System.Security.Cryptography.RSASignaturePadding left, System.Security.Cryptography.RSASignaturePadding right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public enum RSASignaturePaddingMode
    {
        Pkcs1 = 0,
        Pss = 1,
    }
    public abstract partial class SHA1 : System.Security.Cryptography.HashAlgorithm
    {
        protected SHA1() { }
        public static System.Security.Cryptography.SHA1 Create() { return default(System.Security.Cryptography.SHA1); }
    }
    public abstract partial class SHA256 : System.Security.Cryptography.HashAlgorithm
    {
        protected SHA256() { }
        public static System.Security.Cryptography.SHA256 Create() { return default(System.Security.Cryptography.SHA256); }
    }
    public abstract partial class SHA384 : System.Security.Cryptography.HashAlgorithm
    {
        protected SHA384() { }
        public static System.Security.Cryptography.SHA384 Create() { return default(System.Security.Cryptography.SHA384); }
    }
    public abstract partial class SHA512 : System.Security.Cryptography.HashAlgorithm
    {
        protected SHA512() { }
        public static System.Security.Cryptography.SHA512 Create() { return default(System.Security.Cryptography.SHA512); }
    }
    public abstract partial class TripleDES : System.Security.Cryptography.SymmetricAlgorithm
    {
        protected TripleDES() { }
        public override byte[] Key { get { return default(byte[]); } set { } }
        public override System.Security.Cryptography.KeySizes[] LegalBlockSizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public override System.Security.Cryptography.KeySizes[] LegalKeySizes { get { return default(System.Security.Cryptography.KeySizes[]); } }
        public static System.Security.Cryptography.TripleDES Create() { return default(System.Security.Cryptography.TripleDES); }
        public static bool IsWeakKey(byte[] rgbKey) { return default(bool); }
    }
}
