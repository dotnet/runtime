// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace System.Security.Policy
{
    public sealed partial class HashMembershipCondition : IDeserializationCallback, ISerializable, ISecurityEncodable, ISecurityPolicyEncodable, IMembershipCondition
    {
        public HashMembershipCondition(HashAlgorithm hashAlg, byte[] value) { }
        public HashAlgorithm HashAlgorithm { get; set; }
        public byte[] HashValue { get; set; }
        public bool Check(Evidence evidence) { return false; }
        public IMembershipCondition Copy() { return this; }
        public override bool Equals(object o) => base.Equals(o);
        public void FromXml(SecurityElement e) { }
        public void FromXml(SecurityElement e, PolicyLevel level) { }
        public override int GetHashCode() => base.GetHashCode();
        void IDeserializationCallback.OnDeserialization(object sender) { }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}
