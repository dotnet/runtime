// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Options that control how a <seealso cref="Uri"/> is created and behaves.
    /// </summary>
    public struct UriCreationOptions
    {
        private bool _disablePathAndQueryCanonicalization;

        /// <summary>
        /// Disables validation and normalization of the Path and Query.
        /// No transformations of the URI past the Authority will take place.
        /// <see cref="Uri"/> instances created with this option do not support <see cref="Uri.Fragment"/>s.
        /// <see cref="Uri.GetComponents(UriComponents, UriFormat)"/> may not be used for <see cref="UriComponents.Path"/> or <see cref="UriComponents.Query"/>.
        /// Be aware that disabling canonicalization also means that reserved characters will not be escaped,
        /// which may corrupt the HTTP request and makes the application subject to request smuggling.
        /// Only set this option if you have ensured that the URI string is already sanitized.
        /// </summary>
        public bool DangerousDisablePathAndQueryCanonicalization
        {
            readonly get => _disablePathAndQueryCanonicalization;
            set => _disablePathAndQueryCanonicalization = value;
        }
    }
}
