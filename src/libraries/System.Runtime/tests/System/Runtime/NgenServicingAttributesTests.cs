// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Tests
{
    public static class NgenServicingAttributesTests
    {
        [Fact]
        public static void AssemblyTargetedPatchBandAttributeTest()
        {
            string targetedPatchBand = "testStr";
            var attr = new AssemblyTargetedPatchBandAttribute(targetedPatchBand);
            Assert.Equal(targetedPatchBand, attr.TargetedPatchBand);
        }

        [Fact]
        public static void TargetedPatchingOptOutAttributeTest()
        {
            string reason = "testStr";
            var attr = new TargetedPatchingOptOutAttribute(reason);
            Assert.Equal(reason, attr.Reason);
        }
    }
}
