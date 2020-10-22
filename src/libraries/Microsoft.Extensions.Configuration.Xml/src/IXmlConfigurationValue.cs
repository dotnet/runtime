// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// Represents a configuration value that was parsed from an XML source
    /// </summary>
    internal interface IXmlConfigurationValue
    {
        string Key { get; }
        string Value { get; }
        string LineInfo { get; }
    }
}
