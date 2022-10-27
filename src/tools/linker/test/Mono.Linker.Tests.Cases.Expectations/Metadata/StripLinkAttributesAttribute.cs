// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    public sealed class StripLinkAttributesAttribute : BaseMetadataAttribute
    {
        public readonly bool Value;

        public StripLinkAttributesAttribute(bool value)
        {
            Value = value;
        }
    }
}
