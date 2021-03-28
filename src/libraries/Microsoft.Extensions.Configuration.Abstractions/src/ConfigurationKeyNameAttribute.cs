// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ConfigurationKeyNameAttribute : Attribute
    {
        public ConfigurationKeyNameAttribute()
        {
        }

        public ConfigurationKeyNameAttribute(string name) => Name = name;

        public string Name { get; }
    }
}
