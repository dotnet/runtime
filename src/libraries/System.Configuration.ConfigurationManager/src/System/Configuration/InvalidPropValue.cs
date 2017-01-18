// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // Class to support caching of property values as string
    internal sealed class InvalidPropValue
    {
        internal InvalidPropValue(string value, ConfigurationException error)
        {
            Value = value;
            Error = error;
        }

        internal ConfigurationException Error { get; }

        internal string Value { get; }
    }
}