// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
    [Conditional("INCLUDE_EXPECTATIONS")]
    public abstract class BaseMetadataAttribute : Attribute
    {
    }
}
