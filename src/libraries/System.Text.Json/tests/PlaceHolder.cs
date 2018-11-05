// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class PlaceHolderTests
    {
        [Fact]
        public static void JsonTokenTypeEnumvalues()
        {
            int expectedNumberOfTokenTypes = 12;
            int numberOfTokenTypes = 0;
            foreach (JsonTokenType token in Enum.GetValues(typeof(JsonTokenType)))
            {
                numberOfTokenTypes++;
            }
            Assert.Equal(expectedNumberOfTokenTypes, numberOfTokenTypes);
        }
    }
}
