// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Security.Policy
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class PolicyException : System.SystemException
    {
        public PolicyException() { }
        protected PolicyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public PolicyException(string message) : base(message) { }
        public PolicyException(string message, Exception exception) : base(message, exception) { }
    }
}
