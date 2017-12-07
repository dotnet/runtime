// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Diagnostics
{
    public class CompositionTraceIdTests
    {
        [Fact]
        [ActiveIssue(25498, TargetFrameworkMonikers.UapAot)]
        public void CompositionTraceIdsAreInSyncWithTraceIds()
        {
            ExtendedAssert.EnumsContainSameValues<CompositionTraceId, TraceId>();
        }
    }
}
