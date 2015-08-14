// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public abstract partial class HashAlgorithm : System.IDisposable
    {
        protected HashAlgorithm() { }
        public virtual int HashSize { get { return default(int); } }
        public byte[] ComputeHash(byte[] buffer) { return default(byte[]); }
        public byte[] ComputeHash(byte[] buffer, int offset, int count) { return default(byte[]); }
        public byte[] ComputeHash(System.IO.Stream inputStream) { return default(byte[]); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
        public abstract void Initialize();
    }
    public abstract partial class HMAC : System.Security.Cryptography.KeyedHashAlgorithm
    {
        protected HMAC() { }
        public string HashName { get { return default(string); } set { } }
        public override byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
        protected override void HashCore(byte[] rgb, int ib, int cb) { }
        protected override byte[] HashFinal() { return default(byte[]); }
        public override void Initialize() { }
    }
    public abstract partial class KeyedHashAlgorithm : System.Security.Cryptography.HashAlgorithm
    {
        protected KeyedHashAlgorithm() { }
        public virtual byte[] Key { get { return default(byte[]); } set { } }
        protected override void Dispose(bool disposing) { }
    }
}
