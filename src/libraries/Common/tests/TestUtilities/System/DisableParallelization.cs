// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System
{
    [CollectionDefinition(nameof(DisableParallelization), DisableParallelization = true)]
    public class DisableParallelization { }
}
