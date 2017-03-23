using System.Runtime.Serialization;
using System.Security;

namespace System
{
    [Serializable]
    public sealed class ApplicationIdentity : ISerializable
    {
        private ApplicationIdentity() { }
        public ApplicationIdentity(string applicationIdentityFullName) { }
        private ApplicationIdentity(SerializationInfo info, StreamingContext context) { }
        public string FullName { get { return null; } }
        public string CodeBase { get { return null; } }
        public override string ToString() { return null; }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
}
