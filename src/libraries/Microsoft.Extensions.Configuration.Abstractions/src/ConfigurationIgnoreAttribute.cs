// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Specifies that a configuration property should be excluded from binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigurationIgnoreAttribute : Attribute
    {
    }
}
