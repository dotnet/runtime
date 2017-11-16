// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Management.Tests
{
    [CollectionDefinition("Mof Collection")]
    [OuterLoop]
    public class MofCollection : ICollectionFixture<MofFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the 
        // ICollectionFixture<> interfaces.
        // See https://xunit.github.io/docs/shared-context.html#collection-fixture
    }
}
