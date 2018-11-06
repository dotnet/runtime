// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                throw new ArgumentException(Resources.Common_StringNullOrEmpty, nameof(userSecretId));
            }

            UserSecretsId = userSecretId;
        }

        /// <summary>
        /// The user secrets ID.
        /// </summary>
        public string UserSecretsId { get; }
    }
}
