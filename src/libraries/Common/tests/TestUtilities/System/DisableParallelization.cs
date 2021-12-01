// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System
{
    // The collection definitions must be in the same assembly as the test that uses them.
    // So please use "Compile Include" in the project file to include this class.
    [CollectionDefinition(nameof(DisableParallelization), DisableParallelization = true)]
    public class DisableParallelization { }
}
