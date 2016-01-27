// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
//  Extends PermissionSet to allow an associated name and description
//

namespace System.Security {
    
    using System;
    using System.Security.Util;
    using System.Security.Permissions;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

#if !FEATURE_CAS_POLICY
    using Microsoft.Win32;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Runtime.Versioning;
    using System.Text;
    
#else // FEATURE_CAS_POLICY
    
    using System.Threading;

#endif // FEATURE_CAS_POLICY
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NamedPermissionSet : PermissionSet
    {
#if FEATURE_CAS_POLICY
        // The name of this PermissionSet
        private String m_name;
        
        // The description of this PermissionSet
        private String m_description;
        [OptionalField(VersionAdded = 2)]                
        internal String m_descrResource;

        internal NamedPermissionSet()
            : base()
        {
        }
        
        public NamedPermissionSet( String name )
            : base()
        {
            CheckName( name );
            m_name = name;
        }
        
        public NamedPermissionSet( String name, PermissionState state)
            : base( state )
        {
            CheckName( name );
            m_name = name;
        }
        
        
        public NamedPermissionSet( String name, PermissionSet permSet )
            : base( permSet )
        {
            CheckName( name );
            m_name = name;
        }

        public NamedPermissionSet( NamedPermissionSet permSet )
            : base( permSet )
        {
            m_name = permSet.m_name;
            m_description = permSet.Description;
        }

        internal NamedPermissionSet(SecurityElement permissionSetXml)
            : base(PermissionState.None)
        {
            Contract.Assert(permissionSetXml != null);
            FromXml(permissionSetXml);
        }

        public String Name {
            get { return m_name; }
            set { CheckName( value ); m_name = value; }
        }
    
        private static void CheckName( String name )
        {
            if (name == null || name.Equals( "" ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_NPMSInvalidName" ));
            Contract.EndContractBlock();
        }
        
        public String Description {
            get
            {
                if(m_descrResource != null)
                {
                    m_description = Environment.GetResourceString(m_descrResource);
                    m_descrResource = null;
                }
                return m_description;
            }

            set
            {
                m_description = value;
                m_descrResource = null;
            }
        }
        
        public override PermissionSet Copy()
        {
            return new NamedPermissionSet( this );
        }
        
        public NamedPermissionSet Copy( String name )
        {
            NamedPermissionSet set = new NamedPermissionSet( this );
            set.Name = name;
            return set;
        }
        
        public override SecurityElement ToXml()
        {
            SecurityElement elem = base.ToXml("System.Security.NamedPermissionSet");
            // If you hit this assert then most likely you are trying to change the name of this class. 
            // This is ok as long as you change the hard coded string above and change the assert below.
            Contract.Assert( this.GetType().FullName.Equals( "System.Security.NamedPermissionSet" ), "Class name changed!" );

            if (m_name != null && !m_name.Equals( "" ))
            {
                elem.AddAttribute( "Name", SecurityElement.Escape( m_name ) );
            }
            
            if (Description != null && !Description.Equals( "" ))
            {
                elem.AddAttribute( "Description", SecurityElement.Escape( Description ) );
            }
            
            return elem;
        }
        
        public override void FromXml( SecurityElement et )
        {
            FromXml( et, false, false );
        }

        internal override void FromXml( SecurityElement et, bool allowInternalOnly, bool ignoreTypeLoadFailures )
        {
            if (et == null)
                throw new ArgumentNullException( "et" );
            Contract.EndContractBlock();

            String elem;

            elem = et.Attribute( "Name" );
            m_name = elem == null ? null : elem;

            elem = et.Attribute( "Description" );
            m_description = (elem == null ? "" : elem);
            m_descrResource = null;

            base.FromXml( et, allowInternalOnly, ignoreTypeLoadFailures );
        }

        internal void FromXmlNameOnly( SecurityElement et )
        {
            // This function gets only the name for the permission set, ignoring all other info.

            String elem;

            elem = et.Attribute( "Name" );
            m_name = (elem == null ? null : elem);
        }

        // NamedPermissionSet Equals should have the exact semantic as PermissionSet.
        // We explicitly override them here to make sure that no one accidently
        // changes this.

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals( Object obj )
        {
            return base.Equals( obj );
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }
#else // FEATURE_CAS_POLICY

        internal static PermissionSet GetBuiltInSet(string name) {
            // Used by PermissionSetAttribute to create one of the built-in,
            // immutable permission sets.
        
            if (name == null)
                return null;
            else if (name.Equals("FullTrust"))
                return CreateFullTrustSet();
            else if (name.Equals("Nothing"))
                return CreateNothingSet();
            else if (name.Equals("Execution"))
                return CreateExecutionSet();
            else if (name.Equals("SkipVerification"))
                return CreateSkipVerificationSet();
            else if (name.Equals("Internet"))
                return CreateInternetSet();
            else
                return null;
        }

        private static PermissionSet CreateFullTrustSet() {
            return new PermissionSet(PermissionState.Unrestricted);
        }

        private static PermissionSet CreateNothingSet() {
            return new PermissionSet(PermissionState.None);
        }

        private static PermissionSet CreateExecutionSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
#pragma warning restore 618
            return permSet;
        }

        private static PermissionSet CreateSkipVerificationSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.SkipVerification));
#pragma warning restore 618
            return permSet;
        }

        private static PermissionSet CreateInternetSet() {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new FileDialogPermission(FileDialogPermissionAccess.Open));
#pragma warning disable 618
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
#pragma warning restore 618
            permSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows, UIPermissionClipboard.OwnClipboard));
            return permSet;
            

        }
#endif // !FEATURE_CAS_POLICY
    }
}
