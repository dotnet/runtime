// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Serialization;

namespace System.Security
{
    public class SecurityException : SystemException
    {
        public SecurityException()
            : base(SR.Arg_SecurityException)
        {
            HResult = __HResults.COR_E_SECURITY;
        }

        public SecurityException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_SECURITY;
        }

        public SecurityException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_SECURITY;
        }

        public SecurityException(string message, Type type)
            : base(message)
        {
            HResult = __HResults.COR_E_SECURITY;
            PermissionType = type;
        }

        public SecurityException(string message, Type type, string state)
            : base(message)
        {
            HResult = __HResults.COR_E_SECURITY;
            PermissionType = type;
            PermissionState = state;
        }

        protected SecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public override string ToString() => base.ToString();

        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);

        public object Demanded { get; set; }
        public object DenySetInstance { get; set; }
        public AssemblyName FailedAssemblyInfo { get; set; }
        public string GrantedSet { get; set; }
        public MethodInfo Method { get; set; }
        public string PermissionState { get; set; }
        public Type PermissionType { get; set; }
        public object PermitOnlySetInstance { get; set; }
        public string RefusedSet { get; set; }
        public string Url { get; set; }
    }
}
