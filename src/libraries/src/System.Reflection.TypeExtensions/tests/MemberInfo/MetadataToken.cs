// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.TypeExtensions.Tests.MemberInfo
{
    public class MetadataTokenTests
    {
        // This applies on all platforms. See S.R.TE.CoreCLR.Tests for more test cases that rely on
        // that rely platform-specific capabilities.
        [Fact]
        public void ArraysAndTheirMembersDoNotHaveMetadataTokens()
        {
            Assert.False(typeof(byte[]).GetTypeInfo().HasMetadataToken());
            Assert.Throws<InvalidOperationException>(() => typeof(byte[]).GetTypeInfo().GetMetadataToken());

            Assert.False(typeof(byte[]).GetMethods()[0].HasMetadataToken());
            Assert.Throws<InvalidOperationException>(() => typeof(byte[]).GetTypeInfo().GetMetadataToken());
        }
    }
}
