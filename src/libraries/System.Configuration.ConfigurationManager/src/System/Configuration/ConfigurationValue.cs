// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    internal class ConfigurationValue
    {
        internal PropertySourceInfo SourceInfo;
        internal object Value;

        internal ConfigurationValueFlags ValueFlags;

        internal ConfigurationValue(object value, ConfigurationValueFlags valueFlags, PropertySourceInfo sourceInfo)
        {
            Value = value;
            ValueFlags = valueFlags;
            SourceInfo = sourceInfo;
        }
    }
}