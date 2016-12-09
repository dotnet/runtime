//------------------------------------------------------------------------------
// <copyright file="SearchResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.Security.Permissions;

    /// <include file='doc\SearchResult.uex' path='docs/doc[@for="SearchResult"]/*' />
    /// <devdoc>
    ///    <para>Encapsulates a node in the Active Directory hierarchy 
    ///       that is returned during a search through <see cref='System.DirectoryServices.DirectorySearcher'/>.</para>
    /// </devdoc>
    [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
    public class SearchResult {
        private NetworkCredential parentCredentials;        
        private AuthenticationTypes parentAuthenticationType;
        private ResultPropertyCollection properties = new ResultPropertyCollection();

        internal SearchResult(NetworkCredential parentCredentials, AuthenticationTypes parentAuthenticationType) {
            this.parentCredentials = parentCredentials;             
            this.parentAuthenticationType = parentAuthenticationType;
        }

        /// <include file='doc\SearchResult.uex' path='docs/doc[@for="SearchResult.GetDirectoryEntry"]/*' />
        /// <devdoc>
        /// <para>Retrieves the <see cref='System.DirectoryServices.DirectoryEntry'/> that corresponds to the <see cref='System.DirectoryServices.SearchResult'/>, from the Active Directory 
        ///    hierarchy.</para>
        /// </devdoc>
        [        
            EnvironmentPermission(SecurityAction.Assert, Unrestricted=true),
            SecurityPermission(SecurityAction.Assert, Flags=SecurityPermissionFlag.UnmanagedCode)        
        ]
        public DirectoryEntry GetDirectoryEntry() {
            if (this.parentCredentials != null)
                return new DirectoryEntry(Path, true, this.parentCredentials.UserName, this.parentCredentials.Password, this.parentAuthenticationType);            
            else {                
                DirectoryEntry newEntry = new DirectoryEntry(Path, true, null, null, this.parentAuthenticationType);
                return newEntry;
            }                
        }

        /// <include file='doc\SearchResult.uex' path='docs/doc[@for="SearchResult.Path"]/*' />
        /// <devdoc>
        /// <para> Gets the path for this <see cref='System.DirectoryServices.SearchResult'/>.</para>
        /// </devdoc>
        public string Path {
            get {
                return (string) Properties["ADsPath"][0];
            }
        }

        /// <include file='doc\SearchResult.uex' path='docs/doc[@for="SearchResult.Properties"]/*' />
        /// <devdoc>
        /// <para>Gets a <see cref='System.DirectoryServices.ResultPropertyCollection'/>
        /// of properties set on this object.</para>
        /// </devdoc>
        public ResultPropertyCollection Properties {
            get {
                return properties;
            }
        }

    }

}
