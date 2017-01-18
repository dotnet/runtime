// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Indicates to the provider what default value to use for this setting when no stored value
    /// is found. The value should be encoded into a string and is interpreted based on the SerializeAs
    /// value for this setting. For example, if SerializeAs is Xml, the default value will be
    /// "stringified" Xml.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultSettingValueAttribute : Attribute
    {
        private readonly string _value;

        /// <summary>
        /// Constructor takes the default value as string.
        /// </summary>
        public DefaultSettingValueAttribute(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Default value.
        /// </summary>
        public string Value
        {
            get
            {
                return _value;
            }
        }
    }
}
