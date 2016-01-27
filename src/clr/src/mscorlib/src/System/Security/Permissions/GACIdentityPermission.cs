// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
#if FEATURE_CAS_POLICY
    using SecurityElement = System.Security.SecurityElement;
#endif // FEATURE_CAS_POLICY
    using System.Globalization;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class GacIdentityPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
#pragma warning disable 618
        public GacIdentityPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public override IPermission CreatePermission()
        {
            return new GacIdentityPermission();
        }
    }


    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed public class GacIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------

        public GacIdentityPermission(PermissionState state)
        {
            if (state != PermissionState.Unrestricted && state != PermissionState.None)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public GacIdentityPermission()
        {
        }

        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------


        public override IPermission Copy()
        {
            return new GacIdentityPermission();
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return false;
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return true;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return this.Copy();
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return this.Copy();
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return this.Copy();
        }

#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml()
        {
            SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.GacIdentityPermission" );
            return securityElement;
        }

        public override void FromXml(SecurityElement securityElement)
        {
            CodeAccessPermission.ValidateElement(securityElement, this);
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return GacIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.GacIdentityPermissionIndex;
        }
    }
}
