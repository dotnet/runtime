// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public partial class HMACSHA1 : System.Security.Cryptography.HMAC
    {
        public HMACSHA1() { }
        public HMACSHA1(byte[] key) { }
    }
    public partial class HMACSHA256 : System.Security.Cryptography.HMAC
    {
        public HMACSHA256() { }
        public HMACSHA256(byte[] key) { }
    }
    public partial class HMACSHA384 : System.Security.Cryptography.HMAC
    {
        public HMACSHA384() { }
        public HMACSHA384(byte[] key) { }
    }
    public partial class HMACSHA512 : System.Security.Cryptography.HMAC
    {
        public HMACSHA512() { }
        public HMACSHA512(byte[] key) { }
    }
    public abstract partial class MD5 : System.Security.Cryptography.HashAlgorithm
    {
        protected MD5() { }
        public static System.Security.Cryptography.MD5 Create() { return default(System.Security.Cryptography.MD5); }
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
}
