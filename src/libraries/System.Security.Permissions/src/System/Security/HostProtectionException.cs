// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    public partial class HostProtectionException : System.SystemException
    {
        public HostProtectionException() { }
        protected HostProtectionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public HostProtectionException(string message) { }
        public HostProtectionException(string message, System.Exception e) { }
        public HostProtectionException(string message, System.Security.Permissions.HostProtectionResource protectedResources, System.Security.Permissions.HostProtectionResource demandedResources) { }
        public System.Security.Permissions.HostProtectionResource DemandedResources { get { return default(System.Security.Permissions.HostProtectionResource); } }
        public System.Security.Permissions.HostProtectionResource ProtectedResources { get { return default(System.Security.Permissions.HostProtectionResource); } }
        public override string ToString() => base.ToString();
    }
}
