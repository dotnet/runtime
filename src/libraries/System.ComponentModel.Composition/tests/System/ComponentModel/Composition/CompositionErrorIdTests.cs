// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionErrorIdTests
    {
        [Fact]
        public void CompositionErrorIdsAreInSyncWithErrorIds()
        {
            ExtendedAssert.EnumsContainSameValues<CompositionErrorId, ErrorId>();
        }
    }
}
