// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public static class ReflectionTypeLoadExceptionTests
    {
        [Fact]
        public static void NullExceptionsNoNullPointerException()
        {
            Type[] types = new Type[1];
            Exception[] exceptions = new Exception[1];
            ReflectionTypeLoadException rtle = new ReflectionTypeLoadException(types, exceptions, "Null elements in Exceptions array");
            Assert.NotNull(rtle.ToString());
            Assert.NotNull(rtle.Message);
            Assert.Equal(1, rtle.LoaderExceptions.Length);
            Assert.Null(rtle.LoaderExceptions[0]);
            Assert.Equal(1, rtle.Types.Length);
            Assert.Null(rtle.Types[0]);
        }

        [Fact]
        public static void NullArgumentsNoNullPointerException()
        {
            ReflectionTypeLoadException rtle = new ReflectionTypeLoadException(null, null, "Null arguments");
            Assert.NotNull(rtle.ToString());
            Assert.NotNull(rtle.Message);
            Assert.Null(rtle.LoaderExceptions);
            Assert.Null(rtle.Types);
        }
    }
}
