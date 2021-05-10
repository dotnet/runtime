// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Encapsulates a node in the Active Directory hierarchy
    /// that is returned during a search through <see cref='System.DirectoryServices.DirectorySearcher'/>.
    /// </devdoc>
    public class SearchResult
    {
        private readonly NetworkCredential? _parentCredentials;
        private readonly AuthenticationTypes _parentAuthenticationType;

        internal SearchResult(NetworkCredential? parentCredentials, AuthenticationTypes parentAuthenticationType)
        {
            _parentCredentials = parentCredentials;
            _parentAuthenticationType = parentAuthenticationType;
        }

        /// <devdoc>
        /// Retrieves the <see cref='System.DirectoryServices.DirectoryEntry'/> that corresponds to the <see cref='System.DirectoryServices.SearchResult'/>, from the Active Directory
        ///    hierarchy.
        /// </devdoc>
        public DirectoryEntry GetDirectoryEntry()
        {
            if (_parentCredentials != null)
                return new DirectoryEntry(Path, true, _parentCredentials.UserName, _parentCredentials.Password, _parentAuthenticationType);
            else
            {
                DirectoryEntry newEntry = new DirectoryEntry(Path, true, null, null, _parentAuthenticationType);
                return newEntry;
            }
        }

        /// <devdoc>
        /// Gets the path for this <see cref='System.DirectoryServices.SearchResult'/>.
        /// </devdoc>
        public string Path => (string)Properties["ADsPath"][0]!;

        /// <devdoc>
        /// Gets a <see cref='System.DirectoryServices.ResultPropertyCollection'/>
        /// of properties set on this object.
        /// </devdoc>
        public ResultPropertyCollection Properties { get; } = new ResultPropertyCollection();
    }
}
