// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Test implementation of ProviderAliasAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProviderAliasAttribute : Attribute
    {
        public ProviderAliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; }
    }
}