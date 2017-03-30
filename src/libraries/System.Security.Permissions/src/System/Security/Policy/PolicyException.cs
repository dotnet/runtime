// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Security.Policy
{
    [Serializable]
    public partial class PolicyException : System.SystemException
    {
        public PolicyException() { }
        protected PolicyException(SerializationInfo info, StreamingContext context) { }
        public PolicyException(string message) { }
        public PolicyException(string message, Exception exception) { }
    }
}
