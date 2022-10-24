// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SetupLinkerActionAttribute : BaseMetadataAttribute
    {
        public SetupLinkerActionAttribute(string action, string assembly)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentNullException(nameof(action));
        }
    }
}
