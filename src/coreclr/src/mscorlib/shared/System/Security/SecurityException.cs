// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Serialization;

namespace System.Security
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SecurityException : SystemException
    {
        private const string DemandedName = "Demanded";
        private const string GrantedSetName = "GrantedSet";
        private const string RefusedSetName = "RefusedSet";
        private const string DeniedName = "Denied";
        private const string PermitOnlyName = "PermitOnly";
        private const string UrlName = "Url";

        public SecurityException()
            : base(SR.Arg_SecurityException)
        {
            HResult = HResults.COR_E_SECURITY;
        }

        public SecurityException(string message)
            : base(message)
        {
            HResult = HResults.COR_E_SECURITY;
        }

        public SecurityException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_SECURITY;
        }

        public SecurityException(string message, Type type)
            : base(message)
        {
            HResult = HResults.COR_E_SECURITY;
            PermissionType = type;
        }

        public SecurityException(string message, Type type, string state)
            : base(message)
        {
            HResult = HResults.COR_E_SECURITY;
            PermissionType = type;
            PermissionState = state;
        }

        protected SecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Demanded = (string)info.GetValueNoThrow(DemandedName, typeof(string));
            GrantedSet = (string)info.GetValueNoThrow(GrantedSetName, typeof(string));
            RefusedSet = (string)info.GetValueNoThrow(RefusedSetName, typeof(string));
            DenySetInstance = (string)info.GetValueNoThrow(DeniedName, typeof(string));
            PermitOnlySetInstance = (string)info.GetValueNoThrow(PermitOnlyName, typeof(string));
            Url = (string)info.GetValueNoThrow(UrlName, typeof(string));
        }

        public override string ToString() => base.ToString();

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context); 
            info.AddValue(DemandedName, Demanded, typeof(string));
            info.AddValue(GrantedSetName, GrantedSet, typeof(string));
            info.AddValue(RefusedSetName, RefusedSet, typeof(string));
            info.AddValue(DeniedName, DenySetInstance, typeof(string));
            info.AddValue(PermitOnlyName, PermitOnlySetInstance, typeof(string));
            info.AddValue(UrlName, Url, typeof(string));
        }

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
