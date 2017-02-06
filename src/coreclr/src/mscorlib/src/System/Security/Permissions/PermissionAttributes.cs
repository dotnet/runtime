// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{

    using System.Security.Util;
    using System.IO;
    using System.Security.Policy;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    // The csharp compiler requires these types to be public, but they are not used elsewhere.
    [Obsolete("SecurityAction is no longer accessible to application code.")]
    internal enum SecurityAction
    {
        // Demand permission of all caller
        Demand = 2,

        // Assert permission so callers don't need
        Assert = 3,

        // Deny permissions so checks will fail
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        Deny = 4,

        // Reduce permissions so check will fail
        PermitOnly = 5,

        // Demand permission of caller
        LinkDemand = 6,
    
        // Demand permission of a subclass
        InheritanceDemand = 7,

        // Request minimum permissions to run
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestMinimum = 8,

        // Request optional additional permissions
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestOptional = 9,

        // Refuse to be granted these permissions
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestRefuse = 10,
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    // The csharp compiler requires these types to be public, but they are not used elsewhere.
    [Obsolete("SecurityAttribute is no longer accessible to application code.")]
    internal abstract class SecurityAttribute : System.Attribute
    {
        /// <internalonly/>
        internal SecurityAction m_action;
        /// <internalonly/>
        internal bool m_unrestricted;

        protected SecurityAttribute( SecurityAction action ) 
        {
            m_action = action;
        }

        abstract public IPermission CreatePermission();
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    [Obsolete("CodeAccessSecurityAttribute is no longer accessible to application code.")]
    internal abstract class CodeAccessSecurityAttribute : SecurityAttribute
    {
        protected CodeAccessSecurityAttribute( SecurityAction action )
            : base( action )
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed internal class ReflectionPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private ReflectionPermissionFlag m_flag = ReflectionPermissionFlag.NoFlags;

        public ReflectionPermissionFlag Flags {
            get { return m_flag; }
            set { m_flag = value; }
        }

#pragma warning disable 618
        public ReflectionPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new ReflectionPermission( PermissionState.Unrestricted );
            }
            else
            {
                return new ReflectionPermission( m_flag );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed internal class PermissionSetAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_file;
        private String m_name;
        private bool m_unicode;
        private String m_xml;
        private String m_hex;

#pragma warning disable 618
        public PermissionSetAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
            m_unicode = false;
        }

        public override IPermission CreatePermission()
        {
            return null;
        }

        public PermissionSet CreatePermissionSet()
        {
            if (m_unrestricted)
                return new PermissionSet( PermissionState.Unrestricted );
            else if (m_name != null)
                return NamedPermissionSet.GetBuiltInSet( m_name );
            else
                return new PermissionSet( PermissionState.None );
        }
    }
}
