// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
** 
**
**
** Purpose: Exception class for security
**
**
=============================================================================*/

using System.Security;
using System;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text;
using System.Security.Policy;
using System.IO;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Security
{
    [Serializable]
    public class SecurityException : SystemException
    {
        internal static string GetResString(string sResourceName)
        {
            return Environment.GetResourceString(sResourceName);
        }

#pragma warning disable 618
        internal static Exception MakeSecurityException(AssemblyName asmName, Evidence asmEvidence, RuntimeMethodHandleInternal rmh, Object demand)
#pragma warning restore 618
        {
            return new SecurityException(GetResString("Arg_SecurityException"));
        }

        public SecurityException()
            : base(GetResString("Arg_SecurityException"))
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        public SecurityException(String message)
            : base(message)
        {
            // This is the constructor that gets called if you Assert but don't have permission to Assert.  (So don't assert in here.)
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        public SecurityException(String message, Exception inner)
            : base(message, inner)
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        protected SecurityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();
        }

        public override String ToString()
        {
            return base.ToString();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            base.GetObjectData(info, context);
        }

        // Stubs for surface area compatibility only
        public SecurityException(String message, Type type)
            : base(message)
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
            PermissionType = type;
        }

        public SecurityException(string message, System.Type type, string state)
            : base(message)
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
            PermissionType = type;
            PermissionState = state;
        }

        public object Demanded { get; set; }
        public object DenySetInstance { get; set; }
        public System.Reflection.AssemblyName FailedAssemblyInfo { get; set; }
        public string GrantedSet { get; set; }
        public System.Reflection.MethodInfo Method { get; set; }
        public string PermissionState { get; set; }
        public System.Type PermissionType { get; set; }
        public object PermitOnlySetInstance { get; set; }
        public string RefusedSet { get; set; }
        public string Url { get; set; }
    }
}
