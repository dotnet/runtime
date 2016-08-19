// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class Hash : System.Security.Policy.EvidenceBase, System.Runtime.Serialization.ISerializable
    {
        public Hash(System.Reflection.Assembly assembly) { }
        public byte[] MD5 { get { return null; } }
        public byte[] SHA1 { get { return null; } }
        public static System.Security.Policy.Hash CreateMD5(byte[] md5) { return default(System.Security.Policy.Hash); }
        public static System.Security.Policy.Hash CreateSHA1(byte[] sha1) { return default(System.Security.Policy.Hash); }
        public byte[] GenerateHash(System.Security.Cryptography.HashAlgorithm hashAlg) { return null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() => base.ToString();
    }
}
