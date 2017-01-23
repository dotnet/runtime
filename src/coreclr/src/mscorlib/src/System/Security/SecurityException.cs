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

namespace System.Security
{
    using System.Security;
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Text;
    using System.Security.Policy;
    using System.IO;
    using System.Globalization;
    using System.Security.Util;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class SecurityException : SystemException
    {
        internal static string GetResString(string sResourceName)
        {
            PermissionSet.s_fullTrust.Assert();
            return Environment.GetResourceString(sResourceName);
        }

#pragma warning disable 618
        internal static Exception MakeSecurityException(AssemblyName asmName, Evidence asmEvidence, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
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

        internal SecurityException(PermissionSet grantedSetObj, PermissionSet refusedSetObj)
            : this(){}
#pragma warning disable 618
        internal SecurityException(string message, AssemblyName assemblyName, PermissionSet grant, PermissionSet refused, MethodInfo method, SecurityAction action, Object demanded, IPermission permThatFailed, Evidence evidence)
#pragma warning restore 618
                    : this(){}

        internal SecurityException(string message, Object deny, Object permitOnly, MethodInfo method, Object demanded, IPermission permThatFailed)
                    : this(){}

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

        private bool CanAccessSensitiveInfo()
        {
            bool retVal = false;
            try
            {
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy).Demand();
#pragma warning restore 618
                retVal = true;
            }
            catch (SecurityException)
            {
            }
            return retVal;
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
