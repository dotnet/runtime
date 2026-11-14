// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class PropertyChangedEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("propertyName")]
        public void Ctor_String(string? propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            Assert.Equal(propertyName, e.PropertyName);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        [InlineData(null, "PropertyName = null")]
        [InlineData("", "PropertyName = \"\"")]
        [InlineData("propertyName", "PropertyName = \"propertyName\"")]
        public void DebuggerDisplay(string? propertyName, string expected)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            Assert.Equal(expected, DebuggerAttributes.ValidateDebuggerDisplayReferences(e));
        }
    }
}
