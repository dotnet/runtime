// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Reflection.Tests
{
    public static class ReflectionTypeLoadExceptionTests
    {
        [Fact]
        public static void NullExceptionsNoNullPointerException()
        {
            try
            {
                Type[] typo = new Type[1];
                Exception[] excepto = new Exception[1];
                throw new ReflectionTypeLoadException(typo, excepto, "Null elements in Exceptions array");
            }
            catch (ReflectionTypeLoadException e)
            {
                Assert.NotNull(e.ToString());
                Assert.NotNull(e.Message);
            }
        }

        [Fact]
        public static void NullArgumentsNoNullPointerException()
        {
            try
            {
                throw new ReflectionTypeLoadException(null, null, "Null arguments");
            }
            catch (ReflectionTypeLoadException e)
            {
                Assert.NotNull(e.ToString());
                Assert.NotNull(e.Message);
            }
        }
    }
}
