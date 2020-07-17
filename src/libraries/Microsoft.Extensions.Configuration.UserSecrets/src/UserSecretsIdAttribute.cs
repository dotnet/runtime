// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    /// <summary>
    /// <para>
    /// Represents the user secrets ID.
    /// </para>
    /// <para>
    /// In most cases, this attribute is automatically generated during compilation by MSBuild targets
    /// included in the UserSecrets NuGet package. These targets use the MSBuild property 'UserSecretsId'
    /// to set the value for <see cref="UserSecretsId"/>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class UserSecretsIdAttribute : Attribute
    {
        /// <summary>
        /// Initializes an instance of <see cref="UserSecretsIdAttribute" />.
        /// </summary>
        /// <param name="userSecretId">The user secrets ID.</param>
        public UserSecretsIdAttribute(string userSecretId)
        {
            if (string.IsNullOrEmpty(userSecretId))
            {
                throw new ArgumentException(SR.Common_StringNullOrEmpty, nameof(userSecretId));
            }

            UserSecretsId = userSecretId;
        }

        /// <summary>
        /// The user secrets ID.
        /// </summary>
        public string UserSecretsId { get; }
    }
}
