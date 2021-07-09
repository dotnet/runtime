// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Versioning.Tests
{
    public class RequiresPreviewFeaturesAttributeTests
    {
        [Fact]
        public void RequiresPreviewFeaturesAttributeTest()
        {
            new RequiresPreviewFeaturesAttribute();
        }
    }
}
