// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Defines alias for <see cref="ILoggerProvider"/> implementation to be used in filtering rules.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProviderAliasAttribute: Attribute
    {
        public ProviderAliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; }

    }
}