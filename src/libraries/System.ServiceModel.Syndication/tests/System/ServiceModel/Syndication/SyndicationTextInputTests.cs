// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationTextInputTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var textInput = new SyndicationTextInput();
            Assert.Null(textInput.Description);
            Assert.Null(textInput.Link);
            Assert.Null(textInput.Name);
            Assert.Null(textInput.Title);
        }
    }
}
