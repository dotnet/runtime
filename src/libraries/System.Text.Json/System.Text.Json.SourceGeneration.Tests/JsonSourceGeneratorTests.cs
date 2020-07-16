// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public class JsonSerializerSouceGeneratorTests
    {
        [Fact]
        public static void TestGeneratedCode()
        {
            Assert.Equal("Hello", HelloWorldGenerated.HelloWorld.SayHello());
        }
    }
}
