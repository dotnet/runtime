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
    public enum SecurityAction
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
    public abstract class SecurityAttribute : System.Attribute
    {
        /// <internalonly/>
        internal SecurityAction m_action;
        /// <internalonly/>
        internal bool m_unrestricted;

        protected SecurityAttribute( SecurityAction action ) 
        {
            m_action = action;
        }

        public SecurityAction Action
        {
            get { return m_action; }
            set { m_action = value; }
        }

        public bool Unrestricted
        {
            get { return m_unrestricted; }
            set { m_unrestricted = value; }
        }

        abstract public IPermission CreatePermission();

        internal static unsafe IntPtr FindSecurityAttributeTypeHandle(String typeName)
        {
            PermissionSet.s_fullTrust.Assert();
            Type t = Type.GetType(typeName, false, false);
            if(t == null)
                return IntPtr.Zero;
            IntPtr typeHandle = t.TypeHandle.Value;
            return typeHandle;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    // The csharp compiler requires these types to be public, but they are not used elsewhere.
    [Obsolete("CodeAccessSecurityAttribute is no longer accessible to application code.")]
    public abstract class CodeAccessSecurityAttribute : SecurityAttribute
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
    sealed public class EnvironmentPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_read = null;
        private String m_write = null;
    
#pragma warning disable 618
        public EnvironmentPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Read {
            get { return m_read; }
            set { m_read = value; }
        }
    
        public String Write {
            get { return m_write; }
            set { m_write = value; }
        }

        public String All {
            get { throw new NotSupportedException( Environment.GetResourceString( "NotSupported_GetMethod" ) ); }
            set { m_write = value; m_read = value; }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new EnvironmentPermission(PermissionState.Unrestricted);
            }
            else
            {
                EnvironmentPermission perm = new EnvironmentPermission(PermissionState.None);
                if (m_read != null)
                    perm.SetPathList( EnvironmentPermissionAccess.Read, m_read );
                if (m_write != null)
                    perm.SetPathList( EnvironmentPermissionAccess.Write, m_write );
                return perm;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class FileDialogPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private FileDialogPermissionAccess m_access;

#pragma warning disable 618
        public FileDialogPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public bool Open
        {
            get { return (m_access & FileDialogPermissionAccess.Open) != 0; }
            set { m_access = value ? m_access | FileDialogPermissionAccess.Open : m_access & ~FileDialogPermissionAccess.Open; }
        }
            
        public bool Save
        {
            get { return (m_access & FileDialogPermissionAccess.Save) != 0; }
            set { m_access = value ? m_access | FileDialogPermissionAccess.Save : m_access & ~FileDialogPermissionAccess.Save; }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new FileDialogPermission( PermissionState.Unrestricted );
            }
            else
            {
                return new FileDialogPermission( m_access );
            }
        }
    }


    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class FileIOPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_read = null;
        private String m_write = null;
        private String m_append = null;
        private String m_pathDiscovery = null;
        private String m_viewAccess = null;
        private String m_changeAccess = null;
        [OptionalField(VersionAdded = 2)] private FileIOPermissionAccess m_allLocalFiles = FileIOPermissionAccess.NoAccess;
        [OptionalField(VersionAdded = 2)] private FileIOPermissionAccess m_allFiles = FileIOPermissionAccess.NoAccess;
    
#pragma warning disable 618
        public FileIOPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Read {
            get { return m_read; }
            set { m_read = value; }
        }
    
        public String Write {
            get { return m_write; }
            set { m_write = value; }
        }

        public String Append {
            get { return m_append; }
            set { m_append = value; }
        }

        public String PathDiscovery {
            get { return m_pathDiscovery; }
            set { m_pathDiscovery = value; }
        }

        public String ViewAccessControl {
            get { return m_viewAccess; }
            set { m_viewAccess = value; }
        }

        public String ChangeAccessControl {
            get { return m_changeAccess; }
            set { m_changeAccess = value; }
        }

        [Obsolete("Please use the ViewAndModify property instead.")]
        public String All {
            set { m_read = value; m_write = value; m_append = value; m_pathDiscovery = value; }
            get { throw new NotSupportedException( Environment.GetResourceString( "NotSupported_GetMethod" ) ); }
        }

        // Read, Write, Append, PathDiscovery, but no ACL-related permissions
        public String ViewAndModify {
            get { throw new NotSupportedException( Environment.GetResourceString( "NotSupported_GetMethod" ) ); }
            set { m_read = value; m_write = value; m_append = value; m_pathDiscovery = value; }
        }

        public FileIOPermissionAccess AllFiles {
            get { return m_allFiles; }
            set { m_allFiles = value; }
        }

        public FileIOPermissionAccess AllLocalFiles {
            get { return m_allLocalFiles; }
            set { m_allLocalFiles = value; }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new FileIOPermission(PermissionState.Unrestricted);
            }
            else
            {
                FileIOPermission perm = new FileIOPermission(PermissionState.None);
                if (m_read != null)
                    perm.SetPathList( FileIOPermissionAccess.Read, m_read );
                if (m_write != null)
                    perm.SetPathList( FileIOPermissionAccess.Write, m_write );
                if (m_append != null)
                    perm.SetPathList( FileIOPermissionAccess.Append, m_append );
                if (m_pathDiscovery != null)
                    perm.SetPathList( FileIOPermissionAccess.PathDiscovery, m_pathDiscovery );

                perm.AllFiles = m_allFiles;
                perm.AllLocalFiles = m_allLocalFiles;
                return perm;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
#pragma warning disable 618
    public sealed class KeyContainerPermissionAttribute : CodeAccessSecurityAttribute {
#pragma warning restore 618
        KeyContainerPermissionFlags m_flags = KeyContainerPermissionFlags.NoFlags;
        private string m_keyStore;
        private string m_providerName;
        private int m_providerType = -1;
        private string m_keyContainerName;
        private int m_keySpec = -1;

#pragma warning disable 618
        public KeyContainerPermissionAttribute(SecurityAction action) : base(action) {}
#pragma warning restore 618

        public string KeyStore {
            get { return m_keyStore; }
            set { m_keyStore = value; }
        }

        public string ProviderName {
            get { return m_providerName; }
            set { m_providerName = value; }
        }

        public int ProviderType {
            get { return m_providerType; }
            set { m_providerType = value; }
        }

        public string KeyContainerName {
            get { return m_keyContainerName; }
            set { m_keyContainerName = value; }
        }

        public int KeySpec {
            get { return m_keySpec; }
            set { m_keySpec = value; }
        }

        public KeyContainerPermissionFlags Flags {
            get { return m_flags; }
            set { m_flags = value; }
        }

        public override IPermission CreatePermission() {
            if (m_unrestricted) {
                return new KeyContainerPermission(PermissionState.Unrestricted);
            } else {
                if (KeyContainerPermissionAccessEntry.IsUnrestrictedEntry(m_keyStore, m_providerName, m_providerType, m_keyContainerName, m_keySpec))
                    return new KeyContainerPermission(m_flags);

                // create a KeyContainerPermission with a single access entry.
                KeyContainerPermission cp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry accessEntry = new KeyContainerPermissionAccessEntry(m_keyStore, m_providerName, m_providerType, m_keyContainerName, m_keySpec, m_flags);
                cp.AccessEntries.Add(accessEntry);
                return cp;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class ReflectionPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private ReflectionPermissionFlag m_flag = ReflectionPermissionFlag.NoFlags;

#pragma warning disable 618
        public ReflectionPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public ReflectionPermissionFlag Flags {
            get { return m_flag; }
            set { m_flag = value; }
        }

        [Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public bool TypeInformation {
#pragma warning disable 618
            get { return (m_flag & ReflectionPermissionFlag.TypeInformation) != 0; }
            set { m_flag = value ? m_flag | ReflectionPermissionFlag.TypeInformation : m_flag & ~ReflectionPermissionFlag.TypeInformation; }
#pragma warning restore 618
        }

        public bool MemberAccess {
            get { return (m_flag & ReflectionPermissionFlag.MemberAccess) != 0; }
            set { m_flag = value ? m_flag | ReflectionPermissionFlag.MemberAccess : m_flag & ~ReflectionPermissionFlag.MemberAccess; }
        }

        [Obsolete("This permission is no longer used by the CLR.")]
        public bool ReflectionEmit {
#pragma warning disable 618
            get { return (m_flag & ReflectionPermissionFlag.ReflectionEmit) != 0; }
            set { m_flag = value ? m_flag | ReflectionPermissionFlag.ReflectionEmit : m_flag & ~ReflectionPermissionFlag.ReflectionEmit; }
#pragma warning restore 618
        }

        public bool RestrictedMemberAccess
        {
            get { return (m_flag & ReflectionPermissionFlag.RestrictedMemberAccess) != 0; }
            set { m_flag = value ? m_flag | ReflectionPermissionFlag.RestrictedMemberAccess : m_flag & ~ReflectionPermissionFlag.RestrictedMemberAccess; }
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
    sealed public class RegistryPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_read = null;
        private String m_write = null;
        private String m_create = null;
        private String m_viewAcl = null;
        private String m_changeAcl = null;

#pragma warning disable 618
        public RegistryPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Read {
            get { return m_read; }
            set { m_read = value; }
        }
    
        public String Write {
            get { return m_write; }
            set { m_write = value; }
        }

        public String Create {
            get { return m_create; }
            set { m_create = value; }
        }

        public String ViewAccessControl {
            get { return m_viewAcl; }
            set { m_viewAcl = value; }
        }

        public String ChangeAccessControl {
            get { return m_changeAcl; }
            set { m_changeAcl = value; }
        }

        // Read, Write, & Create, but no ACL's
        public String ViewAndModify {
            get { throw new NotSupportedException( Environment.GetResourceString( "NotSupported_GetMethod" ) ); }
            set { m_read = value; m_write = value; m_create = value; }
        }

        [Obsolete("Please use the ViewAndModify property instead.")]
        public String All {
            get { throw new NotSupportedException( Environment.GetResourceString( "NotSupported_GetMethod" ) ); }
            set { m_read = value; m_write = value; m_create = value; }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new RegistryPermission( PermissionState.Unrestricted );
            }
            else
            {
                RegistryPermission perm = new RegistryPermission(PermissionState.None);
                if (m_read != null)
                    perm.SetPathList( RegistryPermissionAccess.Read, m_read );
                if (m_write != null)
                    perm.SetPathList( RegistryPermissionAccess.Write, m_write );
                if (m_create != null)
                    perm.SetPathList( RegistryPermissionAccess.Create, m_create );
                return perm;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class UIPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private UIPermissionWindow m_windowFlag = UIPermissionWindow.NoWindows;
        private UIPermissionClipboard m_clipboardFlag = UIPermissionClipboard.NoClipboard;
    
#pragma warning disable 618
        public UIPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public UIPermissionWindow Window {
            get { return m_windowFlag; }
            set { m_windowFlag = value; }
        }

        public UIPermissionClipboard Clipboard {
            get { return m_clipboardFlag; }
            set { m_clipboardFlag = value; }
        }
    
        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new UIPermission( PermissionState.Unrestricted );
            }
            else
            {
                return new UIPermission( m_windowFlag, m_clipboardFlag );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class ZoneIdentityPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private SecurityZone m_flag = SecurityZone.NoZone;
    
#pragma warning disable 618
        public ZoneIdentityPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public SecurityZone Zone {
            get { return m_flag; }
            set { m_flag = value; }
        }
    
        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new ZoneIdentityPermission(PermissionState.Unrestricted);
            }
            else
            {
                return new ZoneIdentityPermission( m_flag );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class StrongNameIdentityPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_name = null;
        private String m_version = null;
        private String m_blob = null;

#pragma warning disable 618
        public StrongNameIdentityPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        
        public String Version
        {
            get { return m_version; }
            set { m_version = value; }
        }
        
        public String PublicKey
        {
            get { return m_blob; }
            set { m_blob = value; }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new StrongNameIdentityPermission( PermissionState.Unrestricted );
            }
            else
            {
                if (m_blob == null && m_name == null && m_version == null)
                    return new StrongNameIdentityPermission( PermissionState.None );
            
                if (m_blob == null)
                    throw new ArgumentException( Environment.GetResourceString("ArgumentNull_Key"));
                    
                StrongNamePublicKeyBlob blob = new StrongNamePublicKeyBlob( m_blob );
                
                if (m_version == null || m_version.Equals(String.Empty))
                    return new StrongNameIdentityPermission( blob, m_name, null );
                else    
                    return new StrongNameIdentityPermission( blob, m_name, new Version( m_version ) );
            }
        }
    }


    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class SiteIdentityPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_site = null;
    
#pragma warning disable 618
        public SiteIdentityPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Site {
            get { return m_site; }
            set { m_site = value; }
        }
    
        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new SiteIdentityPermission( PermissionState.Unrestricted );
            }
            else
            {
                if (m_site == null)
                    return new SiteIdentityPermission( PermissionState.None );
            
                return new SiteIdentityPermission( m_site );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
#pragma warning disable 618
    [Serializable] sealed public class UrlIdentityPermissionAttribute : CodeAccessSecurityAttribute
#pragma warning restore 618
    {
        private String m_url = null;
    
#pragma warning disable 618
        public UrlIdentityPermissionAttribute( SecurityAction action )
#pragma warning restore 618
            : base( action )
        {
        }

        public String Url {
            get { return m_url; }
            set { m_url = value; }
        }
    
        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new UrlIdentityPermission( PermissionState.Unrestricted );
            }
            else
            {
                if (m_url == null)
                    return new UrlIdentityPermission( PermissionState.None );
                    
                return new UrlIdentityPermission( m_url );
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
#pragma warning disable 618
    sealed public class PermissionSetAttribute : CodeAccessSecurityAttribute
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

        public String File {
            get { return m_file; }
            set { m_file = value; }
        }
    
        public bool UnicodeEncoded {
            get { return m_unicode; }
            set { m_unicode = value; }
        }
        
        public String Name {
            get { return m_name; }
            set { m_name = value; }
        }
        
        public String XML {
            get { return m_xml; }
            set { m_xml = value; }
        }       

        public String Hex {
            get { return m_hex; }
            set { m_hex = value; }
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
