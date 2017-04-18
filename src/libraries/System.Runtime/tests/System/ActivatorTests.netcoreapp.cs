// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class ActivatorNetcoreTests
    {
        [Fact]
        public static void CreateInstance_Invalid()
        {
            // Type is not a valid RuntimeType
            AssertExtensions.Throws<ArgumentException>("type", () => Activator.CreateInstance(Helpers.NonRuntimeType()));
        }
    }
}
