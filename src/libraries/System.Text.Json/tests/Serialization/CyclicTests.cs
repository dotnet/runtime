// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CyclicTests
    {
        [Fact]
        public static void WriteCyclicFail()
        {
            TestClassWithCycle obj = new TestClassWithCycle();
            obj.Initialize();

            // We don't allow cycles; we throw InvalidOperation instead of an unrecoverable StackOverflow.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.ToString(obj));
        }

        [Fact]
        [ActiveIssue(37313)]
        public static void WriteTestClassWithArrayOfElementsOfTheSameClassWithoutCyclesDoesNotFail()
        {
            TestClassWithArrayOfElementsOfTheSameClass obj = new TestClassWithArrayOfElementsOfTheSameClass();

            //It shouldn't throw when there is no real cycle reference, and just empty object is created
            string json = JsonSerializer.ToString(obj);
            Assert.Equal(@"{}", json);
        }
    }
}
