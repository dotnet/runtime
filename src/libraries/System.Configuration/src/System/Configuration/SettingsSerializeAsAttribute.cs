// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Indicates the SettingsSerializeAs for a group of/individual setting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class SettingsSerializeAsAttribute : Attribute
    {
        private readonly SettingsSerializeAs _serializeAs;

        /// <summary>
        /// Constructor takes a SettingsSerializeAs enum value.
        /// </summary>
        public SettingsSerializeAsAttribute(SettingsSerializeAs serializeAs)
        {
            _serializeAs = serializeAs;
        }

        /// <summary>
        /// SettingsSerializeAs value to use
        /// </summary>
        public SettingsSerializeAs SerializeAs
        {
            get
            {
                return _serializeAs;
            }
        }
    }
}
