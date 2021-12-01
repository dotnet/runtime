// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    internal sealed class ConfigurationValue
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
