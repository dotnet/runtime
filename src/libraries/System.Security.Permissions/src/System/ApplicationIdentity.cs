// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Security;

namespace System
{
    public sealed class ApplicationIdentity : ISerializable
    {
        private ApplicationIdentity() { }
        public ApplicationIdentity(string applicationIdentityFullName) { }
        public string FullName { get { return null; } }
        public string CodeBase { get { return null; } }
        public override string ToString() { return null; }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
