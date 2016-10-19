// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Security
{
    [Serializable]
    public partial class HostProtectionException : System.SystemException
    {
        public HostProtectionException() { }
        protected HostProtectionException(SerializationInfo info, StreamingContext context) { }
        public HostProtectionException(string message) { }
        public HostProtectionException(string message, Exception e) { }
        public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources) { }
        public HostProtectionResource DemandedResources { get { return default(HostProtectionResource); } }
        public HostProtectionResource ProtectedResources { get { return default(HostProtectionResource); } }
        public override string ToString() => base.ToString();
    }
}
