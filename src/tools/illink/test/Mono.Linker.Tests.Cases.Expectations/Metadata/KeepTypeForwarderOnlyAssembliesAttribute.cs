// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class KeepTypeForwarderOnlyAssembliesAttribute : BaseMetadataAttribute
    {
        public KeepTypeForwarderOnlyAssembliesAttribute(string value)
        {
#if NET // Avoid compile errors when targeting older TFMs
            ArgumentException.ThrowIfNullOrEmpty(value);
#endif
        }
    }
}
