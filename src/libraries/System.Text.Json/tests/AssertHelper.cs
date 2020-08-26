// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class AssertHelper
    {
        public static void ValidateJson(IEnumerable<string> expectedProperties, string json)
        {
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
            foreach (string expectedProperty in expectedProperties)
                Assert.Contains(expectedProperty, json);
        }

    }
}
