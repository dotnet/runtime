// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Indicates the provider associated with a group of/individual setting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class SettingsProviderAttribute : Attribute
    {
        private readonly string _providerTypeName;

        /// <summary>
        /// Constructor takes the provider's assembly qualified type name.
        /// </summary>
        public SettingsProviderAttribute(string providerTypeName)
        {
            _providerTypeName = providerTypeName;
        }

        /// <summary>
        /// Constructor takes the provider's type.
        /// </summary>
        public SettingsProviderAttribute(Type providerType)
        {
            if (providerType != null)
            {
                _providerTypeName = providerType.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Type name of the provider
        /// </summary>
        public string ProviderTypeName
        {
            get
            {
                return _providerTypeName;
            }
        }
    }
}
