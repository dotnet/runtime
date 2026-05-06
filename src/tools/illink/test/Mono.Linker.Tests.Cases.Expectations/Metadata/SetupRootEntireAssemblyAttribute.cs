// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    /// <summary>
    /// Request that the specified assembly's entire contents be kept.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SetupRootEntireAssemblyAttribute : BaseMetadataAttribute
    {
        public SetupRootEntireAssemblyAttribute(string assembly)
        {
            if (string.IsNullOrEmpty(assembly))
                throw new ArgumentNullException(nameof(assembly));
        }
    }
}
