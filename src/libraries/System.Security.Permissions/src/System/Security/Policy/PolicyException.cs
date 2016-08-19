// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public partial class PolicyException : System.SystemException
    {
        public PolicyException() { }
        protected PolicyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public PolicyException(string message) { }
        public PolicyException(string message, System.Exception exception) { }
    }
}
